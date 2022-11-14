using GraphQL;
using GraphQL.Types;
using Omnikeeper.Authz;
using Omnikeeper.Base.Authz;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.Config;
using Omnikeeper.Base.Generator;
using Omnikeeper.Base.GraphQL;
using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.GraphQL.Types;
using Omnikeeper.Startup;
using System;
using System.Linq;
using System.Text.Json;

namespace Omnikeeper.GraphQL
{
    public partial class GraphQLMutation
    {
        // TODO: fix/rework authz for management... what is the difference between CheckManagementPermissionThrow() and CheckModifyManagementThrow?
        // and when to use what?
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
            Field<LayerDataType>("manage_upsertLayerData")
                .Arguments(
                    new QueryArgument<NonNullGraphType<UpsertLayerInputDataType>> { Name = "layer" }
                )
                .ResolveAsync(async context =>
                {
                    var userContext = context.GetUserContext();

                    var upsertLayer = context.GetArgument<UpsertLayerDataInput>("layer")!;

                    CheckManagementPermissionThrow(userContext, "modify layers");

                    string clConfigID = "";
                    if (upsertLayer.CLConfigID != null && upsertLayer.CLConfigID != "")
                        clConfigID = upsertLayer.CLConfigID;
                    string oiaReference = "";
                    if (upsertLayer.OnlineInboundAdapterName != null && upsertLayer.OnlineInboundAdapterName != "")
                        oiaReference = upsertLayer.OnlineInboundAdapterName;
                    var generators = upsertLayer.Generators.Where(g => !string.IsNullOrEmpty(g)).ToArray();
                    var (updatedLayer, _, _) = await layerDataModel.UpsertLayerData(
                        upsertLayer.ID, upsertLayer.Description, upsertLayer.Color, upsertLayer.State.ToString(), clConfigID, oiaReference, generators,
                        userContext.ChangesetProxy, userContext.Transaction
                        );

                    userContext.CommitAndStartNewTransactionIfLastMutationAndNoErrors(context, modelContextBuilder => modelContextBuilder.BuildImmediate());

                    return updatedLayer;
                });

            Field<LayerDataType>("manage_createLayer")
                .Arguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id" }
                )
                .ResolveAsync(async context =>
                {
                    var userContext = context.GetUserContext();

                    var layerID = context.GetArgument<string>("id")!;

                    CheckManagementPermissionThrow(userContext, "create layers");

                    // check that layer exists
                    var layer = await layerModel.CreateLayerIfNotExists(layerID, userContext.Transaction);

                    var layerData = await layerDataModel.GetLayerData(layerID, userContext.Transaction, userContext.GetTimeThreshold(context.Path));

                    userContext.CommitAndStartNewTransactionIfLastMutationAndNoErrors(context, modelContextBuilder => modelContextBuilder.BuildImmediate());

                    return layerData;
                });


            Field<OIAContextType>("manage_createOIAContext")
                .Arguments(
                    new QueryArgument<NonNullGraphType<CreateOIAContextInputType>> { Name = "oiaContext" }
                )
                .ResolveAsync(async context =>
                {
                    var userContext = context.GetUserContext();

                    var configInput = context.GetArgument<CreateOIAContextInput>("oiaContext")!;

                    CheckManagementPermissionThrow(userContext, "create OIAContext");

                    try
                    {
                        var config = IOnlineInboundAdapter.IConfig.Serializer.Deserialize(configInput.Config);
                        var createdOIAContext = await oiaContextModel.Create(configInput.Name, config, userContext.Transaction);
                        userContext.CommitAndStartNewTransactionIfLastMutationAndNoErrors(context, modelContextBuilder => modelContextBuilder.BuildImmediate());

                        return createdOIAContext;
                    }
                    catch (Exception e)
                    {
                        throw new ExecutionError($"Could not parse configuration", e);
                    }
                });
            Field<OIAContextType>("manage_updateOIAContext")
              .Arguments(
                new QueryArgument<NonNullGraphType<UpdateOIAContextInputType>> { Name = "oiaContext" }
              )
              .ResolveAsync(async context =>
              {
                  var userContext = context.GetUserContext();

                  var configInput = context.GetArgument<UpdateOIAContextInput>("oiaContext")!;

                  CheckManagementPermissionThrow(userContext, "update OIAContext");

                  try
                  {
                      var config = IOnlineInboundAdapter.IConfig.Serializer.Deserialize(configInput.Config);
                      var oiaContext = await oiaContextModel.Update(configInput.ID, configInput.Name, config, userContext.Transaction);
                      userContext.CommitAndStartNewTransactionIfLastMutationAndNoErrors(context, modelContextBuilder => modelContextBuilder.BuildImmediate());

                      return oiaContext;
                  }
                  catch (Exception e)
                  {
                      throw new ExecutionError($"Could not parse configuration", e);
                  }
              });
            Field<BooleanGraphType>("manage_deleteOIAContext")
              .Arguments(
                new QueryArgument<NonNullGraphType<LongGraphType>> { Name = "oiaID" }
              )
              .ResolveAsync(async context =>
              {
                  var userContext = context.GetUserContext();

                  var id = context.GetArgument<long>("oiaID");

                  CheckManagementPermissionThrow(userContext, "delete OIAContext");

                  var deleted = await oiaContextModel.Delete(id, userContext.Transaction);
                  userContext.CommitAndStartNewTransactionIfLastMutationAndNoErrors(context, modelContextBuilder => modelContextBuilder.BuildImmediate());
                  return deleted != null;
              });

