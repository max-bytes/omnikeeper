using GraphQL;
using GraphQL.Types;
using Omnikeeper.Base.AttributeValues;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Generator;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Plugins;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.GraphQL.Types;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Omnikeeper.GraphQL
{
    public partial class GraphQLMutation : ObjectGraphType
    {
        private readonly ILayerModel layerModel;
        private readonly GenericTraitEntityModel<Predicate, string> predicateModel;
        private readonly IChangesetModel changesetModel;
        private readonly GenericTraitEntityModel<GeneratorV1, string> generatorModel;
        private readonly IOIAContextModel oiaContextModel;
        private readonly IODataAPIContextModel odataAPIContextModel;
        private readonly GenericTraitEntityModel<AuthRole, string> authRoleModel;
        private readonly GenericTraitEntityModel<RecursiveTrait, string> recursiveDataTraitModel;
        private readonly ILayerDataModel layerDataModel;
        private readonly IBaseConfigurationModel baseConfigurationModel;
        private readonly IManagementAuthorizationService managementAuthorizationService;
        private readonly GenericTraitEntityModel<CLConfigV1, string> clConfigModel;
        private readonly IMetaConfigurationModel metaConfigurationModel;
        private readonly IBaseAttributeRevisionistModel baseAttributeRevisionistModel;
        private readonly IBaseRelationRevisionistModel baseRelationRevisionistModel;
        private readonly IScheduler scheduler;
        private readonly ILayerBasedAuthorizationService layerBasedAuthorizationService;

        public GraphQLMutation(ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel, ILayerModel layerModel,
            GenericTraitEntityModel<Predicate, string> predicateModel, IChangesetModel changesetModel, GenericTraitEntityModel<GeneratorV1, string> generatorModel,
            IOIAContextModel oiaContextModel, IODataAPIContextModel odataAPIContextModel, GenericTraitEntityModel<AuthRole, string> authRoleModel,
            GenericTraitEntityModel<RecursiveTrait, string> recursiveDataTraitModel, IBaseConfigurationModel baseConfigurationModel,
            IManagementAuthorizationService managementAuthorizationService, GenericTraitEntityModel<CLConfigV1, string> clConfigModel, IMetaConfigurationModel metaConfigurationModel,
            IBaseAttributeRevisionistModel baseAttributeRevisionistModel, IBaseRelationRevisionistModel baseRelationRevisionistModel,
            IEnumerable<IPluginRegistration> plugins, IScheduler scheduler,
            ICIBasedAuthorizationService ciBasedAuthorizationService, ILayerBasedAuthorizationService layerBasedAuthorizationService, ILayerDataModel layerDataModel)
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

                    var userContext = await context.SetupUserContext()
                        .WithTransaction(modelContextBuilder => modelContextBuilder.BuildDeferred())
                        .WithTimeThreshold(TimeThreshold.BuildLatest(), context.Path)
                        .WithLayersetAsync(async trans => await layerModel.BuildLayerSet(readLayerIDs, trans), context.Path);

                    if (!layerBasedAuthorizationService.CanUserWriteToLayer(userContext.User, writeLayerID))
                        throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to write to the layerID: {writeLayerID}");
                    if (!layerBasedAuthorizationService.CanUserReadFromAllLayers(userContext.User, readLayerIDs))
                        throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to read from at least one of the following layerIDs: {string.Join(',', readLayerIDs)}");

                    var writeCIIDs = insertAttributes.Select(a => a.CI)
                    .Concat(removeAttributes.Select(a => a.CI))
                    .Concat(insertRelations.SelectMany(a => new Guid[] { a.FromCIID, a.ToCIID }))
                    .Concat(removeRelations.SelectMany(a => new Guid[] { a.FromCIID, a.ToCIID }))
                    .Distinct();
                    if (!ciBasedAuthorizationService.CanWriteToAllCIs(writeCIIDs, out var notAllowedCI))
                        throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to write to CI {notAllowedCI}");

                    var changeset = new ChangesetProxy(userContext.User.InDatabase, userContext.GetTimeThreshold(context.Path), changesetModel);

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

                            var changed = await attributeModel.InsertAttribute(attribute.Name, nonGenericAttributeValue, ciIdentity, writeLayerID, changeset, new DataOriginV1(DataOriginType.Manual), userContext.Transaction, otherLayersValueHandling);
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
                            var changed = await attributeModel.RemoveAttribute(attribute.Name, ciIdentity, writeLayerID, changeset, new DataOriginV1(DataOriginType.Manual), userContext.Transaction, maskHandlingForRemoval);
                            if (changed)
                                affectedCIIDs.Add(ciIdentity);
                        }
                    }

                    foreach (var insertRelation in insertRelations)
                    {
                        var changed = await relationModel.InsertRelation(insertRelation.FromCIID, insertRelation.ToCIID, insertRelation.PredicateID, insertRelation.Mask, writeLayerID, changeset, new DataOriginV1(DataOriginType.Manual), userContext.Transaction, otherLayersValueHandling);
                        if (changed)
                        {
                            affectedCIIDs.Add(insertRelation.FromCIID);
                            affectedCIIDs.Add(insertRelation.ToCIID);
                        }
                    }

                    foreach (var removeRelation in removeRelations)
                    {
                        var changed = await relationModel.RemoveRelation(removeRelation.FromCIID, removeRelation.ToCIID, removeRelation.PredicateID, writeLayerID, changeset, new DataOriginV1(DataOriginType.Manual), userContext.Transaction, maskHandlingForRemoval);
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

                    userContext.CommitAndStartNewTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate());

                    return new MutateReturn(affectedCIs);
                });

            FieldAsync<CreateCIsReturnType>("createCIs",
                arguments: new QueryArguments(
                new QueryArgument<ListGraphType<CreateCIInputType>> { Name = "cis" }
                ),
                resolve: async context =>
                {
                    var createCIs = context.GetArgument("cis", new List<CreateCIInput>())!;

                    var userContext = context.SetupUserContext()
                        .WithTransaction(modelContextBuilder => modelContextBuilder.BuildDeferred())
                        .WithTimeThreshold(TimeThreshold.BuildLatest(), context.Path);

                    if (!layerBasedAuthorizationService.CanUserWriteToAllLayers(userContext.User, createCIs.Select(ci => ci.LayerIDForName)))
                        throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to write to at least one of the following layerIDs: {string.Join(',', createCIs.Select(ci => ci.LayerIDForName))}");
                    // NOTE: a newly created CI cannot be checked with CIBasedAuthorizationService yet. That's why we don't do a .CanWriteToCI() check here

                    var changeset = new ChangesetProxy(userContext.User.InDatabase, userContext.GetTimeThreshold(context.Path), changesetModel);

                    // TODO: other-layers-value handling
                    var otherLayersValueHandling = OtherLayersValueHandlingForceWrite.Instance;

                    var createdCIIDs = new List<Guid>();
                    foreach (var ci in createCIs)
                    {
                        Guid ciid = await ciModel.CreateCI(userContext.Transaction);

                        await attributeModel.InsertCINameAttribute(ci.Name, ciid, ci.LayerIDForName, changeset, new DataOriginV1(DataOriginType.Manual), userContext.Transaction, otherLayersValueHandling);

                        createdCIIDs.Add(ciid);
                    }
                    userContext.CommitAndStartNewTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate());

                    return new CreateCIsReturn(createdCIIDs);
                });

            this.layerModel = layerModel;
            this.predicateModel = predicateModel;
            this.changesetModel = changesetModel;
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
            this.scheduler = scheduler;
            this.layerBasedAuthorizationService = layerBasedAuthorizationService;
            this.layerDataModel = layerDataModel;

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
