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
using System.Linq;

namespace Omnikeeper.GraphQL
{
    public partial class GraphQLMutation
    {
        public void CreateManage()
        {
            FieldAsync<LayerType>("manage_upsertLayer",
                arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<UpsertLayerInputType>> { Name = "layer" }
                ),
                resolve: async context =>
                {
                    var layerModel = context.RequestServices.GetRequiredService<ILayerModel>();
                    var modelContextBuilder = context.RequestServices.GetRequiredService<IModelContextBuilder>();

                    var upsertLayer = context.GetArgument<UpsertLayerInput>("layer");
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;

                    var managementAuthorizationService = context.RequestServices.GetRequiredService<IManagementAuthorizationService>();
                    if (!managementAuthorizationService.HasManagementPermission(userContext.User))
                        throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to create Layers");

                    using var transaction = modelContextBuilder.BuildDeferred();

                    ComputeLayerBrainLink clb = LayerModel.DefaultCLB;
                    if (upsertLayer.BrainName != null && upsertLayer.BrainName != "")
                        clb = ComputeLayerBrainLink.Build(upsertLayer.BrainName);
                    OnlineInboundAdapterLink oilp = LayerModel.DefaultOILP;
                    if (upsertLayer.OnlineInboundAdapterName != null && upsertLayer.OnlineInboundAdapterName != "")
                        oilp = OnlineInboundAdapterLink.Build(upsertLayer.OnlineInboundAdapterName);
                    var updatedLayer = await layerModel.UpsertLayer(upsertLayer.ID, upsertLayer.Description, Color.FromArgb(upsertLayer.Color), upsertLayer.State, clb, oilp, transaction);

                    transaction.Commit();
                    userContext.Transaction = modelContextBuilder.BuildImmediate(); // HACK: so that later running parts of the graphql tree have a proper transaction object

                    return updatedLayer;
                });


