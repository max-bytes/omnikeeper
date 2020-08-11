using GraphQL;
using GraphQL.Types;
using Landscape.Base.Entity;
using Landscape.Base.Inbound;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using LandscapeRegistry.Entity.AttributeValues;
using LandscapeRegistry.Model;
using LandscapeRegistry.Service;
using Newtonsoft.Json;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace LandscapeRegistry.GraphQL
{
    public class RegistryMutation : ObjectGraphType
    {
        public RegistryMutation(ICIModel ciModel, IAttributeModel attributeModel, ILayerModel layerModel, IRelationModel relationModel, IOIAConfigModel OIAConfigModel,
            IChangesetModel changesetModel, IPredicateModel predicateModel, IRegistryAuthorizationService authorizationService, NpgsqlConnection conn)
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

                    var userContext = context.UserContext as RegistryUserContext;

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

                            insertedAttributes.Add(await attributeModel.InsertAttribute(attribute.Name, nonGenericAttributeValue, attribute.LayerID, ciIdentity, changeset, transaction));
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
                            removedAttributes.Add(await attributeModel.RemoveAttribute(attribute.Name, attribute.LayerID, ciIdentity, changeset, transaction));
                        }
                    }

                    var insertedRelations = new List<Relation>();
                    foreach (var insertRelation in insertRelations)
                    {
                        insertedRelations.Add(await relationModel.InsertRelation(insertRelation.FromCIID, insertRelation.ToCIID, insertRelation.PredicateID, insertRelation.LayerID, changeset, transaction));
                    }

                    var removedRelations = new List<Relation>();
                    foreach (var removeRelation in removeRelations)
                    {
                        removedRelations.Add(await relationModel.RemoveRelation(removeRelation.FromCIID, removeRelation.ToCIID, removeRelation.PredicateID, removeRelation.LayerID, changeset, transaction));
                    }

                    IEnumerable<MergedCI> affectedCIs = null;
                    if (userContext.LayerSet != null)
                    {
                        var affectedCIIDs = removedAttributes.Select(r => r.CIID)
                        .Concat(insertedAttributes.Select(i => i.CIID))
                        .Concat(insertedRelations.SelectMany(i => new Guid[] { i.FromCIID, i.ToCIID }))
                        .Concat(removedRelations.SelectMany(i => new Guid[] { i.FromCIID, i.ToCIID }))
                        .Distinct();
                        affectedCIs = await ciModel.GetMergedCIs(userContext.LayerSet, new MultiCIIDsSelection(affectedCIIDs), true, transaction, userContext.TimeThreshold);
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

                    var userContext = context.UserContext as RegistryUserContext;

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

                        await attributeModel.InsertCINameAttribute(ci.Name, ci.LayerIDForName, ciid, changeset, transaction);

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
                    var userContext = context.UserContext as RegistryUserContext;

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

                  var userContext = context.UserContext as RegistryUserContext;

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


            FieldAsync<OIAConfigType>("createOIAConfig",
                arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<CreateOIAConfigInputType>> { Name = "oiaConfig" }
                ),
                resolve: async context =>
                {
                    var configInput = context.GetArgument<CreateOIAConfigInput>("oiaConfig");
                    var userContext = context.UserContext as RegistryUserContext;

                    // TODO: auth
                    //if (!authorizationService.CanUserCreateLayer(userContext.User))
                    //    throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to create Layers");

                    using var transaction = await conn.BeginTransactionAsync();
                    userContext.Transaction = transaction;

                    try {
                        var config = JsonConvert.DeserializeObject<IOnlineInboundAdapter.IConfig>(configInput.Config, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Objects });

                        var createdOIAConfig = await OIAConfigModel.Create(configInput.Name, config, transaction);

                        await transaction.CommitAsync();

                        return createdOIAConfig;
                    }
                    catch (Exception e)
                    {
                        throw new ExecutionError($"Could not parse configuration", e);
                    }
                });
            FieldAsync<OIAConfigType>("updateOIAConfig",
              arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<UpdateOIAConfigInputType>> { Name = "oiaConfig" }
              ),
              resolve: async context =>
              {
                  var configInput = context.GetArgument<UpdateOIAConfigInput>("oiaConfig");

                  var userContext = context.UserContext as RegistryUserContext;

                  // TODO: auth
                  //if (!authorizationService.CanUserUpdateLayer(userContext.User))
                  //    throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to update Layers");

                  using var transaction = await conn.BeginTransactionAsync();
                  userContext.Transaction = transaction;

                  try
                  {
                      var config = JsonConvert.DeserializeObject<IOnlineInboundAdapter.IConfig>(configInput.Config, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Objects });
                      var oiaConfig = await OIAConfigModel.Update(configInput.ID, configInput.Name, config, transaction);
                      await transaction.CommitAsync();

                      return oiaConfig;
                  } catch (Exception e)
                  {
                      throw new ExecutionError($"Could not parse configuration", e);
                  }
              });
            FieldAsync<BooleanGraphType>("deleteOIAConfig",
              arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<LongGraphType>> { Name = "oiaID" }
              ),
              resolve: async context =>
              {
                  var id = context.GetArgument<long>("oiaID");

                  var userContext = context.UserContext as RegistryUserContext;

                  // TODO: auth
                  //if (!authorizationService.CanUserUpdateLayer(userContext.User))
                  //    throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to update Layers");

                  using var transaction = await conn.BeginTransactionAsync();
                  userContext.Transaction = transaction;

                  var success = await OIAConfigModel.Delete(id, transaction);
                  await transaction.CommitAsync();
                  return success;
              });

            FieldAsync<PredicateType>("upsertPredicate",
              arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<UpsertPredicateInputType>> { Name = "predicate" }
              ),
              resolve: async context =>
              {
                  var predicate = context.GetArgument<UpsertPredicateInput>("predicate");

                  var userContext = context.UserContext as RegistryUserContext;

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