            Field<BooleanGraphType>("manage_truncateLayer")
              .Arguments(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id" }
              )
              .ResolveAsync(async context =>
              {
                  var userContext = context.GetUserContext();

                  var id = context.GetArgument<string>("id")!;

                  CheckManagementPermissionThrow(userContext, "manage layer");
                  if (await authzFilterManager.ApplyPreFilterForMutation(new PreTruncateLayerContextForCIs(), userContext.User, id, id, userContext.Transaction, userContext.GetTimeThreshold(context.Path)) is AuthzFilterResultDeny d)
                      throw new ExecutionError(d.Reason);

                  var numDeletedAttributes = await baseAttributeRevisionistModel.DeleteAllAttributes(AllCIIDsSelection.Instance, id, userContext.Transaction);
                  var numDeletedRelations = await baseRelationRevisionistModel.DeleteAllRelations(id, userContext.Transaction);

                  if (await authzFilterManager.ApplyPostFilterForMutation(new PostTruncateLayerContextForCIs(), userContext.User, id, null, userContext.Transaction, userContext.GetTimeThreshold(context.Path)) is AuthzFilterResultDeny dPost)
                      throw new ExecutionError(dPost.Reason);

                  userContext.CommitAndStartNewTransactionIfLastMutationAndNoErrors(context, modelContextBuilder => modelContextBuilder.BuildImmediate());
                  return true;
              });