            FieldAsync<OIAContextType>("manage_createOIAContext",
                arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<CreateOIAContextInputType>> { Name = "oiaContext" }
                ),
                resolve: async context =>
                {
                    var OIAContextModel = context.RequestServices.GetRequiredService<IOIAContextModel>();
                    var modelContextBuilder = context.RequestServices.GetRequiredService<IModelContextBuilder>();

                    var configInput = context.GetArgument<CreateOIAContextInput>("oiaContext");
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;

                    var managementAuthorizationService = context.RequestServices.GetRequiredService<IManagementAuthorizationService>();
                    if (!managementAuthorizationService.HasManagementPermission(userContext.User))
                        throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to create OIAContext");

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
            FieldAsync<OIAContextType>("manage_updateOIAContext",
              arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<UpdateOIAContextInputType>> { Name = "oiaContext" }
              ),
              resolve: async context =>
              {
                  var OIAContextModel = context.RequestServices.GetRequiredService<IOIAContextModel>();
                  var modelContextBuilder = context.RequestServices.GetRequiredService<IModelContextBuilder>();

                  var configInput = context.GetArgument<UpdateOIAContextInput>("oiaContext");

                  var userContext = (context.UserContext as OmnikeeperUserContext)!;

                  var managementAuthorizationService = context.RequestServices.GetRequiredService<IManagementAuthorizationService>();
                  if (!managementAuthorizationService.HasManagementPermission(userContext.User))
                      throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to update OIAContext");

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
            FieldAsync<BooleanGraphType>("manage_deleteOIAContext",
              arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<LongGraphType>> { Name = "oiaID" }
              ),
              resolve: async context =>
              {
                  var OIAContextModel = context.RequestServices.GetRequiredService<IOIAContextModel>();
                  var modelContextBuilder = context.RequestServices.GetRequiredService<IModelContextBuilder>();

                  var id = context.GetArgument<long>("oiaID");

                  var userContext = (context.UserContext as OmnikeeperUserContext)!;

                  var managementAuthorizationService = context.RequestServices.GetRequiredService<IManagementAuthorizationService>();
                  if (!managementAuthorizationService.HasManagementPermission(userContext.User))
                      throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to delete OIAContext");

                  using var transaction = modelContextBuilder.BuildDeferred();

                  var deleted = await OIAContextModel.Delete(id, transaction);
                  transaction.Commit();
                  userContext.Transaction = modelContextBuilder.BuildImmediate(); // HACK: so that later running parts of the graphql tree have a proper transaction object
                  return deleted != null;
              });


            FieldAsync<ODataAPIContextType>("manage_upsertODataAPIContext",
                arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<UpsertODataAPIContextInputType>> { Name = "odataAPIContext" }
                ),
                resolve: async context =>
                {
                    var odataAPIContextModel = context.RequestServices.GetRequiredService<IODataAPIContextModel>();
                    var modelContextBuilder = context.RequestServices.GetRequiredService<IModelContextBuilder>();

                    var contextInput = context.GetArgument<UpsertODataAPIContextInput>("odataAPIContext");
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;

                    var managementAuthorizationService = context.RequestServices.GetRequiredService<IManagementAuthorizationService>();
                    if (!managementAuthorizationService.HasManagementPermission(userContext.User))
                        throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to modify ODataAPIContext");

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

            FieldAsync<BooleanGraphType>("manage_deleteODataAPIContext",
              arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id" }
              ),
              resolve: async context =>
              {
                  var odataAPIContextModel = context.RequestServices.GetRequiredService<IODataAPIContextModel>();
                  var modelContextBuilder = context.RequestServices.GetRequiredService<IModelContextBuilder>();

                  var id = context.GetArgument<string>("id");

                  var userContext = (context.UserContext as OmnikeeperUserContext)!;

                  var managementAuthorizationService = context.RequestServices.GetRequiredService<IManagementAuthorizationService>();
                  if (!managementAuthorizationService.HasManagementPermission(userContext.User))
                      throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to modify ODataAPIContext");

                  using var transaction = modelContextBuilder.BuildDeferred();

                  var deleted = await odataAPIContextModel.Delete(id, transaction);
                  transaction.Commit();
                  userContext.Transaction = modelContextBuilder.BuildImmediate(); // HACK: so that later running parts of the graphql tree have a proper transaction object
                  return deleted != null;
              });


            FieldAsync<BooleanGraphType>("manage_truncateLayer",
              arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id" }
              ),
              resolve: async context =>
              {
                  var baseAttributeRevisionistModel = context.RequestServices.GetRequiredService<IBaseAttributeRevisionistModel>();
                  var baseRelationRevisionistModel = context.RequestServices.GetRequiredService<IBaseRelationRevisionistModel>();
                  var modelContextBuilder = context.RequestServices.GetRequiredService<IModelContextBuilder>();

                  var id = context.GetArgument<string>("id");

                  var userContext = (context.UserContext as OmnikeeperUserContext)!;

                  var managementAuthorizationService = context.RequestServices.GetRequiredService<IManagementAuthorizationService>();
                  if (!managementAuthorizationService.HasManagementPermission(userContext.User))
                      throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to manage layer");
                  var layerBasedAuthorizationService = context.RequestServices.GetRequiredService<ILayerBasedAuthorizationService>();
                  if (!layerBasedAuthorizationService.CanUserWriteToLayer(userContext.User, id))
                      throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to modify layer");

                  using var transaction = modelContextBuilder.BuildDeferred();
                  var numDeletedAttributes = await baseAttributeRevisionistModel.DeleteAllAttributes(id, transaction);
                  var numDeletedRelations = await baseRelationRevisionistModel.DeleteAllRelations(id, transaction);
                  transaction.Commit();
                  return true;
              });

