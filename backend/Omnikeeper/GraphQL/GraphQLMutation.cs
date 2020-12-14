using GraphQL;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.Config;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Omnikeeper.GraphQL
{
    public class GraphQLMutation : ObjectGraphType
    {
        public GraphQLMutation()
        {
            FieldAsync<MutateReturnType>("mutateCIs",
                arguments: new QueryArguments(
                new QueryArgument<ListGraphType<StringGraphType>> { Name = "layers" },
                new QueryArgument<ListGraphType<InsertCIAttributeInputType>> { Name = "InsertAttributes" },
                new QueryArgument<ListGraphType<RemoveCIAttributeInputType>> { Name = "RemoveAttributes" },
                new QueryArgument<ListGraphType<InsertRelationInputType>> { Name = "InsertRelations" },
                new QueryArgument<ListGraphType<RemoveRelationInputType>> { Name = "RemoveRelations" }
                ),
                resolve: async context =>
                {
                    var layerModel = context.RequestServices.GetRequiredService<ILayerModel>();
                    var ciModel = context.RequestServices.GetRequiredService<ICIModel>();
                    var changesetModel = context.RequestServices.GetRequiredService<IChangesetModel>();
                    var attributeModel = context.RequestServices.GetRequiredService<IAttributeModel>();
                    var relationModel = context.RequestServices.GetRequiredService<IRelationModel>();
                    var ciBasedAuthorizationService = context.RequestServices.GetRequiredService<ICIBasedAuthorizationService>();
                    var layerBasedAuthorizationService = context.RequestServices.GetRequiredService<ILayerBasedAuthorizationService>();
                    var modelContextBuilder = context.RequestServices.GetRequiredService<IModelContextBuilder>();

                    var layers = context.GetArgument<string[]?>("layers", null);
                    var insertAttributes = context.GetArgument("InsertAttributes", new List<InsertCIAttributeInput>());
                    var removeAttributes = context.GetArgument("RemoveAttributes", new List<RemoveCIAttributeInput>());
                    var insertRelations = context.GetArgument("InsertRelations", new List<InsertRelationInput>());
                    var removeRelations = context.GetArgument("RemoveRelations", new List<RemoveRelationInput>());

                    var userContext = (context.UserContext as OmnikeeperUserContext)!;

                    var writeLayerIDs = insertAttributes.Select(a => a.LayerID)
                    .Concat(removeAttributes.Select(a => a.LayerID))
                    .Concat(insertRelations.Select(a => a.LayerID))
                    .Concat(removeRelations.Select(a => a.LayerID))
                    .Distinct();
                    if (!layerBasedAuthorizationService.CanUserWriteToLayers(userContext.User, writeLayerIDs))
                        throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to write to at least one of the following layerIDs: {string.Join(',', writeLayerIDs)}");

                    var writeCIIDs = insertAttributes.Select(a => a.CI)
                    .Concat(removeAttributes.Select(a => a.CI))
                    .Concat(insertRelations.SelectMany(a => new Guid[] { a.FromCIID, a.ToCIID }))
                    .Concat(removeRelations.SelectMany(a => new Guid[] { a.FromCIID, a.ToCIID }))
                    .Distinct();
                    if (!ciBasedAuthorizationService.CanWriteToAllCIs(writeCIIDs, out var notAllowedCI))
                        throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to write to CI {notAllowedCI}");

                    using var transaction = modelContextBuilder.BuildDeferred();
                    userContext.LayerSet = layers != null ? await layerModel.BuildLayerSet(layers, transaction) : null;
                    userContext.TimeThreshold = TimeThreshold.BuildLatest();

                    var changeset = new ChangesetProxy(userContext.User.InDatabase, userContext.TimeThreshold.Time, changesetModel); //await changesetModel.CreateChangeset(userContext.User.InDatabase.ID, transaction, userContext.TimeThreshold.Time);

                    var groupedInsertAttributes = insertAttributes.GroupBy(a => a.CI);
                    var insertedAttributes = new List<CIAttribute>();
                    foreach (var attributeGroup in groupedInsertAttributes)
                    {
                        // look for ciid
                        var ciIdentity = attributeGroup.Key;
                        foreach (var attribute in attributeGroup)
                        {
                            var nonGenericAttributeValue = AttributeValueBuilder.BuildFromDTO(attribute.Value);

                            var (a, changed) = await attributeModel.InsertAttribute(attribute.Name, nonGenericAttributeValue, ciIdentity, attribute.LayerID, changeset, new DataOriginV1(DataOriginType.Manual), transaction);
                            insertedAttributes.Add(a);
                        }
                    }

                    var groupedRemoveAttributes = removeAttributes.GroupBy(a => a.CI);
                    var removedAttributes = new List<CIAttribute>();
                    foreach (var attributeGroup in groupedRemoveAttributes)
                    {
                        // look for ciid
                        var ciIdentity = attributeGroup.Key;
                        foreach (var attribute in attributeGroup)
                        {
                            var (a, changed) = await attributeModel.RemoveAttribute(attribute.Name, ciIdentity, attribute.LayerID, changeset, transaction);
                            removedAttributes.Add(a);
                        }
                    }

                    var insertedRelations = new List<Relation>();
                    foreach (var insertRelation in insertRelations)
                    {
                        var (r, changed) = await relationModel.InsertRelation(insertRelation.FromCIID, insertRelation.ToCIID, insertRelation.PredicateID, insertRelation.LayerID, changeset, new DataOriginV1(DataOriginType.Manual), transaction);
                        insertedRelations.Add(r);
                    }

                    var removedRelations = new List<Relation>();
                    foreach (var removeRelation in removeRelations)
                    {
                        var (r, changed) = await relationModel.RemoveRelation(removeRelation.FromCIID, removeRelation.ToCIID, removeRelation.PredicateID, removeRelation.LayerID, changeset, transaction);
                        removedRelations.Add(r);
                    }

                    IEnumerable<MergedCI> affectedCIs = new List<MergedCI>(); ;
                    if (userContext.LayerSet != null)
                    {
                        var affectedCIIDs = removedAttributes.Select(r => r.CIID)
                        .Concat(insertedAttributes.Select(i => i.CIID))
                        .Concat(insertedRelations.SelectMany(i => new Guid[] { i.FromCIID, i.ToCIID }))
                        .Concat(removedRelations.SelectMany(i => new Guid[] { i.FromCIID, i.ToCIID }))
                        .Distinct();
                        if (!affectedCIIDs.IsEmpty())
                            affectedCIs = await ciModel.GetMergedCIs(SpecificCIIDsSelection.Build(affectedCIIDs), userContext.LayerSet, true, transaction, userContext.TimeThreshold);
                    }

                    transaction.Commit();
                    userContext.Transaction = modelContextBuilder.BuildImmediate(); // HACK: so that later running parts of the graphql tree have a proper transaction object

                    return new MutateReturn(insertedAttributes, removedAttributes, insertedRelations, affectedCIs);
                });

            FieldAsync<CreateCIsReturnType>("createCIs",
                arguments: new QueryArguments(
                new QueryArgument<ListGraphType<CreateCIInputType>> { Name = "cis" }
                ),
                resolve: async context =>
                {
                    var layerModel = context.RequestServices.GetRequiredService<ILayerModel>();
                    var ciModel = context.RequestServices.GetRequiredService<ICIModel>();
                    var changesetModel = context.RequestServices.GetRequiredService<IChangesetModel>();
                    var attributeModel = context.RequestServices.GetRequiredService<IAttributeModel>();
                    var relationModel = context.RequestServices.GetRequiredService<IRelationModel>();
                    var managementAuthorizationService = context.RequestServices.GetRequiredService<IManagementAuthorizationService>();
                    var layerBasedAuthorizationService = context.RequestServices.GetRequiredService<ILayerBasedAuthorizationService>();
                    var modelContextBuilder = context.RequestServices.GetRequiredService<IModelContextBuilder>();

                    var createCIs = context.GetArgument("cis", new List<CreateCIInput>());

                    var userContext = (context.UserContext as OmnikeeperUserContext)!;

                    if (!managementAuthorizationService.CanUserCreateCI(userContext.User))
                        throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to create CIs");
                    if (!layerBasedAuthorizationService.CanUserWriteToLayers(userContext.User, createCIs.Select(ci => ci.LayerIDForName)))
                        throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to write to at least one of the following layerIDs: {string.Join(',', createCIs.Select(ci => ci.LayerIDForName))}");
                    // NOTE: a newly created CI cannot be checked with CIBasedAuthorizationService yet. That's why we don't do a .CanWriteToCI() check here

                    using var transaction = modelContextBuilder.BuildDeferred();
                    userContext.TimeThreshold = TimeThreshold.BuildLatest();

                    var changeset = new ChangesetProxy(userContext.User.InDatabase, userContext.TimeThreshold.Time, changesetModel);

                    var createdCIIDs = new List<Guid>();
                    foreach (var ci in createCIs)
                    {
                        Guid ciid = await ciModel.CreateCI(transaction);

                        await attributeModel.InsertCINameAttribute(ci.Name, ciid, ci.LayerIDForName, changeset, new DataOriginV1(DataOriginType.Manual), transaction);

                        createdCIIDs.Add(ciid);
                    }
                    transaction.Commit();
                    userContext.Transaction = modelContextBuilder.BuildImmediate(); // HACK: so that later running parts of the graphql tree have a proper transaction object

                    return new CreateCIsReturn(createdCIIDs);
                });

            FieldAsync<LayerType>("createLayer",
                arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<CreateLayerInputType>> { Name = "layer" }
                ),
                resolve: async context =>
                {
                    var layerModel = context.RequestServices.GetRequiredService<ILayerModel>();
                    var modelContextBuilder = context.RequestServices.GetRequiredService<IModelContextBuilder>();
                    var managementAuthorizationService = context.RequestServices.GetRequiredService<IManagementAuthorizationService>();
                    
                    var createLayer = context.GetArgument<CreateLayerInput>("layer");
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;

                    if (!managementAuthorizationService.CanUserCreateLayer(userContext.User))
                        throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to create Layers");

                    using var transaction = modelContextBuilder.BuildDeferred();

                    ComputeLayerBrainLink clb = LayerModel.DefaultCLB;
                    if (createLayer.BrainName != null && createLayer.BrainName != "")
                        clb = ComputeLayerBrainLink.Build(createLayer.BrainName);
                    OnlineInboundAdapterLink oilp = LayerModel.DefaultOILP;
                    if (createLayer.OnlineInboundAdapterName != null && createLayer.OnlineInboundAdapterName != "")
                        oilp = OnlineInboundAdapterLink.Build(createLayer.OnlineInboundAdapterName);
                    var createdLayer = await layerModel.CreateLayer(createLayer.Name, Color.FromArgb(createLayer.Color), createLayer.State, clb, oilp, transaction);

                    //var writeAccessGroupInKeycloakCreated = await keycloakModel.CreateGroup(authorizationService.GetWriteAccessGroupNameFromLayerName(createLayer.Name));
                    //if (!writeAccessGroupInKeycloakCreated)
                    //{
                    //    throw new Exception("Could not create write access layer in keycloak realm");
                    //}

                    transaction.Commit();
                    userContext.Transaction = modelContextBuilder.BuildImmediate(); // HACK: so that later running parts of the graphql tree have a proper transaction object

                    return createdLayer;
                });
            FieldAsync<LayerType>("updateLayer",
              arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<UpdateLayerInputType>> { Name = "layer" }
              ),
              resolve: async context =>
              {
                  var layerModel = context.RequestServices.GetRequiredService<ILayerModel>();
                  var modelContextBuilder = context.RequestServices.GetRequiredService<IModelContextBuilder>();
                  var managementAuthorizationService = context.RequestServices.GetRequiredService<IManagementAuthorizationService>();

                  var layer = context.GetArgument<UpdateLayerInput>("layer");

                  var userContext = (context.UserContext as OmnikeeperUserContext)!;

                  if (!managementAuthorizationService.CanUserUpdateLayer(userContext.User))
                      throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to update Layers");

                  using var transaction = modelContextBuilder.BuildDeferred();
                  var clb = ComputeLayerBrainLink.Build(layer.BrainName);
                  var oilp = OnlineInboundAdapterLink.Build(layer.OnlineInboundAdapterName);
                  var updatedLayer = await layerModel.Update(layer.ID, Color.FromArgb(layer.Color), layer.State, clb, oilp, transaction);
                  transaction.Commit();
                  userContext.Transaction = modelContextBuilder.BuildImmediate(); // HACK: so that later running parts of the graphql tree have a proper transaction object

                  return updatedLayer;
              });


            FieldAsync<OIAContextType>("createOIAContext",
                arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<CreateOIAContextInputType>> { Name = "oiaContext" }
                ),
                resolve: async context =>
                {
                    var OIAContextModel = context.RequestServices.GetRequiredService<IOIAContextModel>();
                    var modelContextBuilder = context.RequestServices.GetRequiredService<IModelContextBuilder>();

                    var configInput = context.GetArgument<CreateOIAContextInput>("oiaContext");
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;

                    // TODO: auth
                    //if (!authorizationService.CanUserCreateLayer(userContext.User))
                    //    throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to create Layers");

                    using var transaction = modelContextBuilder.BuildDeferred();

                    try
                    {
                        var config = IOnlineInboundAdapter.IConfig.Serializer.Deserialize(configInput.Config);
                        var createdOIAContext = await OIAContextModel.Create(configInput.Name, config, transaction);

                        transaction.Commit();
                        userContext.Transaction = modelContextBuilder.BuildImmediate(); // HACK: so that later running parts of the graphql tree have a proper transaction object

                        return createdOIAContext;
                    }
                    catch (Exception e)
                    {
                        throw new ExecutionError($"Could not parse configuration", e);
                    }
                });
            FieldAsync<OIAContextType>("updateOIAContext",
              arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<UpdateOIAContextInputType>> { Name = "oiaContext" }
              ),
              resolve: async context =>
              {
                  var OIAContextModel = context.RequestServices.GetRequiredService<IOIAContextModel>();
                  var modelContextBuilder = context.RequestServices.GetRequiredService<IModelContextBuilder>();

                  var configInput = context.GetArgument<UpdateOIAContextInput>("oiaContext");

                  var userContext = (context.UserContext as OmnikeeperUserContext)!;

                  // TODO: auth
                  //if (!authorizationService.CanUserUpdateLayer(userContext.User))
                  //    throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to update Layers");

                  using var transaction = modelContextBuilder.BuildDeferred();

                  try
                  {
                      var config = IOnlineInboundAdapter.IConfig.Serializer.Deserialize(configInput.Config);
                      var oiaContext = await OIAContextModel.Update(configInput.ID, configInput.Name, config, transaction);
                      transaction.Commit();
                      userContext.Transaction = modelContextBuilder.BuildImmediate(); // HACK: so that later running parts of the graphql tree have a proper transaction object

                      return oiaContext;
                  }
                  catch (Exception e)
                  {
                      throw new ExecutionError($"Could not parse configuration", e);
                  }
              });
            FieldAsync<BooleanGraphType>("deleteOIAContext",
              arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<LongGraphType>> { Name = "oiaID" }
              ),
              resolve: async context =>
              {
                  var OIAContextModel = context.RequestServices.GetRequiredService<IOIAContextModel>();
                  var modelContextBuilder = context.RequestServices.GetRequiredService<IModelContextBuilder>();

                  var id = context.GetArgument<long>("oiaID");

                  var userContext = (context.UserContext as OmnikeeperUserContext)!;

                  // TODO: auth
                  //if (!authorizationService.CanUserUpdateLayer(userContext.User))
                  //    throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission");

                  using var transaction = modelContextBuilder.BuildDeferred();

                  var deleted = await OIAContextModel.Delete(id, transaction);
                  transaction.Commit();
                  userContext.Transaction = modelContextBuilder.BuildImmediate(); // HACK: so that later running parts of the graphql tree have a proper transaction object
                  return deleted != null;
              });


            FieldAsync<ODataAPIContextType>("upsertODataAPIContext",
                arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<UpsertODataAPIContextInputType>> { Name = "odataAPIContext" }
                ),
                resolve: async context =>
                {
                    var odataAPIContextModel = context.RequestServices.GetRequiredService<IODataAPIContextModel>();
                    var modelContextBuilder = context.RequestServices.GetRequiredService<IModelContextBuilder>();

                    var contextInput = context.GetArgument<UpsertODataAPIContextInput>("odataAPIContext");
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;

                    // TODO: auth
                    //if (!authorizationService.CanUserCreateLayer(userContext.User))
                    //    throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to create Layers");

                    using var transaction = modelContextBuilder.BuildDeferred();

                    try
                    {
                        var config = ODataAPIContext.ConfigSerializer.Deserialize(contextInput.Config);

                        var created = await odataAPIContextModel.Upsert(contextInput.ID, config, transaction);

                        transaction.Commit();
                        userContext.Transaction = modelContextBuilder.BuildImmediate(); // HACK: so that later running parts of the graphql tree have a proper transaction object

                        return created;
                    }
                    catch (Exception e)
                    {
                        throw new ExecutionError($"Could not parse configuration", e);
                    }
                });

            FieldAsync<BooleanGraphType>("deleteODataAPIContext",
              arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id" }
              ),
              resolve: async context =>
              {
                  var odataAPIContextModel = context.RequestServices.GetRequiredService<IODataAPIContextModel>();
                  var modelContextBuilder = context.RequestServices.GetRequiredService<IModelContextBuilder>();

                  var id = context.GetArgument<string>("id");

                  var userContext = (context.UserContext as OmnikeeperUserContext)!;

                  // TODO: auth
                  //if (!authorizationService.CanUserUpdateLayer(userContext.User))
                  //    throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission");

                  using var transaction = modelContextBuilder.BuildDeferred();

                  var deleted = await odataAPIContextModel.Delete(id, transaction);
                  transaction.Commit();
                  userContext.Transaction = modelContextBuilder.BuildImmediate(); // HACK: so that later running parts of the graphql tree have a proper transaction object
                  return deleted != null;
              });


            FieldAsync<StringGraphType>("setTraitSet",
                arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "traitSet" }
                ),
                resolve: async context =>
                {
                    var traitModel = context.RequestServices.GetRequiredService<IRecursiveTraitModel>();
                    var modelContextBuilder = context.RequestServices.GetRequiredService<IModelContextBuilder>();

                    var traitSetInput = context.GetArgument<string>("traitSet");
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;

                    // TODO: auth
                    //if (!authorizationService.CanUserCreateLayer(userContext.User))
                    //    throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to create Layers");

                    using var transaction = modelContextBuilder.BuildDeferred();

                    try
                    {
                        var traitSet = RecursiveTraitSet.Serializer.Deserialize(traitSetInput);

                        var created = await traitModel.SetRecursiveTraitSet(traitSet, transaction);

                        transaction.Commit();
                        userContext.Transaction = modelContextBuilder.BuildImmediate(); // HACK: so that later running parts of the graphql tree have a proper transaction object

                        return RecursiveTraitSet.Serializer.SerializeToString(created);
                    }
                    catch (Exception e)
                    {
                        throw new ExecutionError($"Could not parse traitSet", e);
                    }
                });


            FieldAsync<StringGraphType>("setBaseConfiguration",
                arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "baseConfiguration" }
                ),
                resolve: async context =>
                {
                    var baseConfigurationModel = context.RequestServices.GetRequiredService<IBaseConfigurationModel>();
                    var modelContextBuilder = context.RequestServices.GetRequiredService<IModelContextBuilder>();

                    var configStr = context.GetArgument<string>("baseConfiguration");
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;

                    // TODO: auth
                    //if (!authorizationService.CanUserCreateLayer(userContext.User))
                    //    throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to create Layers");

                    using var transaction = modelContextBuilder.BuildDeferred();

                    try
                    {
                        var config = BaseConfigurationV1.Serializer.Deserialize(configStr);
                        var created = await baseConfigurationModel.SetConfig(config, transaction);

                        transaction.Commit();
                        userContext.Transaction = modelContextBuilder.BuildImmediate(); // HACK: so that later running parts of the graphql tree have a proper transaction object

                        return BaseConfigurationV1.Serializer.SerializeToString(created);
                    }
                    catch (Exception e)
                    {
                        throw new ExecutionError($"Could not save base configuration", e);
                    }
                });

            FieldAsync<PredicateType>("upsertPredicate",
              arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<UpsertPredicateInputType>> { Name = "predicate" }
              ),
              resolve: async context =>
              {
                  var predicateModel = context.RequestServices.GetRequiredService<IPredicateModel>();
                  var modelContextBuilder = context.RequestServices.GetRequiredService<IModelContextBuilder>();
                  var managementAuthorizationService = context.RequestServices.GetRequiredService<IManagementAuthorizationService>();

                  var predicate = context.GetArgument<UpsertPredicateInput>("predicate");

                  var userContext = (context.UserContext as OmnikeeperUserContext)!;

                  if (!managementAuthorizationService.CanUserUpsertPredicate(userContext.User))
                      throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to update or insert Predicates");

                  using var transaction = modelContextBuilder.BuildDeferred();

                  var newPredicate = await predicateModel.InsertOrUpdate(predicate.ID, predicate.WordingFrom, predicate.WordingTo, predicate.State, predicate.Constraints, transaction);

                  transaction.Commit();
                  userContext.Transaction = modelContextBuilder.BuildImmediate(); // HACK: so that later running parts of the graphql tree have a proper transaction object

                  return newPredicate.predicate;
              });
        }
    }
}
