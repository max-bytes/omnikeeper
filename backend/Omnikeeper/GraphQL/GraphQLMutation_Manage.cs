using GraphQL;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
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
        private void CheckManagementPermissionThrow(OmnikeeperUserContext userContext, string reasonForCheck)
        {
            if (!managementAuthorizationService.HasManagementPermission(userContext.User))
                throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to {reasonForCheck}");
        }

        private void CheckModifyManagementThrow(OmnikeeperUserContext userContext, MetaConfiguration metaConfiguration, string reasonForCheck)
        {
            if (!managementAuthorizationService.CanModifyManagement(userContext.User, metaConfiguration, out var message))
                throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to {reasonForCheck}: {message}");
        }

        public void CreateManage()
        {
            FieldAsync<LayerType>("manage_upsertLayer",
                arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<UpsertLayerInputType>> { Name = "layer" }
                ),
                resolve: async context =>
                {
                    var userContext = context.SetupUserContext()
                        .WithTransaction(modelContextBuilder => modelContextBuilder.BuildDeferred())
                        .WithTimeThreshold(() => TimeThreshold.BuildLatest());

                    var upsertLayer = context.GetArgument<UpsertLayerInput>("layer")!;

                    CheckManagementPermissionThrow(userContext, "modify layers");

                    string clConfigID = "";
                    if (upsertLayer.CLConfigID != null && upsertLayer.CLConfigID != "")
                        clConfigID = upsertLayer.CLConfigID;
                    OnlineInboundAdapterLink oilp = LayerModel.DefaultOILP;
                    if (upsertLayer.OnlineInboundAdapterName != null && upsertLayer.OnlineInboundAdapterName != "")
                        oilp = OnlineInboundAdapterLink.Build(upsertLayer.OnlineInboundAdapterName);
                    var updatedLayer = await layerModel.UpsertLayer(upsertLayer.ID, upsertLayer.Description, Color.FromArgb(upsertLayer.Color), upsertLayer.State, clConfigID, oilp, upsertLayer.Generators, userContext.Transaction);

                    userContext.CommitAndStartNewTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate());

                    return updatedLayer;
                });


            FieldAsync<OIAContextType>("manage_createOIAContext",
                arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<CreateOIAContextInputType>> { Name = "oiaContext" }
                ),
                resolve: async context =>
                {
                    var userContext = context.SetupUserContext()
                        .WithTransaction(modelContextBuilder => modelContextBuilder.BuildDeferred())
                        .WithTimeThreshold(() => TimeThreshold.BuildLatest());

                    var configInput = context.GetArgument<CreateOIAContextInput>("oiaContext")!;

                    CheckManagementPermissionThrow(userContext, "create OIAContext");

                    try
                    {
                        var config = IOnlineInboundAdapter.IConfig.Serializer.Deserialize(configInput.Config);
                        var createdOIAContext = await oiaContextModel.Create(configInput.Name, config, userContext.Transaction);
                        userContext.CommitAndStartNewTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate());

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
                  var userContext = context.SetupUserContext()
                      .WithTransaction(modelContextBuilder => modelContextBuilder.BuildDeferred())
                      .WithTimeThreshold(() => TimeThreshold.BuildLatest());

                  var configInput = context.GetArgument<UpdateOIAContextInput>("oiaContext")!;

                  CheckManagementPermissionThrow(userContext, "update OIAContext");

                  try
                  {
                      var config = IOnlineInboundAdapter.IConfig.Serializer.Deserialize(configInput.Config);
                      var oiaContext = await oiaContextModel.Update(configInput.ID, configInput.Name, config, userContext.Transaction);
                      userContext.CommitAndStartNewTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate());

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
                  var userContext = context.SetupUserContext()
                      .WithTransaction(modelContextBuilder => modelContextBuilder.BuildDeferred())
                      .WithTimeThreshold(() => TimeThreshold.BuildLatest());

                  var id = context.GetArgument<long>("oiaID");

                  CheckManagementPermissionThrow(userContext, "delete OIAContext");

                  var deleted = await oiaContextModel.Delete(id, userContext.Transaction);
                  userContext.CommitAndStartNewTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate());
                  return deleted != null;
              });


            FieldAsync<ODataAPIContextType>("manage_upsertODataAPIContext",
                arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<UpsertODataAPIContextInputType>> { Name = "odataAPIContext" }
                ),
                resolve: async context =>
                {
                    var userContext = context.SetupUserContext()
                        .WithTransaction(modelContextBuilder => modelContextBuilder.BuildDeferred())
                        .WithTimeThreshold(() => TimeThreshold.BuildLatest());

                    var contextInput = context.GetArgument<UpsertODataAPIContextInput>("odataAPIContext")!;

                    CheckManagementPermissionThrow(userContext, "modify ODataAPIContext");

                    try
                    {
                        var config = ODataAPIContext.ConfigSerializer.Deserialize(contextInput.Config);

                        var created = await odataAPIContextModel.Upsert(contextInput.ID, config, userContext.Transaction);
                        userContext.CommitAndStartNewTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate());

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
                  var userContext = context.SetupUserContext()
                      .WithTransaction(modelContextBuilder => modelContextBuilder.BuildDeferred())
                      .WithTimeThreshold(() => TimeThreshold.BuildLatest());

                  var id = context.GetArgument<string>("id")!;

                  CheckManagementPermissionThrow(userContext, "delete ODataAPIContext");

                  var deleted = await odataAPIContextModel.Delete(id, userContext.Transaction);
                  userContext.CommitAndStartNewTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate());
                  return deleted != null;
              });


            FieldAsync<BooleanGraphType>("manage_truncateLayer",
              arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id" }
              ),
              resolve: async context =>
              {
                  var userContext = context.SetupUserContext()
                      .WithTransaction(modelContextBuilder => modelContextBuilder.BuildDeferred())
                      .WithTimeThreshold(() => TimeThreshold.BuildLatest());

                  var id = context.GetArgument<string>("id")!;

                  CheckManagementPermissionThrow(userContext, "manage layer");
                  if (!layerBasedAuthorizationService.CanUserWriteToLayer(userContext.User, id))
                      throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to modify layer");

                  var numDeletedAttributes = await baseAttributeRevisionistModel.DeleteAllAttributes(id, userContext.Transaction);
                  var numDeletedRelations = await baseRelationRevisionistModel.DeleteAllRelations(id, userContext.Transaction);
                  userContext.CommitAndStartNewTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate());
                  return true;
              });

            FieldAsync<StringGraphType>("manage_setBaseConfiguration",
                arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "baseConfiguration" }
                ),
                resolve: async context =>
                {
                    var userContext = context.SetupUserContext()
                        .WithTransaction(modelContextBuilder => modelContextBuilder.BuildDeferred())
                        .WithTimeThreshold(() => TimeThreshold.BuildLatest());

                    var configStr = context.GetArgument<string>("baseConfiguration")!;

                    CheckManagementPermissionThrow(userContext, "manage base configuration");

                    var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                    CheckModifyManagementThrow(userContext, metaConfiguration, "modify base configuration");

                    try
                    {
                        var config = BaseConfigurationV2.Serializer.Deserialize(configStr);

                        var changesetProxy = new ChangesetProxy(userContext.User.InDatabase, userContext.TimeThreshold, changesetModel);
                        var created = await baseConfigurationModel.SetConfig(config, new LayerSet(metaConfiguration.ConfigLayerset), metaConfiguration.ConfigWriteLayer, new Base.Entity.DataOrigin.DataOriginV1(Base.Entity.DataOrigin.DataOriginType.Manual),
                            changesetProxy, userContext.Transaction);
                        userContext.CommitAndStartNewTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate());

                        return BaseConfigurationV2.Serializer.SerializeToString(created);
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
                  var userContext = context.SetupUserContext()
                      .WithTransaction(modelContextBuilder => modelContextBuilder.BuildDeferred())
                      .WithTimeThreshold(() => TimeThreshold.BuildLatest());

                  var predicate = context.GetArgument<UpsertPredicateInput>("predicate")!;

                  var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                  CheckModifyManagementThrow(userContext, metaConfiguration, "modify predicates");

                  var changesetProxy = new ChangesetProxy(userContext.User.InDatabase, userContext.TimeThreshold, changesetModel);
                  var newPredicate = await predicateModel.InsertOrUpdate(predicate.ID, predicate.WordingFrom, predicate.WordingTo, new LayerSet(metaConfiguration.ConfigLayerset), metaConfiguration.ConfigWriteLayer, new Base.Entity.DataOrigin.DataOriginV1(Base.Entity.DataOrigin.DataOriginType.Manual),
                      changesetProxy, userContext.Transaction);
                  userContext.CommitAndStartNewTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate());

                  return newPredicate.predicate;
              });


            FieldAsync<BooleanGraphType>("manage_removePredicate",
              arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "predicateID" }
              ),
              resolve: async context =>
              {
                  var userContext = context.SetupUserContext()
                      .WithTransaction(modelContextBuilder => modelContextBuilder.BuildDeferred())
                      .WithTimeThreshold(() => TimeThreshold.BuildLatest());

                  var predicateID = context.GetArgument<string>("predicateID")!;

                  var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                  CheckModifyManagementThrow(userContext, metaConfiguration, "remove predicates");

                  var changesetProxy = new ChangesetProxy(userContext.User.InDatabase, userContext.TimeThreshold, changesetModel);

                  var deleted = await predicateModel.TryToDelete(predicateID, new LayerSet(metaConfiguration.ConfigLayerset), metaConfiguration.ConfigWriteLayer,
                      new Base.Entity.DataOrigin.DataOriginV1(Base.Entity.DataOrigin.DataOriginType.Manual), changesetProxy, userContext.Transaction);
                  userContext.CommitAndStartNewTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate());

                  return deleted;
              });

            FieldAsync<RecursiveTraitType>("manage_upsertRecursiveTrait",
              arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<UpsertRecursiveTraitInputType>> { Name = "trait" }
              ),
              resolve: async context =>
              {
                  var userContext = context.SetupUserContext()
                      .WithTransaction(modelContextBuilder => modelContextBuilder.BuildDeferred())
                      .WithTimeThreshold(() => TimeThreshold.BuildLatest());

                  var trait = context.GetArgument<UpsertRecursiveTraitInput>("trait")!;

                  var requiredAttributes = trait.RequiredAttributes.Select(str => TraitAttribute.Serializer.Deserialize(str));
                  var optionalAttributes = trait.OptionalAttributes?.Select(str => TraitAttribute.Serializer.Deserialize(str));
                  var requiredRelations = trait.RequiredRelations?.Select(str => TraitRelation.Serializer.Deserialize(str));
                  var optionalRelations = trait.OptionalRelations?.Select(str => TraitRelation.Serializer.Deserialize(str));

                  var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                  CheckModifyManagementThrow(userContext, metaConfiguration, "modify traits");

                  var changesetProxy = new ChangesetProxy(userContext.User.InDatabase, userContext.TimeThreshold, changesetModel);

                  var newTrait = await recursiveDataTraitModel.InsertOrUpdate(
                      trait.ID, requiredAttributes, optionalAttributes, requiredRelations, optionalRelations, trait.RequiredTraits,
                      new LayerSet(metaConfiguration.ConfigLayerset), metaConfiguration.ConfigWriteLayer,
                      new Base.Entity.DataOrigin.DataOriginV1(Base.Entity.DataOrigin.DataOriginType.Manual),
                      changesetProxy, userContext.Transaction);
                  userContext.CommitAndStartNewTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate());

                  return newTrait.recursiveTrait;
              });

            FieldAsync<BooleanGraphType>("manage_removeRecursiveTrait",
              arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id" }
              ),
              resolve: async context =>
              {
                  var userContext = context.SetupUserContext()
                      .WithTransaction(modelContextBuilder => modelContextBuilder.BuildDeferred())
                      .WithTimeThreshold(() => TimeThreshold.BuildLatest());

                  var traitID = context.GetArgument<string>("id")!;

                  var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                  CheckModifyManagementThrow(userContext, metaConfiguration, "modify traits");

                  var changesetProxy = new ChangesetProxy(userContext.User.InDatabase, userContext.TimeThreshold, changesetModel);

                  var deleted = await recursiveDataTraitModel.TryToDelete(traitID, new LayerSet(metaConfiguration.ConfigLayerset), metaConfiguration.ConfigWriteLayer,
                      new Base.Entity.DataOrigin.DataOriginV1(Base.Entity.DataOrigin.DataOriginType.Manual), changesetProxy, userContext.Transaction);
                  userContext.CommitAndStartNewTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate());

                  return deleted;
              });

            FieldAsync<GeneratorType>("manage_upsertGenerator",
              arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<UpsertGeneratorInputType>> { Name = "generator" }
              ),
              resolve: async context =>
              {
                  var userContext = context.SetupUserContext()
                      .WithTransaction(modelContextBuilder => modelContextBuilder.BuildDeferred())
                      .WithTimeThreshold(() => TimeThreshold.BuildLatest());

                  var generator = context.GetArgument<UpsertGeneratorInput>("generator")!;

                  var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                  CheckModifyManagementThrow(userContext, metaConfiguration, "modify generators");

                  var changesetProxy = new ChangesetProxy(userContext.User.InDatabase, userContext.TimeThreshold, changesetModel);

                  var newGenerator = await generatorModel.InsertOrUpdate(
                      generator.ID, generator.AttributeName, generator.AttributeValueTemplate,
                      new LayerSet(metaConfiguration.ConfigLayerset), metaConfiguration.ConfigWriteLayer,
                      new Base.Entity.DataOrigin.DataOriginV1(Base.Entity.DataOrigin.DataOriginType.Manual),
                      changesetProxy, userContext.Transaction);
                  userContext.CommitAndStartNewTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate());

                  return newGenerator.generator;
              });
            FieldAsync<BooleanGraphType>("manage_removeGenerator",
              arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id" }
              ),
              resolve: async context =>
              {
                  var userContext = context.SetupUserContext()
                      .WithTransaction(modelContextBuilder => modelContextBuilder.BuildDeferred())
                      .WithTimeThreshold(() => TimeThreshold.BuildLatest());

                  var generatorID = context.GetArgument<string>("id")!;

                  var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                  CheckModifyManagementThrow(userContext, metaConfiguration, "modify generators");

                  var changesetProxy = new ChangesetProxy(userContext.User.InDatabase, userContext.TimeThreshold, changesetModel);

                  var deleted = await generatorModel.TryToDelete(generatorID, new LayerSet(metaConfiguration.ConfigLayerset), metaConfiguration.ConfigWriteLayer,
                      new Base.Entity.DataOrigin.DataOriginV1(Base.Entity.DataOrigin.DataOriginType.Manual), changesetProxy, userContext.Transaction);
                  userContext.CommitAndStartNewTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate());

                  return deleted;
              });

            FieldAsync<AuthRoleType>("manage_upsertAuthRole",
              arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<UpsertAuthRoleInputType>> { Name = "authRole" }
              ),
              resolve: async context =>
              {
                  var userContext = context.SetupUserContext()
                      .WithTransaction(modelContextBuilder => modelContextBuilder.BuildDeferred())
                      .WithTimeThreshold(() => TimeThreshold.BuildLatest());

                  var authRole = context.GetArgument<UpsertAuthRoleInput>("authRole")!;

                  var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                  CheckModifyManagementThrow(userContext, metaConfiguration, "modify auth roles");

                  var changesetProxy = new ChangesetProxy(userContext.User.InDatabase, userContext.TimeThreshold, changesetModel);

                  var newAuthRole = await authRoleModel.InsertOrUpdate(
                      authRole.ID, authRole.Permissions,
                      new LayerSet(metaConfiguration.ConfigLayerset), metaConfiguration.ConfigWriteLayer,
                      new Base.Entity.DataOrigin.DataOriginV1(Base.Entity.DataOrigin.DataOriginType.Manual),
                      changesetProxy, userContext.Transaction);
                  userContext.CommitAndStartNewTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate());

                  return newAuthRole.authRole;
              });

            FieldAsync<BooleanGraphType>("manage_removeAuthRole",
              arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id" }
              ),
              resolve: async context =>
              {
                  var userContext = context.SetupUserContext()
                      .WithTransaction(modelContextBuilder => modelContextBuilder.BuildDeferred())
                      .WithTimeThreshold(() => TimeThreshold.BuildLatest());

                  var authRoleID = context.GetArgument<string>("id")!;

                  var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                  CheckModifyManagementThrow(userContext, metaConfiguration, "modify auth roles");

                  var changesetProxy = new ChangesetProxy(userContext.User.InDatabase, userContext.TimeThreshold, changesetModel);

                  var deleted = await authRoleModel.TryToDelete(authRoleID,
                      new LayerSet(metaConfiguration.ConfigLayerset), metaConfiguration.ConfigWriteLayer,
                      new Base.Entity.DataOrigin.DataOriginV1(Base.Entity.DataOrigin.DataOriginType.Manual), changesetProxy, userContext.Transaction);
                  userContext.CommitAndStartNewTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate());

                  return deleted;
              });

            FieldAsync<CLConfigType>("manage_upsertCLConfig",
              arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<UpsertCLConfigInputType>> { Name = "config" }
              ),
              resolve: async context =>
              {
                  var userContext = context.SetupUserContext()
                      .WithTransaction(modelContextBuilder => modelContextBuilder.BuildDeferred())
                      .WithTimeThreshold(() => TimeThreshold.BuildLatest());

                  var clConfig = context.GetArgument<UpsertCLConfigInput>("config")!;

                  var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                  CheckModifyManagementThrow(userContext, metaConfiguration, "modify cl configs");

                  var changesetProxy = new ChangesetProxy(userContext.User.InDatabase, userContext.TimeThreshold, changesetModel);

                  var config = JObject.Parse(clConfig.CLBrainConfig);

                  var newCLConfig = await clConfigModel.InsertOrUpdate(
                      clConfig.ID, clConfig.CLBrainReference, config,
                      new LayerSet(metaConfiguration.ConfigLayerset), metaConfiguration.ConfigWriteLayer,
                      new Base.Entity.DataOrigin.DataOriginV1(Base.Entity.DataOrigin.DataOriginType.Manual),
                      changesetProxy, userContext.Transaction);
                  userContext.CommitAndStartNewTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate());

                  return newCLConfig.config;
              });

            FieldAsync<BooleanGraphType>("manage_removeCLConfig",
              arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id" }
              ),
              resolve: async context =>
              {
                  var userContext = context.SetupUserContext()
                      .WithTransaction(modelContextBuilder => modelContextBuilder.BuildDeferred())
                      .WithTimeThreshold(() => TimeThreshold.BuildLatest());

                  var id = context.GetArgument<string>("id")!;

                  var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                  CheckModifyManagementThrow(userContext, metaConfiguration, "modify cl configs");

                  var changesetProxy = new ChangesetProxy(userContext.User.InDatabase, userContext.TimeThreshold, changesetModel);

                  var deleted = await clConfigModel.TryToDelete(id,
                      new LayerSet(metaConfiguration.ConfigLayerset), metaConfiguration.ConfigWriteLayer,
                      new Base.Entity.DataOrigin.DataOriginV1(Base.Entity.DataOrigin.DataOriginType.Manual), changesetProxy, userContext.Transaction);
                  userContext.CommitAndStartNewTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate());

                  return deleted;
              });
        }
    }
}