            FieldAsync<StringGraphType>("manage_setBaseConfiguration",
                arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "baseConfiguration" }
                ),
                resolve: async context =>
                {
                    var baseConfigurationModel = context.RequestServices.GetRequiredService<IBaseConfigurationModel>();
                    var modelContextBuilder = context.RequestServices.GetRequiredService<IModelContextBuilder>();

                    var configStr = context.GetArgument<string>("baseConfiguration");
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;

                    var managementAuthorizationService = context.RequestServices.GetRequiredService<IManagementAuthorizationService>();
                    if (!managementAuthorizationService.HasManagementPermission(userContext.User))
                        throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to set base configuration");

                    using var transaction = modelContextBuilder.BuildDeferred();

                    try
                    {
                        var config = BaseConfigurationV1.Serializer.Deserialize(configStr);

                        IDValidations.ValidateLayerIDThrow(config.ConfigWriteLayer);
                        IDValidations.ValidateLayerIDsThrow(config.ConfigLayerset);

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

            FieldAsync<PredicateType>("manage_upsertPredicate",
              arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<UpsertPredicateInputType>> { Name = "predicate" }
              ),
              resolve: async context =>
              {
                  //var predicateWriteService = context.RequestServices.GetRequiredService<IPredicateWriteService>();
                  var predicateModel = context.RequestServices.GetRequiredService<IPredicateModel>();
                  var modelContextBuilder = context.RequestServices.GetRequiredService<IModelContextBuilder>();
                  var changesetModel = context.RequestServices.GetRequiredService<IChangesetModel>();
                  var baseConfigurationModel = context.RequestServices.GetRequiredService<IBaseConfigurationModel>();

                  var predicate = context.GetArgument<UpsertPredicateInput>("predicate")!;

                  var userContext = (context.UserContext as OmnikeeperUserContext)!;

                  using var transaction = modelContextBuilder.BuildDeferred();

                  var baseConfiguration = await baseConfigurationModel.GetConfigOrDefault(transaction);
                  if (!context.RequestServices.GetRequiredService<IManagementAuthorizationService>().CanModifyManagement(userContext.User, baseConfiguration, out var message))
                      throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to modify predicates: {message}");

                  var changesetProxy = new ChangesetProxy(userContext.User.InDatabase, TimeThreshold.BuildLatest(), changesetModel);
                  var newPredicate = await predicateModel.InsertOrUpdate(predicate.ID, predicate.WordingFrom, predicate.WordingTo, new LayerSet(baseConfiguration.ConfigLayerset), baseConfiguration.ConfigWriteLayer, new Base.Entity.DataOrigin.DataOriginV1(Base.Entity.DataOrigin.DataOriginType.Manual),
                      changesetProxy, transaction);

                  transaction.Commit();
                  userContext.Transaction = modelContextBuilder.BuildImmediate(); // HACK: so that later running parts of the graphql tree have a proper transaction object

                  return newPredicate.predicate;
              });


            FieldAsync<BooleanGraphType>("manage_removePredicate",
              arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "predicateID" }
              ),
              resolve: async context =>
              {
                  var predicateModel = context.RequestServices.GetRequiredService<IPredicateModel>();
                  var modelContextBuilder = context.RequestServices.GetRequiredService<IModelContextBuilder>();
                  var changesetModel = context.RequestServices.GetRequiredService<IChangesetModel>();
                  var baseConfigurationModel = context.RequestServices.GetRequiredService<IBaseConfigurationModel>();

                  var predicateID = context.GetArgument<string>("predicateID");

                  var userContext = (context.UserContext as OmnikeeperUserContext)!;

                  using var transaction = modelContextBuilder.BuildDeferred();

                  var baseConfiguration = await baseConfigurationModel.GetConfigOrDefault(transaction);
                  if (!context.RequestServices.GetRequiredService<IManagementAuthorizationService>().CanModifyManagement(userContext.User, baseConfiguration, out var message))
                      throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to modify predicates: {message}");

                  var changesetProxy = new ChangesetProxy(userContext.User.InDatabase, TimeThreshold.BuildLatest(), changesetModel);

                  var deleted = await predicateModel.TryToDelete(predicateID, new LayerSet(baseConfiguration.ConfigLayerset), baseConfiguration.ConfigWriteLayer,
                      new Base.Entity.DataOrigin.DataOriginV1(Base.Entity.DataOrigin.DataOriginType.Manual), changesetProxy, transaction);

                  transaction.Commit();
                  userContext.Transaction = modelContextBuilder.BuildImmediate(); // HACK: so that later running parts of the graphql tree have a proper transaction object

                  return deleted;
              });

            FieldAsync<RecursiveTraitType>("manage_upsertRecursiveTrait",
              arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<UpsertRecursiveTraitInputType>> { Name = "trait" }
              ),
              resolve: async context =>
              {
                  var traitModel = context.RequestServices.GetRequiredService<IRecursiveDataTraitModel>();
                  var modelContextBuilder = context.RequestServices.GetRequiredService<IModelContextBuilder>();
                  var changesetModel = context.RequestServices.GetRequiredService<IChangesetModel>();
                  var baseConfigurationModel = context.RequestServices.GetRequiredService<IBaseConfigurationModel>();

                  var trait = context.GetArgument<UpsertRecursiveTraitInput>("trait");

                  var requiredAttributes = trait.RequiredAttributes.Select(str => TraitAttribute.Serializer.Deserialize(str));
                  var optionalAttributes = trait.OptionalAttributes?.Select(str => TraitAttribute.Serializer.Deserialize(str));
                  var requiredRelations = trait.RequiredRelations?.Select(str => TraitRelation.Serializer.Deserialize(str));

                  var userContext = (context.UserContext as OmnikeeperUserContext)!;

                  using var transaction = modelContextBuilder.BuildDeferred();

                  var baseConfiguration = await baseConfigurationModel.GetConfigOrDefault(transaction);
                  if (!context.RequestServices.GetRequiredService<IManagementAuthorizationService>().CanModifyManagement(userContext.User, baseConfiguration, out var message))
                      throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to modify traits: {message}");

                  var changesetProxy = new ChangesetProxy(userContext.User.InDatabase, TimeThreshold.BuildLatest(), changesetModel);

                  var newTrait = await traitModel.InsertOrUpdate(
                      trait.ID, requiredAttributes, optionalAttributes, requiredRelations, trait.RequiredTraits,
                      new LayerSet(baseConfiguration.ConfigLayerset), baseConfiguration.ConfigWriteLayer,
                      new Base.Entity.DataOrigin.DataOriginV1(Base.Entity.DataOrigin.DataOriginType.Manual),
                      changesetProxy, transaction);

                  transaction.Commit();
                  userContext.Transaction = modelContextBuilder.BuildImmediate(); // HACK: so that later running parts of the graphql tree have a proper transaction object

                  return newTrait.recursiveTrait;
              });

            FieldAsync<BooleanGraphType>("manage_removeRecursiveTrait",
              arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id" }
              ),
              resolve: async context =>
              {
                  var traitModel = context.RequestServices.GetRequiredService<IRecursiveDataTraitModel>();
                  var modelContextBuilder = context.RequestServices.GetRequiredService<IModelContextBuilder>();
                  var changesetModel = context.RequestServices.GetRequiredService<IChangesetModel>();
                  var baseConfigurationModel = context.RequestServices.GetRequiredService<IBaseConfigurationModel>();

                  var traitID = context.GetArgument<string>("id");

                  var userContext = (context.UserContext as OmnikeeperUserContext)!;

                  using var transaction = modelContextBuilder.BuildDeferred();

                  var baseConfiguration = await baseConfigurationModel.GetConfigOrDefault(transaction);
                  if (!context.RequestServices.GetRequiredService<IManagementAuthorizationService>().CanModifyManagement(userContext.User, baseConfiguration, out var message))
                      throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to modify traits: {message}");

                  var changesetProxy = new ChangesetProxy(userContext.User.InDatabase, TimeThreshold.BuildLatest(), changesetModel);

                  var deleted = await traitModel.TryToDelete(traitID, new LayerSet(baseConfiguration.ConfigLayerset), baseConfiguration.ConfigWriteLayer, 
                      new Base.Entity.DataOrigin.DataOriginV1(Base.Entity.DataOrigin.DataOriginType.Manual), changesetProxy, transaction);

                  transaction.Commit();
                  userContext.Transaction = modelContextBuilder.BuildImmediate(); // HACK: so that later running parts of the graphql tree have a proper transaction object

                  return deleted;
              });

            FieldAsync<AuthRoleType>("manage_upsertAuthRole",
              arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<UpsertAuthRoleInputType>> { Name = "authRole" }
              ),
              resolve: async context =>
              {
                  var authRoleModel = context.RequestServices.GetRequiredService<IAuthRoleModel>();
                  var modelContextBuilder = context.RequestServices.GetRequiredService<IModelContextBuilder>();
                  var changesetModel = context.RequestServices.GetRequiredService<IChangesetModel>();
                  var baseConfigurationModel = context.RequestServices.GetRequiredService<IBaseConfigurationModel>();

                  var authRole = context.GetArgument<UpsertAuthRoleInput>("authRole");

                  var userContext = (context.UserContext as OmnikeeperUserContext)!;

                  using var transaction = modelContextBuilder.BuildDeferred();

                  var baseConfiguration = await baseConfigurationModel.GetConfigOrDefault(transaction);
                  if (!context.RequestServices.GetRequiredService<IManagementAuthorizationService>().CanModifyManagement(userContext.User, baseConfiguration, out var message))
                      throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to modify auth roles: {message}");

                  var changesetProxy = new ChangesetProxy(userContext.User.InDatabase, TimeThreshold.BuildLatest(), changesetModel);

                  var newAuthRole = await authRoleModel.InsertOrUpdate(
                      authRole.ID, authRole.Permissions,
                      new LayerSet(baseConfiguration.ConfigLayerset), baseConfiguration.ConfigWriteLayer,
                      new Base.Entity.DataOrigin.DataOriginV1(Base.Entity.DataOrigin.DataOriginType.Manual),
                      changesetProxy, transaction);

                  transaction.Commit();
                  userContext.Transaction = modelContextBuilder.BuildImmediate(); // HACK: so that later running parts of the graphql tree have a proper transaction object

                  return newAuthRole.authRole;
              });

            FieldAsync<BooleanGraphType>("manage_removeAuthRole",
              arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id" }
              ),
              resolve: async context =>
              {
                  var authRoleModel = context.RequestServices.GetRequiredService<IAuthRoleModel>();
                  var modelContextBuilder = context.RequestServices.GetRequiredService<IModelContextBuilder>();
                  var changesetModel = context.RequestServices.GetRequiredService<IChangesetModel>();
                  var baseConfigurationModel = context.RequestServices.GetRequiredService<IBaseConfigurationModel>();

                  var authRoleID = context.GetArgument<string>("id");

                  var userContext = (context.UserContext as OmnikeeperUserContext)!;

                  using var transaction = modelContextBuilder.BuildDeferred();

                  var baseConfiguration = await baseConfigurationModel.GetConfigOrDefault(transaction);
                  if (!context.RequestServices.GetRequiredService<IManagementAuthorizationService>().CanModifyManagement(userContext.User, baseConfiguration, out var message))
                      throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to modify auth roles: {message}");

                  var changesetProxy = new ChangesetProxy(userContext.User.InDatabase, TimeThreshold.BuildLatest(), changesetModel);

                  var deleted = await authRoleModel.TryToDelete(authRoleID,
                      new LayerSet(baseConfiguration.ConfigLayerset), baseConfiguration.ConfigWriteLayer,
                      new Base.Entity.DataOrigin.DataOriginV1(Base.Entity.DataOrigin.DataOriginType.Manual), changesetProxy, transaction);

                  transaction.Commit();
                  userContext.Transaction = modelContextBuilder.BuildImmediate(); // HACK: so that later running parts of the graphql tree have a proper transaction object

                  return deleted;
              });
        }
    }
}
