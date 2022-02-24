using GraphQL;
using GraphQL.Types;
using Newtonsoft.Json.Linq;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.Config;
using Omnikeeper.Base.Generator;
using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.GraphQL.Types;
using System;
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
            FieldAsync<LayerDataType>("manage_upsertLayerData",
                arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<UpsertLayerInputDataType>> { Name = "layer" }
                ),
                resolve: async context =>
                {
                    var userContext = context.SetupUserContext()
                        .WithTransaction(modelContextBuilder => modelContextBuilder.BuildDeferred())
                        .WithTimeThreshold(TimeThreshold.BuildLatest(), context.Path);

                    var upsertLayer = context.GetArgument<UpsertLayerDataInput>("layer")!;

                    CheckManagementPermissionThrow(userContext, "modify layers");

                    string clConfigID = "";
                    if (upsertLayer.CLConfigID != null && upsertLayer.CLConfigID != "")
                        clConfigID = upsertLayer.CLConfigID;
                    string oiaReference = "";
                    if (upsertLayer.OnlineInboundAdapterName != null && upsertLayer.OnlineInboundAdapterName != "")
                        oiaReference = upsertLayer.OnlineInboundAdapterName;
                    var generators = upsertLayer.Generators.Where(g => !string.IsNullOrEmpty(g)).ToArray();
                    var changesetProxy = new ChangesetProxy(userContext.User.InDatabase, userContext.GetTimeThreshold(context.Path), changesetModel);
                    var (updatedLayer, _) = await layerDataModel.UpsertLayerData(
                        upsertLayer.ID, upsertLayer.Description, upsertLayer.Color, upsertLayer.State.ToString(), clConfigID, oiaReference, generators,
                        new Base.Entity.DataOrigin.DataOriginV1(Base.Entity.DataOrigin.DataOriginType.Manual), changesetProxy, userContext.Transaction
                        );

                    userContext.CommitAndStartNewTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate());

                    return updatedLayer;
                });

            FieldAsync<LayerDataType>("manage_createLayer",
                arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id" }
                ),
                resolve: async context =>
                {
                    var userContext = context.SetupUserContext()
                        .WithTransaction(modelContextBuilder => modelContextBuilder.BuildDeferred())
                        .WithTimeThreshold(TimeThreshold.BuildLatest(), context.Path);

                    var layerID = context.GetArgument<string>("id")!;

                    CheckManagementPermissionThrow(userContext, "create layers");

                    // check that layer exists
                    var layer = await layerModel.CreateLayerIfNotExists(layerID, userContext.Transaction);

                    var layerData = await layerDataModel.GetLayerData(layerID, userContext.Transaction, userContext.GetTimeThreshold(context.Path));

                    userContext.CommitAndStartNewTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate());

                    return layerData;
                });


            FieldAsync<OIAContextType>("manage_createOIAContext",
                arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<CreateOIAContextInputType>> { Name = "oiaContext" }
                ),
                resolve: async context =>
                {
                    var userContext = context.SetupUserContext()
                        .WithTransaction(modelContextBuilder => modelContextBuilder.BuildDeferred())
                        .WithTimeThreshold(TimeThreshold.BuildLatest(), context.Path);

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
                      .WithTimeThreshold(TimeThreshold.BuildLatest(), context.Path);

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
                      .WithTimeThreshold(TimeThreshold.BuildLatest(), context.Path);

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
                        .WithTimeThreshold(TimeThreshold.BuildLatest(), context.Path);

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
                      .WithTimeThreshold(TimeThreshold.BuildLatest(), context.Path);

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
                      .WithTimeThreshold(TimeThreshold.BuildLatest(), context.Path);

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
                        .WithTimeThreshold(TimeThreshold.BuildLatest(), context.Path);

                    var configStr = context.GetArgument<string>("baseConfiguration")!;

                    CheckManagementPermissionThrow(userContext, "manage base configuration");

                    var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                    CheckModifyManagementThrow(userContext, metaConfiguration, "modify base configuration");

                    try
                    {
                        var config = BaseConfigurationV2.Serializer.Deserialize(configStr);

                        var changesetProxy = new ChangesetProxy(userContext.User.InDatabase, userContext.GetTimeThreshold(context.Path), changesetModel);
                        var created = await baseConfigurationModel.SetConfig(config, metaConfiguration.ConfigLayerset, metaConfiguration.ConfigWriteLayer, new Base.Entity.DataOrigin.DataOriginV1(Base.Entity.DataOrigin.DataOriginType.Manual),
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
                      .WithTimeThreshold(TimeThreshold.BuildLatest(), context.Path);

                  var predicate = context.GetArgument<UpsertPredicateInput>("predicate")!;

                  var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                  CheckModifyManagementThrow(userContext, metaConfiguration, "modify predicates");

                  var changesetProxy = new ChangesetProxy(userContext.User.InDatabase, userContext.GetTimeThreshold(context.Path), changesetModel);

                  var @new = new Predicate(predicate.ID, predicate.WordingFrom, predicate.WordingTo);

                  var newPredicate = await predicateModel.InsertOrUpdate(@new, metaConfiguration.ConfigLayerset, metaConfiguration.ConfigWriteLayer, new Base.Entity.DataOrigin.DataOriginV1(Base.Entity.DataOrigin.DataOriginType.Manual),
                      changesetProxy, userContext.Transaction, MaskHandlingForRemovalApplyNoMask.Instance, OtherLayersValueHandlingForceWrite.Instance);
                  userContext.CommitAndStartNewTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate());

                  return newPredicate.dc;
              });


            FieldAsync<BooleanGraphType>("manage_removePredicate",
              arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "predicateID" }
              ),
              resolve: async context =>
              {
                  var userContext = context.SetupUserContext()
                      .WithTransaction(modelContextBuilder => modelContextBuilder.BuildDeferred())
                      .WithTimeThreshold(TimeThreshold.BuildLatest(), context.Path);

                  var predicateID = context.GetArgument<string>("predicateID")!;

                  var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                  CheckModifyManagementThrow(userContext, metaConfiguration, "remove predicates");

                  var changesetProxy = new ChangesetProxy(userContext.User.InDatabase, userContext.GetTimeThreshold(context.Path), changesetModel);

                  var deleted = await predicateModel.TryToDelete(predicateID, metaConfiguration.ConfigLayerset, metaConfiguration.ConfigWriteLayer,
                      new Base.Entity.DataOrigin.DataOriginV1(Base.Entity.DataOrigin.DataOriginType.Manual), changesetProxy, userContext.Transaction, MaskHandlingForRemovalApplyNoMask.Instance, OtherLayersValueHandlingForceWrite.Instance);
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
                      .WithTimeThreshold(TimeThreshold.BuildLatest(), context.Path);

                  var trait = context.GetArgument<UpsertRecursiveTraitInput>("trait")!;

                  var requiredAttributes = trait.RequiredAttributes.Select(str => TraitAttribute.Serializer.Deserialize(str));
                  var optionalAttributes = trait.OptionalAttributes?.Select(str => TraitAttribute.Serializer.Deserialize(str));
                  var optionalRelations = trait.OptionalRelations?.Select(str => TraitRelation.Serializer.Deserialize(str));

                  var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                  CheckModifyManagementThrow(userContext, metaConfiguration, "modify traits");

                  var @new = new RecursiveTrait(trait.ID, new TraitOriginV1(TraitOriginType.Data), requiredAttributes, optionalAttributes, optionalRelations, trait.RequiredTraits);

                  var changesetProxy = new ChangesetProxy(userContext.User.InDatabase, userContext.GetTimeThreshold(context.Path), changesetModel);

                  var newTrait = await recursiveDataTraitModel.InsertOrUpdate(
                      @new,
                      metaConfiguration.ConfigLayerset, metaConfiguration.ConfigWriteLayer,
                      new Base.Entity.DataOrigin.DataOriginV1(Base.Entity.DataOrigin.DataOriginType.Manual),
                      changesetProxy, userContext.Transaction, MaskHandlingForRemovalApplyNoMask.Instance, OtherLayersValueHandlingForceWrite.Instance);
                  userContext.CommitAndStartNewTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate());

                  return newTrait.dc;
              });

            FieldAsync<BooleanGraphType>("manage_removeRecursiveTrait",
              arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id" }
              ),
              resolve: async context =>
              {
                  var userContext = context.SetupUserContext()
                      .WithTransaction(modelContextBuilder => modelContextBuilder.BuildDeferred())
                      .WithTimeThreshold(TimeThreshold.BuildLatest(), context.Path);

                  var traitID = context.GetArgument<string>("id")!;

                  var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                  CheckModifyManagementThrow(userContext, metaConfiguration, "modify traits");

                  var changesetProxy = new ChangesetProxy(userContext.User.InDatabase, userContext.GetTimeThreshold(context.Path), changesetModel);

                  var deleted = await recursiveDataTraitModel.TryToDelete(traitID, metaConfiguration.ConfigLayerset, metaConfiguration.ConfigWriteLayer,
                      new Base.Entity.DataOrigin.DataOriginV1(Base.Entity.DataOrigin.DataOriginType.Manual), changesetProxy, userContext.Transaction, MaskHandlingForRemovalApplyNoMask.Instance, OtherLayersValueHandlingForceWrite.Instance);
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
                      .WithTimeThreshold(TimeThreshold.BuildLatest(), context.Path);

                  var generator = context.GetArgument<UpsertGeneratorInput>("generator")!;

                  var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                  CheckModifyManagementThrow(userContext, metaConfiguration, "modify generators");

                  var changesetProxy = new ChangesetProxy(userContext.User.InDatabase, userContext.GetTimeThreshold(context.Path), changesetModel);

                  var changed = new GeneratorV1(generator.ID, generator.AttributeName, generator.AttributeValueTemplate);

                  var newGenerator = await generatorModel.InsertOrUpdate(
                      changed,
                      metaConfiguration.ConfigLayerset, metaConfiguration.ConfigWriteLayer,
                      new Base.Entity.DataOrigin.DataOriginV1(Base.Entity.DataOrigin.DataOriginType.Manual),
                      changesetProxy, userContext.Transaction, MaskHandlingForRemovalApplyNoMask.Instance, OtherLayersValueHandlingForceWrite.Instance);
                  userContext.CommitAndStartNewTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate());

                  return newGenerator.dc;
              });
            FieldAsync<BooleanGraphType>("manage_removeGenerator",
              arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id" }
              ),
              resolve: async context =>
              {
                  var userContext = context.SetupUserContext()
                      .WithTransaction(modelContextBuilder => modelContextBuilder.BuildDeferred())
                      .WithTimeThreshold(TimeThreshold.BuildLatest(), context.Path);

                  var generatorID = context.GetArgument<string>("id")!;

                  var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                  CheckModifyManagementThrow(userContext, metaConfiguration, "modify generators");

                  var changesetProxy = new ChangesetProxy(userContext.User.InDatabase, userContext.GetTimeThreshold(context.Path), changesetModel);

                  var deleted = await generatorModel.TryToDelete(generatorID, metaConfiguration.ConfigLayerset, metaConfiguration.ConfigWriteLayer,
                      new Base.Entity.DataOrigin.DataOriginV1(Base.Entity.DataOrigin.DataOriginType.Manual), changesetProxy, userContext.Transaction, MaskHandlingForRemovalApplyNoMask.Instance, OtherLayersValueHandlingForceWrite.Instance);
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
                      .WithTimeThreshold(TimeThreshold.BuildLatest(), context.Path);

                  var authRole = context.GetArgument<UpsertAuthRoleInput>("authRole")!;

                  var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                  CheckModifyManagementThrow(userContext, metaConfiguration, "modify auth roles");

                  var changesetProxy = new ChangesetProxy(userContext.User.InDatabase, userContext.GetTimeThreshold(context.Path), changesetModel);

                  var @new = new AuthRole(authRole.ID, authRole.Permissions);

                  var updated = await authRoleModel.InsertOrUpdate(@new,
                      metaConfiguration.ConfigLayerset, metaConfiguration.ConfigWriteLayer,
                      new Base.Entity.DataOrigin.DataOriginV1(Base.Entity.DataOrigin.DataOriginType.Manual),
                      changesetProxy, userContext.Transaction, MaskHandlingForRemovalApplyNoMask.Instance, OtherLayersValueHandlingForceWrite.Instance);
                  userContext.CommitAndStartNewTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate());

                  return updated.dc;
              });

            FieldAsync<BooleanGraphType>("manage_removeAuthRole",
              arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id" }
              ),
              resolve: async context =>
              {
                  var userContext = context.SetupUserContext()
                      .WithTransaction(modelContextBuilder => modelContextBuilder.BuildDeferred())
                      .WithTimeThreshold(TimeThreshold.BuildLatest(), context.Path);

                  var authRoleID = context.GetArgument<string>("id")!;

                  var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                  CheckModifyManagementThrow(userContext, metaConfiguration, "modify auth roles");

                  var changesetProxy = new ChangesetProxy(userContext.User.InDatabase, userContext.GetTimeThreshold(context.Path), changesetModel);

                  var deleted = await authRoleModel.TryToDelete(authRoleID,
                      metaConfiguration.ConfigLayerset, metaConfiguration.ConfigWriteLayer,
                      new Base.Entity.DataOrigin.DataOriginV1(Base.Entity.DataOrigin.DataOriginType.Manual), changesetProxy, userContext.Transaction, MaskHandlingForRemovalApplyNoMask.Instance, OtherLayersValueHandlingForceWrite.Instance);
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
                      .WithTimeThreshold(TimeThreshold.BuildLatest(), context.Path);

                  var clConfig = context.GetArgument<UpsertCLConfigInput>("config")!;

                  var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                  CheckModifyManagementThrow(userContext, metaConfiguration, "modify cl configs");

                  var changesetProxy = new ChangesetProxy(userContext.User.InDatabase, userContext.GetTimeThreshold(context.Path), changesetModel);

                  var config = JObject.Parse(clConfig.CLBrainConfig);

                  var updated = new CLConfigV1(clConfig.ID, clConfig.CLBrainReference, config);

                  var newCLConfig = await clConfigModel.InsertOrUpdate(updated,
                      metaConfiguration.ConfigLayerset, metaConfiguration.ConfigWriteLayer,
                      new Base.Entity.DataOrigin.DataOriginV1(Base.Entity.DataOrigin.DataOriginType.Manual),
                      changesetProxy, userContext.Transaction, MaskHandlingForRemovalApplyNoMask.Instance, OtherLayersValueHandlingForceWrite.Instance);
                  userContext.CommitAndStartNewTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate());

                  return newCLConfig.dc;
              });

            FieldAsync<BooleanGraphType>("manage_removeCLConfig",
              arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id" }
              ),
              resolve: async context =>
              {
                  var userContext = context.SetupUserContext()
                      .WithTransaction(modelContextBuilder => modelContextBuilder.BuildDeferred())
                      .WithTimeThreshold(TimeThreshold.BuildLatest(), context.Path);

                  var id = context.GetArgument<string>("id")!;

                  var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                  CheckModifyManagementThrow(userContext, metaConfiguration, "modify cl configs");

                  var changesetProxy = new ChangesetProxy(userContext.User.InDatabase, userContext.GetTimeThreshold(context.Path), changesetModel);

                  var deleted = await clConfigModel.TryToDelete(id,
                      metaConfiguration.ConfigLayerset, metaConfiguration.ConfigWriteLayer,
                      new Base.Entity.DataOrigin.DataOriginV1(Base.Entity.DataOrigin.DataOriginType.Manual), changesetProxy, userContext.Transaction, MaskHandlingForRemovalApplyNoMask.Instance, OtherLayersValueHandlingForceWrite.Instance);
                  userContext.CommitAndStartNewTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate());

                  return deleted;
              });
        }
    }
}