            Field<StringGraphType>("manage_setBaseConfiguration")
                .Arguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "baseConfiguration" }
                )
                .ResolveAsync(async context =>
                {
                    var userContext = context.GetUserContext();

                    var configStr = context.GetArgument<string>("baseConfiguration")!;

                    CheckManagementPermissionThrow(userContext, "manage base configuration");

                    var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                    CheckModifyManagementThrow(userContext, metaConfiguration, "modify base configuration");

                    try
                    {
                        var config = BaseConfigurationV2.Serializer.Deserialize(configStr);

                        var created = await baseConfigurationModel.SetConfig(config, metaConfiguration.ConfigLayerset, metaConfiguration.ConfigWriteLayer,
                            userContext.ChangesetProxy, userContext.Transaction);
                        userContext.CommitAndStartNewTransactionIfLastMutationAndNoErrors(context, modelContextBuilder => modelContextBuilder.BuildImmediate());

                        return BaseConfigurationV2.Serializer.SerializeToString(created);
                    }
                    catch (Exception e)
                    {
                        throw new ExecutionError($"Could not save base configuration", e);
                    }
                });

            Field<RecursiveTraitType>("manage_upsertRecursiveTrait")
              .Arguments(
                new QueryArgument<NonNullGraphType<UpsertRecursiveTraitInputType>> { Name = "trait" }
              )
              .ResolveAsync(async context =>
              {
                  var userContext = context.GetUserContext();

                  var trait = context.GetArgument<UpsertRecursiveTraitInput>("trait")!;

                  var requiredAttributes = trait.RequiredAttributes;
                  var optionalAttributes = trait.OptionalAttributes;
                  var optionalRelations = trait.OptionalRelations;

                  var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                  CheckModifyManagementThrow(userContext, metaConfiguration, "modify traits");

                  var @new = new RecursiveTrait(trait.ID, new TraitOriginV1(TraitOriginType.Data), requiredAttributes, optionalAttributes, optionalRelations, trait.RequiredTraits);

                  var newTrait = await recursiveDataTraitModel.InsertOrUpdate(
                      @new,
                      metaConfiguration.ConfigLayerset, metaConfiguration.ConfigWriteLayer,
                      userContext.ChangesetProxy, userContext.Transaction, MaskHandlingForRemovalApplyNoMask.Instance);
                  userContext.CommitAndStartNewTransactionIfLastMutationAndNoErrors(context, modelContextBuilder => modelContextBuilder.BuildImmediate());

                  // trigger job to reload traits
                  await localScheduler.TriggerJob(QuartzJobStarter.JKTraitsReloader);

                  return newTrait.dc;
              });

            Field<BooleanGraphType>("manage_removeRecursiveTrait")
              .Arguments(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id" }
              )
              .ResolveAsync(async context =>
              {
                  var userContext = context.GetUserContext();

                  var traitID = context.GetArgument<string>("id")!;

                  var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                  CheckModifyManagementThrow(userContext, metaConfiguration, "modify traits");

                  var deleted = await recursiveDataTraitModel.TryToDelete(traitID, metaConfiguration.ConfigLayerset, metaConfiguration.ConfigWriteLayer,
                      userContext.ChangesetProxy, userContext.Transaction, MaskHandlingForRemovalApplyNoMask.Instance);
                  userContext.CommitAndStartNewTransactionIfLastMutationAndNoErrors(context, modelContextBuilder => modelContextBuilder.BuildImmediate());

                  // trigger job to reload traits
                  await localScheduler.TriggerJob(QuartzJobStarter.JKTraitsReloader);

                  return deleted;
              });

            Field<GeneratorType>("manage_upsertGenerator")
              .Arguments(
                new QueryArgument<NonNullGraphType<UpsertGeneratorInputType>> { Name = "generator" }
              )
              .ResolveAsync(async context =>
              {
                  var userContext = context.GetUserContext();

                  var generator = context.GetArgument<UpsertGeneratorInput>("generator")!;

                  var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                  CheckModifyManagementThrow(userContext, metaConfiguration, "modify generators");

                  var changed = new GeneratorV1(generator.ID, generator.AttributeName, generator.AttributeValueTemplate);

                  var newGenerator = await generatorModel.InsertOrUpdate(
                      changed,
                      metaConfiguration.ConfigLayerset, metaConfiguration.ConfigWriteLayer,
                      userContext.ChangesetProxy, userContext.Transaction, MaskHandlingForRemovalApplyNoMask.Instance);
                  userContext.CommitAndStartNewTransactionIfLastMutationAndNoErrors(context, modelContextBuilder => modelContextBuilder.BuildImmediate());

                  return newGenerator.dc;
              });
            Field<BooleanGraphType>("manage_removeGenerator")
              .Arguments(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id" }
              )
              .ResolveAsync(async context =>
              {
                  var userContext = context.GetUserContext();

                  var generatorID = context.GetArgument<string>("id")!;

                  var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                  CheckModifyManagementThrow(userContext, metaConfiguration, "modify generators");

                  var deleted = await generatorModel.TryToDelete(generatorID, metaConfiguration.ConfigLayerset, metaConfiguration.ConfigWriteLayer,
                      userContext.ChangesetProxy, userContext.Transaction, MaskHandlingForRemovalApplyNoMask.Instance);
                  userContext.CommitAndStartNewTransactionIfLastMutationAndNoErrors(context, modelContextBuilder => modelContextBuilder.BuildImmediate());

                  return deleted;
              });

            Field<AuthRoleType>("manage_upsertAuthRole")
              .Arguments(
                new QueryArgument<NonNullGraphType<UpsertAuthRoleInputType>> { Name = "authRole" }
              )
              .ResolveAsync(async context =>
              {
                  var userContext = context.GetUserContext();

                  var authRole = context.GetArgument<UpsertAuthRoleInput>("authRole")!;

                  var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                  CheckModifyManagementThrow(userContext, metaConfiguration, "modify auth roles");

                  var @new = new AuthRole(authRole.ID, authRole.Permissions);

                  var updated = await authRoleModel.InsertOrUpdate(@new,
                      metaConfiguration.ConfigLayerset, metaConfiguration.ConfigWriteLayer,
                      userContext.ChangesetProxy, userContext.Transaction, MaskHandlingForRemovalApplyNoMask.Instance);
                  userContext.CommitAndStartNewTransactionIfLastMutationAndNoErrors(context, modelContextBuilder => modelContextBuilder.BuildImmediate());

                  return updated.dc;
              });

            Field<BooleanGraphType>("manage_removeAuthRole")
              .Arguments(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id" }
              )
              .ResolveAsync(async context =>
              {
                  var userContext = context.GetUserContext();

                  var authRoleID = context.GetArgument<string>("id")!;

                  var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                  CheckModifyManagementThrow(userContext, metaConfiguration, "modify auth roles");

                  var deleted = await authRoleModel.TryToDelete(authRoleID,
                      metaConfiguration.ConfigLayerset, metaConfiguration.ConfigWriteLayer,
                      userContext.ChangesetProxy, userContext.Transaction, MaskHandlingForRemovalApplyNoMask.Instance);
                  userContext.CommitAndStartNewTransactionIfLastMutationAndNoErrors(context, modelContextBuilder => modelContextBuilder.BuildImmediate());

                  return deleted;
              });

            Field<CLConfigType>("manage_upsertCLConfig")
              .Arguments(
                new QueryArgument<NonNullGraphType<UpsertCLConfigInputType>> { Name = "config" }
              )
              .ResolveAsync(async context =>
              {
                  var userContext = context.GetUserContext();

                  var clConfig = context.GetArgument<UpsertCLConfigInput>("config")!;

                  var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                  CheckModifyManagementThrow(userContext, metaConfiguration, "modify cl configs");

                  using var config = JsonDocument.Parse(clConfig.CLBrainConfig);

                  var updated = new CLConfigV1(clConfig.ID, clConfig.CLBrainReference, config);

                  var newCLConfig = await clConfigModel.InsertOrUpdate(updated,
                      metaConfiguration.ConfigLayerset, metaConfiguration.ConfigWriteLayer,
                      userContext.ChangesetProxy, userContext.Transaction, MaskHandlingForRemovalApplyNoMask.Instance);
                  userContext.CommitAndStartNewTransactionIfLastMutationAndNoErrors(context, modelContextBuilder => modelContextBuilder.BuildImmediate());

                  return newCLConfig.dc;
              });

            Field<BooleanGraphType>("manage_removeCLConfig")
              .Arguments(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id" }
              )
              .ResolveAsync(async context =>
              {
                  var userContext = context.GetUserContext();

                  var id = context.GetArgument<string>("id")!;

                  var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                  CheckModifyManagementThrow(userContext, metaConfiguration, "modify cl configs");

                  var deleted = await clConfigModel.TryToDelete(id,
                      metaConfiguration.ConfigLayerset, metaConfiguration.ConfigWriteLayer,
                      userContext.ChangesetProxy, userContext.Transaction, MaskHandlingForRemovalApplyNoMask.Instance);
                  userContext.CommitAndStartNewTransactionIfLastMutationAndNoErrors(context, modelContextBuilder => modelContextBuilder.BuildImmediate());

                  return deleted;
              });

            Field<ValidatorContextType>("manage_upsertValidatorContext")
              .Arguments(
                new QueryArgument<NonNullGraphType<UpsertValidatorContextInputType>> { Name = "context" }
              )
              .ResolveAsync(async context =>
              {
                  var userContext = context.GetUserContext();

                  var contextInput = context.GetArgument<UpsertValidatorContextInput>("context")!;

                  var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                  CheckModifyManagementThrow(userContext, metaConfiguration, "modify cl configs");

                  using var config = JsonDocument.Parse(contextInput.Config);

                  var updated = new ValidatorContextV1(contextInput.ID, contextInput.ValidatorReference, config);

                  var newContext = await validatorContextModel.InsertOrUpdate(updated,
                      metaConfiguration.ConfigLayerset, metaConfiguration.ConfigWriteLayer,
                      userContext.ChangesetProxy, userContext.Transaction, MaskHandlingForRemovalApplyNoMask.Instance);
                  userContext.CommitAndStartNewTransactionIfLastMutationAndNoErrors(context, modelContextBuilder => modelContextBuilder.BuildImmediate());

                  return newContext.dc;
              });

            Field<BooleanGraphType>("manage_removeValidatorContext")
              .Arguments(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id" }
              )
              .ResolveAsync(async context =>
              {
                  var userContext = context.GetUserContext();

                  var id = context.GetArgument<string>("id")!;

                  var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                  CheckModifyManagementThrow(userContext, metaConfiguration, "modify validator contexts");

                  var deleted = await validatorContextModel.TryToDelete(id,
                      metaConfiguration.ConfigLayerset, metaConfiguration.ConfigWriteLayer,
                      userContext.ChangesetProxy, userContext.Transaction, MaskHandlingForRemovalApplyNoMask.Instance);
                  userContext.CommitAndStartNewTransactionIfLastMutationAndNoErrors(context, modelContextBuilder => modelContextBuilder.BuildImmediate());

                  return deleted;
              });
        }
    }
}
