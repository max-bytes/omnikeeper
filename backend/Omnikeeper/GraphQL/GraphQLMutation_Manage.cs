using GraphQL;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.Config;
using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Model;
using System;
using System.Drawing;

namespace Omnikeeper.GraphQL
{
    public partial class GraphQLMutation
    {
        public void CreateManage()
        {
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


            FieldAsync<BooleanGraphType>("truncateLayer",
              arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<LongGraphType>> { Name = "id" }
              ),
              resolve: async context =>
              {
                  var baseAttributeRevisionistModel = context.RequestServices.GetRequiredService<IBaseAttributeRevisionistModel>();
                  var baseRelationRevisionistModel = context.RequestServices.GetRequiredService<IBaseRelationRevisionistModel>();
                  var modelContextBuilder = context.RequestServices.GetRequiredService<IModelContextBuilder>();

                  var id = context.GetArgument<long>("id");

                  // TODO: auth
                  //if (!authorizationService.CanUserUpdateLayer(userContext.User))
                  //    throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission");

                  using var transaction = modelContextBuilder.BuildDeferred();
                  var numDeletedAttributes = await baseAttributeRevisionistModel.DeleteAllAttributes(id, transaction);
                  var numDeletedRelations = await baseRelationRevisionistModel.DeleteAllRelations(id, transaction);
                  transaction.Commit();
                  return true;
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
                  var predicateWriteService = context.RequestServices.GetRequiredService<IPredicateWriteService>();
                  var modelContextBuilder = context.RequestServices.GetRequiredService<IModelContextBuilder>();
                  var changesetModel = context.RequestServices.GetRequiredService<IChangesetModel>();
                  var managementAuthorizationService = context.RequestServices.GetRequiredService<IManagementAuthorizationService>();

                  var predicate = context.GetArgument<UpsertPredicateInput>("predicate");

                  var userContext = (context.UserContext as OmnikeeperUserContext)!;

                  using var transaction = modelContextBuilder.BuildDeferred();

                  var changesetProxy = new ChangesetProxy(userContext.User.InDatabase, TimeThreshold.BuildLatest(), changesetModel);

                  var newPredicate = await predicateWriteService.InsertOrUpdate(
                      predicate.ID, predicate.WordingFrom, predicate.WordingTo, 
                      predicate.Constraints, new Base.Entity.DataOrigin.DataOriginV1(Base.Entity.DataOrigin.DataOriginType.Manual),
                      changesetProxy, userContext.User, transaction);

                  transaction.Commit();
                  userContext.Transaction = modelContextBuilder.BuildImmediate(); // HACK: so that later running parts of the graphql tree have a proper transaction object

                  return newPredicate.predicate;
              });


            FieldAsync<BooleanGraphType>("removePredicate",
              arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "predicateID" }
              ),
              resolve: async context =>
              {
                  var predicateWriteService = context.RequestServices.GetRequiredService<IPredicateWriteService>();
                  var modelContextBuilder = context.RequestServices.GetRequiredService<IModelContextBuilder>();
                  var changesetModel = context.RequestServices.GetRequiredService<IChangesetModel>();

                  var predicateID = context.GetArgument<string>("predicateID");

                  var userContext = (context.UserContext as OmnikeeperUserContext)!;

                  using var transaction = modelContextBuilder.BuildDeferred();

                  var changesetProxy = new ChangesetProxy(userContext.User.InDatabase, TimeThreshold.BuildLatest(), changesetModel);

                  var deleted = await predicateWriteService.TryToDelete(predicateID, changesetProxy, userContext.User, transaction);

                  transaction.Commit();
                  userContext.Transaction = modelContextBuilder.BuildImmediate(); // HACK: so that later running parts of the graphql tree have a proper transaction object

                  return deleted;
              });
        }
    }
}
