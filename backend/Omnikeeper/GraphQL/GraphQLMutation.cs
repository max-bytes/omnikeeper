using GraphQL;
using GraphQL.Types;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.Model;
using Omnikeeper.Service;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Omnikeeper.GraphQL
{
    public class GraphQLMutation : ObjectGraphType
    {
        public GraphQLMutation(ICIModel ciModel, IBaseAttributeModel attributeModel, ILayerModel layerModel, IRelationModel relationModel, IOIAContextModel OIAContextModel,
             IODataAPIContextModel odataAPIContextModel, IChangesetModel changesetModel, IPredicateModel predicateModel, IRecursiveTraitModel traitModel,
             IOmnikeeperAuthorizationService authorizationService, NpgsqlConnection conn)
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
                    var layers = context.GetArgument<string[]>("layers", null);
                    var insertAttributes = context.GetArgument("InsertAttributes", new List<InsertCIAttributeInput>());
                    var removeAttributes = context.GetArgument("RemoveAttributes", new List<RemoveCIAttributeInput>());
                    var insertRelations = context.GetArgument("InsertRelations", new List<InsertRelationInput>());
                    var removeRelations = context.GetArgument("RemoveRelations", new List<RemoveRelationInput>());

                    var userContext = context.UserContext as OmnikeeperUserContext;

                    var writeLayerIDs = insertAttributes.Select(a => a.LayerID)
                    .Concat(removeAttributes.Select(a => a.LayerID))
                    .Concat(insertRelations.Select(a => a.LayerID))
                    .Concat(removeRelations.Select(a => a.LayerID));
                    if (!authorizationService.CanUserWriteToLayers(userContext.User, writeLayerIDs))
                        throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to write to at least one of the following layerIDs: {string.Join(',', writeLayerIDs)}");

                    using var transaction = await conn.BeginTransactionAsync();
                    userContext.Transaction = transaction;
                    userContext.LayerSet = layers != null ? await layerModel.BuildLayerSet(layers, transaction) : null;
                    userContext.TimeThreshold = TimeThreshold.BuildLatest();

                    var changeset = ChangesetProxy.Build(userContext.User.InDatabase, userContext.TimeThreshold.Time, changesetModel); //await changesetModel.CreateChangeset(userContext.User.InDatabase.ID, transaction, userContext.TimeThreshold.Time);

                    var groupedInsertAttributes = insertAttributes.GroupBy(a => a.CI);
                    var insertedAttributes = new List<CIAttribute>();
                    foreach (var attributeGroup in groupedInsertAttributes)
                    {
                        // look for ciid
                        var ciIdentity = attributeGroup.Key;
                        foreach (var attribute in attributeGroup)
                        {
                            var nonGenericAttributeValue = AttributeValueBuilder.Build(attribute.Value);

                            var (a, changed) = await attributeModel.InsertAttribute(attribute.Name, nonGenericAttributeValue, ciIdentity, attribute.LayerID, changeset, transaction);
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
                        var (r, changed) = await relationModel.InsertRelation(insertRelation.FromCIID, insertRelation.ToCIID, insertRelation.PredicateID, insertRelation.LayerID, changeset, transaction);
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

                    await transaction.CommitAsync();

                    return MutateReturn.Build(insertedAttributes, removedAttributes, insertedRelations, affectedCIs);
                });

            FieldAsync<CreateCIsReturnType>("createCIs",
                arguments: new QueryArguments(
                new QueryArgument<ListGraphType<CreateCIInputType>> { Name = "cis" }
                ),
                resolve: async context =>
                {
                    var createCIs = context.GetArgument("cis", new List<CreateCIInput>());

                    var userContext = context.UserContext as OmnikeeperUserContext;

                    if (!authorizationService.CanUserCreateCI(userContext.User))
                        throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to create CIs");
                    if (!authorizationService.CanUserWriteToLayers(userContext.User, createCIs.Select(ci => ci.LayerIDForName)))
                        throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to write to at least one of the following layerIDs: {string.Join(',', createCIs.Select(ci => ci.LayerIDForName))}");

                    using var transaction = await conn.BeginTransactionAsync();
                    userContext.Transaction = transaction;
                    userContext.TimeThreshold = TimeThreshold.BuildLatest();

                    var changeset = ChangesetProxy.Build(userContext.User.InDatabase, userContext.TimeThreshold.Time, changesetModel);

                    var createdCIIDs = new List<Guid>();
                    foreach (var ci in createCIs)
                    {
                        Guid ciid = await ciModel.CreateCI(transaction);

                        await attributeModel.InsertCINameAttribute(ci.Name, ciid, ci.LayerIDForName, changeset, transaction);

                        createdCIIDs.Add(ciid);
                    }
                    await transaction.CommitAsync();

                    return CreateCIsReturn.Build(createdCIIDs);
                });

            FieldAsync<LayerType>("createLayer",
                arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<CreateLayerInputType>> { Name = "layer" }
                ),
                resolve: async context =>
                {
                    var createLayer = context.GetArgument<CreateLayerInput>("layer");
                    var userContext = context.UserContext as OmnikeeperUserContext;

                    if (!authorizationService.CanUserCreateLayer(userContext.User))
                        throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to create Layers");

                    using var transaction = await conn.BeginTransactionAsync();
                    userContext.Transaction = transaction;

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

                    await transaction.CommitAsync();

                    return createdLayer;
                });
            FieldAsync<LayerType>("updateLayer",
              arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<UpdateLayerInputType>> { Name = "layer" }
              ),
              resolve: async context =>
              {
                  var layer = context.GetArgument<UpdateLayerInput>("layer");

                  var userContext = context.UserContext as OmnikeeperUserContext;

                  if (!authorizationService.CanUserUpdateLayer(userContext.User))
                      throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to update Layers");

                  using var transaction = await conn.BeginTransactionAsync();
                  userContext.Transaction = transaction;

                  var clb = ComputeLayerBrainLink.Build(layer.BrainName);
                  var oilp = OnlineInboundAdapterLink.Build(layer.OnlineInboundAdapterName);
                  var updatedLayer = await layerModel.Update(layer.ID, Color.FromArgb(layer.Color), layer.State, clb, oilp, transaction);
                  await transaction.CommitAsync();

                  return updatedLayer;
              });


            FieldAsync<OIAContextType>("createOIAContext",
                arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<CreateOIAContextInputType>> { Name = "oiaContext" }
                ),
                resolve: async context =>
                {
                    var configInput = context.GetArgument<CreateOIAContextInput>("oiaContext");
                    var userContext = context.UserContext as OmnikeeperUserContext;

                    // TODO: auth
                    //if (!authorizationService.CanUserCreateLayer(userContext.User))
                    //    throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to create Layers");

                    using var transaction = await conn.BeginTransactionAsync();
                    userContext.Transaction = transaction;

                    try
                    {
                        var config = IOnlineInboundAdapter.IConfig.Serializer.Deserialize(configInput.Config);

                        var createdOIAContext = await OIAContextModel.Create(configInput.Name, config, transaction);

                        await transaction.CommitAsync();

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
                  var configInput = context.GetArgument<UpdateOIAContextInput>("oiaContext");

                  var userContext = context.UserContext as OmnikeeperUserContext;

                  // TODO: auth
                  //if (!authorizationService.CanUserUpdateLayer(userContext.User))
                  //    throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to update Layers");

                  using var transaction = await conn.BeginTransactionAsync();
                  userContext.Transaction = transaction;

                  try
                  {
                      var config = IOnlineInboundAdapter.IConfig.Serializer.Deserialize(configInput.Config);
                      var oiaContext = await OIAContextModel.Update(configInput.ID, configInput.Name, config, transaction);
                      await transaction.CommitAsync();

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
                  var id = context.GetArgument<long>("oiaID");

                  var userContext = context.UserContext as OmnikeeperUserContext;

                  // TODO: auth
                  //if (!authorizationService.CanUserUpdateLayer(userContext.User))
                  //    throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission");

                  using var transaction = await conn.BeginTransactionAsync();
                  userContext.Transaction = transaction;

                  var deleted = await OIAContextModel.Delete(id, transaction);
                  await transaction.CommitAsync();
                  return deleted != null;
              });


            FieldAsync<ODataAPIContextType>("upsertODataAPIContext",
                arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<UpsertODataAPIContextInputType>> { Name = "odataAPIContext" }
                ),
                resolve: async context =>
                {
                    var contextInput = context.GetArgument<UpsertODataAPIContextInput>("odataAPIContext");
                    var userContext = context.UserContext as OmnikeeperUserContext;

                    // TODO: auth
                    //if (!authorizationService.CanUserCreateLayer(userContext.User))
                    //    throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to create Layers");

                    using var transaction = await conn.BeginTransactionAsync();
                    userContext.Transaction = transaction;

                    try
                    {
                        var config = ODataAPIContext.ConfigSerializer.Deserialize(contextInput.Config);

                        var created = await odataAPIContextModel.Upsert(contextInput.ID, config, transaction);

                        await transaction.CommitAsync();

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
                  var id = context.GetArgument<string>("id");

                  var userContext = context.UserContext as OmnikeeperUserContext;

                  // TODO: auth
                  //if (!authorizationService.CanUserUpdateLayer(userContext.User))
                  //    throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission");

                  using var transaction = await conn.BeginTransactionAsync();
                  userContext.Transaction = transaction;

                  var deleted = await odataAPIContextModel.Delete(id, transaction);
                  await transaction.CommitAsync();
                  return deleted != null;
              });


            FieldAsync<StringGraphType>("setTraitSet",
                arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "traitSet" }
                ),
                resolve: async context =>
                {
                    var traitSetInput = context.GetArgument<string>("traitSet");
                    var userContext = context.UserContext as OmnikeeperUserContext;

                    // TODO: auth
                    //if (!authorizationService.CanUserCreateLayer(userContext.User))
                    //    throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to create Layers");

                    using var transaction = await conn.BeginTransactionAsync();
                    userContext.Transaction = transaction;

                    try
                    {
                        var traitSet = TraitsProvider.TraitSetSerializer.Deserialize(traitSetInput);

                        var created = await traitModel.SetRecursiveTraitSet(traitSet, transaction);

                        await transaction.CommitAsync();

                        return TraitsProvider.TraitSetSerializer.SerializeToString(created);
                    }
                    catch (Exception e)
                    {
                        throw new ExecutionError($"Could not parse traitSet", e);
                    }
                });


            FieldAsync<PredicateType>("upsertPredicate",
              arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<UpsertPredicateInputType>> { Name = "predicate" }
              ),
              resolve: async context =>
              {
                  var predicate = context.GetArgument<UpsertPredicateInput>("predicate");

                  var userContext = context.UserContext as OmnikeeperUserContext;

                  if (!authorizationService.CanUserUpsertPredicate(userContext.User))
                      throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to update or insert Predicates");

                  using var transaction = await conn.BeginTransactionAsync();
                  userContext.Transaction = transaction;

                  var newPredicate = await predicateModel.InsertOrUpdate(predicate.ID, predicate.WordingFrom, predicate.WordingTo, predicate.State, predicate.Constraints, transaction);
                  await transaction.CommitAsync();

                  return newPredicate;
              });
        }
    }
}
