using Autofac.Features.Indexed;
using GraphQL;
using GraphQL.Types;
using Omnikeeper.Base.AttributeValues;
using Omnikeeper.Base.CLB;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Plugins;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Authz;
using Omnikeeper.Base.Utils;
using Omnikeeper.GraphQL.Types;
using Omnikeeper.Model.Config;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Omnikeeper.GraphQL
{
    public partial class GraphQLMutation : ObjectGraphType
    {
        private readonly ILayerModel layerModel;
        private readonly PredicateModel predicateModel;
        private readonly GeneratorV1Model generatorModel;
        private readonly IOIAContextModel oiaContextModel;
        private readonly ODataAPIContextModel odataAPIContextModel;
        private readonly AuthRoleModel authRoleModel;
        private readonly RecursiveTraitModel recursiveDataTraitModel;
        private readonly ILayerDataModel layerDataModel;
        private readonly IBaseConfigurationModel baseConfigurationModel;
        private readonly IManagementAuthorizationService managementAuthorizationService;
        private readonly CLConfigV1Model clConfigModel;
        private readonly ValidatorContextV1Model validatorContextModel;
        private readonly IMetaConfigurationModel metaConfigurationModel;
        private readonly IBaseAttributeRevisionistModel baseAttributeRevisionistModel;
        private readonly IBaseRelationRevisionistModel baseRelationRevisionistModel;
        private readonly CLBProcessedChangesetsCache clbProcessedChangesetsCache;
        private readonly IAuthzFilterManager authzFilterManager;
        private readonly ValidatorProcessedChangesetsCache validatorProcessedChangesetsCache;
        private readonly IScheduler localScheduler;

        public GraphQLMutation(ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel, ILayerModel layerModel,
            PredicateModel predicateModel, GeneratorV1Model generatorModel,
            IOIAContextModel oiaContextModel, ODataAPIContextModel odataAPIContextModel, AuthRoleModel authRoleModel,
            RecursiveTraitModel recursiveDataTraitModel, IBaseConfigurationModel baseConfigurationModel, ChangesetDataModel changesetDataModel,
            IManagementAuthorizationService managementAuthorizationService, CLConfigV1Model clConfigModel, IMetaConfigurationModel metaConfigurationModel,
            IBaseAttributeRevisionistModel baseAttributeRevisionistModel, IBaseRelationRevisionistModel baseRelationRevisionistModel,
            IEnumerable<IPluginRegistration> plugins, IIndex<string, IScheduler> schedulers, CLBProcessedChangesetsCache clbProcessedChangesetsCache,
            IAuthzFilterManager authzFilterManager, ICIBasedAuthorizationService ciBasedAuthorizationService, ILayerDataModel layerDataModel, ValidatorContextV1Model validatorContextModel, ValidatorProcessedChangesetsCache validatorProcessedChangesetsCache)
        {
            FieldAsync<MutateReturnType>("mutateCIs",
                arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "writeLayer" },
                new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "readLayers" },
                new QueryArgument<ListGraphType<InsertCIAttributeInputType>> { Name = "InsertAttributes" },
                new QueryArgument<ListGraphType<RemoveCIAttributeInputType>> { Name = "RemoveAttributes" },
                new QueryArgument<ListGraphType<InsertRelationInputType>> { Name = "InsertRelations" },
                new QueryArgument<ListGraphType<RemoveRelationInputType>> { Name = "RemoveRelations" }
                ),
                resolve: async context =>
                {
                    var writeLayerID = context.GetArgument<string>("writeLayer")!;
                    var readLayerIDs = context.GetArgument<string[]>("readLayers")!;
                    var insertAttributes = context.GetArgument("InsertAttributes", new List<InsertCIAttributeInput>())!;
                    var removeAttributes = context.GetArgument("RemoveAttributes", new List<RemoveCIAttributeInput>())!;
                    var insertRelations = context.GetArgument("InsertRelations", new List<InsertRelationInput>())!;
                    var removeRelations = context.GetArgument("RemoveRelations", new List<RemoveRelationInput>())!;

                    var userContext = await context.GetUserContext()
                        .WithLayersetAsync(async trans => await layerModel.BuildLayerSet(readLayerIDs, trans), context.Path);

                    if (await authzFilterManager.ApplyPreFilterForMutation(new PreMutateContextForCIs(), writeLayerID, userContext, context.Path) is AuthzFilterResultDeny d)
                        throw new ExecutionError(d.Reason);

                    var writeCIIDs = insertAttributes.Select(a => a.CI)
                    .Concat(removeAttributes.Select(a => a.CI))
                    .Concat(insertRelations.SelectMany(a => new Guid[] { a.FromCIID, a.ToCIID }))
                    .Concat(removeRelations.SelectMany(a => new Guid[] { a.FromCIID, a.ToCIID }))
                    .Distinct();
                    if (!ciBasedAuthorizationService.CanWriteToAllCIs(writeCIIDs, out var notAllowedCI))
                        throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to write to CI {notAllowedCI}");

                    var changeset = userContext.ChangesetProxy;

                    // TODO: replace with bulk update
                    var affectedCIIDs = new HashSet<Guid>();

                    // TODO: other-layers-value handling
                    var otherLayersValueHandling = OtherLayersValueHandlingForceWrite.Instance;
                    // TODO: mask handling
                    var maskHandlingForRemoval = MaskHandlingForRemovalApplyNoMask.Instance;

                    var groupedInsertAttributes = insertAttributes.GroupBy(a => a.CI);
                    foreach (var attributeGroup in groupedInsertAttributes)
                    {
                        // look for ciid
                        var ciIdentity = attributeGroup.Key;
                        foreach (var attribute in attributeGroup)
                        {
                            var nonGenericAttributeValue = AttributeValueHelper.BuildFromDTO(attribute.Value);

                            var changed = await attributeModel.InsertAttribute(attribute.Name, nonGenericAttributeValue, ciIdentity, writeLayerID, changeset, userContext.Transaction, otherLayersValueHandling);
                            if (changed)
                                affectedCIIDs.Add(ciIdentity);
                        }
                    }


                    var groupedRemoveAttributes = removeAttributes.GroupBy(a => a.CI);
                    foreach (var attributeGroup in groupedRemoveAttributes)
                    {
                        // look for ciid
                        var ciIdentity = attributeGroup.Key;
                        foreach (var attribute in attributeGroup)
                        {
                            // TODO: mask handling
                            var changed = await attributeModel.RemoveAttribute(attribute.Name, ciIdentity, writeLayerID, changeset, userContext.Transaction, maskHandlingForRemoval);
                            if (changed)
                                affectedCIIDs.Add(ciIdentity);
                        }
                    }

                    foreach (var insertRelation in insertRelations)
                    {
                        var changed = await relationModel.InsertRelation(insertRelation.FromCIID, insertRelation.ToCIID, insertRelation.PredicateID, insertRelation.Mask, writeLayerID, changeset, userContext.Transaction, otherLayersValueHandling);
                        if (changed)
                        {
                            affectedCIIDs.Add(insertRelation.FromCIID);
                            affectedCIIDs.Add(insertRelation.ToCIID);
                        }
                    }

                    foreach (var removeRelation in removeRelations)
                    {
                        var changed = await relationModel.RemoveRelation(removeRelation.FromCIID, removeRelation.ToCIID, removeRelation.PredicateID, writeLayerID, changeset, userContext.Transaction, maskHandlingForRemoval);
                        if (changed)
                        {
                            affectedCIIDs.Add(removeRelation.FromCIID);
                            affectedCIIDs.Add(removeRelation.ToCIID);
                        }

                        // TODO: support for masking of relations
                    }

                    IEnumerable<MergedCI> affectedCIs = new List<MergedCI>();
                    if (!affectedCIIDs.IsEmpty())
                        affectedCIs = await ciModel.GetMergedCIs(SpecificCIIDsSelection.Build(affectedCIIDs), userContext.GetLayerSet(context.Path), true, AllAttributeSelection.Instance, userContext.Transaction, userContext.GetTimeThreshold(context.Path));

                    if (await authzFilterManager.ApplyPostFilterForMutation(new PostMutateContextForCIs(), writeLayerID, userContext, context.Path) is AuthzFilterResultDeny dPost)
                        throw new ExecutionError(dPost.Reason);

                    userContext.CommitAndStartNewTransactionIfLastMutation(context, modelContextBuilder => modelContextBuilder.BuildImmediate());

                    return new MutateReturn(affectedCIs);
                });

            FieldAsync<CreateCIsReturnType>("createCIs",
                arguments: new QueryArguments(
                new QueryArgument<ListGraphType<CreateCIInputType>> { Name = "cis" }
                ),
                resolve: async context =>
                {
                    var createCIs = context.GetArgument("cis", new List<CreateCIInput>())!;

                    var userContext = context.GetUserContext();


                    var layers = createCIs.Select(ci => ci.LayerIDForName);
                    // TODO: this is not ideal; we should split up writes to multiple layers better
                    foreach (var layer in layers)
                        if (await authzFilterManager.ApplyPreFilterForMutation(new PreCreateContextForCIs(), userContext.User, layer, layer, userContext.Transaction, userContext.GetTimeThreshold(context.Path)) is AuthzFilterResultDeny d)
                            throw new ExecutionError(d.Reason);

                    // TODO: other-layers-value handling
                    var otherLayersValueHandling = OtherLayersValueHandlingForceWrite.Instance;

                    var createdCIIDs = new List<Guid>();
                    foreach (var ci in createCIs)
                    {
                        Guid ciid = await ciModel.CreateCI(userContext.Transaction);

                        await attributeModel.InsertCINameAttribute(ci.Name, ciid, ci.LayerIDForName, userContext.ChangesetProxy, userContext.Transaction, otherLayersValueHandling);

                        createdCIIDs.Add(ciid);
                    }

                    // TODO: this is not ideal; we should split up writes to multiple layers better
                    foreach(var layer in layers)
                        if (await authzFilterManager.ApplyPostFilterForMutation(new PostCreateContextForCIs(), layer, userContext, context.Path) is AuthzFilterResultDeny dPost)
                            throw new ExecutionError(dPost.Reason);

                    userContext.CommitAndStartNewTransactionIfLastMutation(context, modelContextBuilder => modelContextBuilder.BuildImmediate());

                    return new CreateCIsReturn(createdCIIDs);
                });

            FieldAsync<InsertChangesetDataReturnType>("insertChangesetData",
                arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "layer" },
                new QueryArgument<NonNullGraphType<ListGraphType<InsertChangesetDataAttributeInputType>>> { Name = "attributes" }
                ),
                resolve: async context =>
                {
                    var layerID = context.GetArgument<string>("layer")!;
                    var insertAttributes = context.GetArgument("attributes", new List<InsertChangesetDataAttributeInput>())!;

                    var userContext = await context.GetUserContext()
                        .WithLayersetAsync(async trans => await layerModel.BuildLayerSet(new string[] { layerID }, trans), context.Path);

                    if (await authzFilterManager.ApplyPreFilterForMutation(new PreInsertChangesetDataContextForCIs(), layerID, userContext, context.Path) is AuthzFilterResultDeny d)
                        throw new ExecutionError(d.Reason);

                    var changesetProxy = userContext.ChangesetProxy;

                    var ciid = await changesetDataModel.InsertOrUpdateWithAdditionalAttributes(changesetProxy, layerID, insertAttributes.Select(a => (name: a.Name, value: AttributeValueHelper.BuildFromDTO(a.Value))), userContext.Transaction);

                    if (await authzFilterManager.ApplyPostFilterForMutation(new PostInsertChangesetDataContextForCIs(), layerID, userContext, context.Path) is AuthzFilterResultDeny dPost)
                        throw new ExecutionError(dPost.Reason);

                    userContext.CommitAndStartNewTransactionIfLastMutation(context, modelContextBuilder => modelContextBuilder.BuildImmediate());

                    return new InsertChangesetDataReturn(ciid);
                });

            this.layerModel = layerModel;
            this.predicateModel = predicateModel;
            this.generatorModel = generatorModel;
            this.oiaContextModel = oiaContextModel;
            this.odataAPIContextModel = odataAPIContextModel;
            this.authRoleModel = authRoleModel;
            this.recursiveDataTraitModel = recursiveDataTraitModel;
            this.baseConfigurationModel = baseConfigurationModel;
            this.managementAuthorizationService = managementAuthorizationService;
            this.clConfigModel = clConfigModel;
            this.metaConfigurationModel = metaConfigurationModel;
            this.baseAttributeRevisionistModel = baseAttributeRevisionistModel;
            this.baseRelationRevisionistModel = baseRelationRevisionistModel;
            this.clbProcessedChangesetsCache = clbProcessedChangesetsCache;
            this.authzFilterManager = authzFilterManager;
            this.layerDataModel = layerDataModel;
            this.validatorContextModel = validatorContextModel;
            this.validatorProcessedChangesetsCache = validatorProcessedChangesetsCache;
            localScheduler = schedulers["localScheduler"];

            CreateManage();
            CreatePlugin(plugins);
        }


        private void CreatePlugin(IEnumerable<IPluginRegistration> plugins)
        {
            foreach (var plugin in plugins)
            {
                plugin.RegisterGraphqlMutations(this);
            }
        }
    }
}
