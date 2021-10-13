using GraphQL;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Omnikeeper.Base.AttributeValues;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Generator;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
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
        private readonly IBaseConfigurationModel baseConfigurationModel;
        private readonly IManagementAuthorizationService managementAuthorizationService;
        private readonly GenericTraitEntityModel<CLConfigV1, string> clConfigModel;
        private readonly IMetaConfigurationModel metaConfigurationModel;
        private readonly IBaseAttributeRevisionistModel baseAttributeRevisionistModel;
        private readonly IBaseRelationRevisionistModel baseRelationRevisionistModel;
        private readonly ILayerBasedAuthorizationService layerBasedAuthorizationService;

        public GraphQLMutation(ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel, ILayerModel layerModel,
            GenericTraitEntityModel<Predicate, string> predicateModel, IChangesetModel changesetModel, GenericTraitEntityModel<GeneratorV1, string> generatorModel,
            IOIAContextModel oiaContextModel, IODataAPIContextModel odataAPIContextModel, GenericTraitEntityModel<AuthRole, string> authRoleModel,
            GenericTraitEntityModel<RecursiveTrait, string> recursiveDataTraitModel, IBaseConfigurationModel baseConfigurationModel,
            IManagementAuthorizationService managementAuthorizationService, GenericTraitEntityModel<CLConfigV1, string> clConfigModel, IMetaConfigurationModel metaConfigurationModel,
            IBaseAttributeRevisionistModel baseAttributeRevisionistModel, IBaseRelationRevisionistModel baseRelationRevisionistModel,
            ICIBasedAuthorizationService ciBasedAuthorizationService, ILayerBasedAuthorizationService layerBasedAuthorizationService)
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
                    var layers = context.GetArgument<string[]?>("layers", null);
                    var insertAttributes = context.GetArgument("InsertAttributes", new List<InsertCIAttributeInput>());
                    var removeAttributes = context.GetArgument("RemoveAttributes", new List<RemoveCIAttributeInput>());
                    var insertRelations = context.GetArgument("InsertRelations", new List<InsertRelationInput>())!;
                    var removeRelations = context.GetArgument("RemoveRelations", new List<RemoveRelationInput>())!;

                    var userContext = await context.SetupUserContext()
                        .WithTransaction(modelContextBuilder => modelContextBuilder.BuildDeferred())
                        .WithTimeThreshold(() => TimeThreshold.BuildLatest())
                        .WithLayerset(async trans => layers != null ? await layerModel.BuildLayerSet(layers, trans) : null);

                    var writeLayerIDs = insertAttributes.Select(a => a.LayerID)
                    .Concat(removeAttributes.Select(a => a.LayerID))
                    .Concat(insertRelations.Select(a => a.LayerID))
                    .Concat(removeRelations.Select(a => a.LayerID))
                    .Distinct();
                    if (!layerBasedAuthorizationService.CanUserWriteToAllLayers(userContext.User, writeLayerIDs))
                        throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to write to at least one of the following layerIDs: {string.Join(',', writeLayerIDs)}");

                    var writeCIIDs = insertAttributes.Select(a => a.CI)
                    .Concat(removeAttributes.Select(a => a.CI))
                    .Concat(insertRelations.SelectMany(a => new Guid[] { a.FromCIID, a.ToCIID }))
                    .Concat(removeRelations.SelectMany(a => new Guid[] { a.FromCIID, a.ToCIID }))
                    .Distinct();
                    if (!ciBasedAuthorizationService.CanWriteToAllCIs(writeCIIDs, out var notAllowedCI))
                        throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to write to CI {notAllowedCI}");

                    var changeset = new ChangesetProxy(userContext.User.InDatabase, userContext.TimeThreshold, changesetModel);

                    var groupedInsertAttributes = insertAttributes.GroupBy(a => a.CI);
                    var insertedAttributes = new List<CIAttribute>();
                    foreach (var attributeGroup in groupedInsertAttributes)
                    {
                        // look for ciid
                        var ciIdentity = attributeGroup.Key;
                        foreach (var attribute in attributeGroup)
                        {
                            var nonGenericAttributeValue = AttributeValueBuilder.BuildFromDTO(attribute.Value);

                            var (a, changed) = await attributeModel.InsertAttribute(attribute.Name, nonGenericAttributeValue, ciIdentity, attribute.LayerID, changeset, new DataOriginV1(DataOriginType.Manual), userContext.Transaction);
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
                            var (a, changed) = await attributeModel.RemoveAttribute(attribute.Name, ciIdentity, attribute.LayerID, changeset, new DataOriginV1(DataOriginType.Manual), userContext.Transaction);
                            removedAttributes.Add(a);
                        }
                    }

                    var insertedRelations = new List<Relation>();
                    foreach (var insertRelation in insertRelations)
                    {
                        var (r, changed) = await relationModel.InsertRelation(insertRelation.FromCIID, insertRelation.ToCIID, insertRelation.PredicateID, insertRelation.LayerID, changeset, new DataOriginV1(DataOriginType.Manual), userContext.Transaction);
                        insertedRelations.Add(r);
                    }

                    var removedRelations = new List<Relation>();
                    foreach (var removeRelation in removeRelations)
                    {
                        var (r, changed) = await relationModel.RemoveRelation(removeRelation.FromCIID, removeRelation.ToCIID, removeRelation.PredicateID, removeRelation.LayerID, changeset, new DataOriginV1(DataOriginType.Manual), userContext.Transaction);
                        removedRelations.Add(r);
                    }

                    IEnumerable<MergedCI> affectedCIs = new List<MergedCI>(); ;
                    if (userContext.LayerSet != null)
                    {
                        var affectedCIIDs = removedAttributes.Select(r => r.CIID)
                        .Concat(insertedAttributes.Select(i => i.CIID))
                        .Concat(insertedRelations.SelectMany(i => new Guid[] { i.FromCIID, i.ToCIID }))
                        .Concat(removedRelations.SelectMany(i => new Guid[] { i.FromCIID, i.ToCIID }))
                        .ToHashSet();
                        if (!affectedCIIDs.IsEmpty())
                            affectedCIs = await ciModel.GetMergedCIs(SpecificCIIDsSelection.Build(affectedCIIDs), userContext.LayerSet, true, AllAttributeSelection.Instance, userContext.Transaction, userContext.TimeThreshold);
                    }

                    userContext.CommitAndStartNewTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate());

                    return new MutateReturn(insertedAttributes, removedAttributes, insertedRelations, affectedCIs);
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
                        .WithTimeThreshold(() => TimeThreshold.BuildLatest());

                    if (!layerBasedAuthorizationService.CanUserWriteToAllLayers(userContext.User, createCIs.Select(ci => ci.LayerIDForName)))
                        throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to write to at least one of the following layerIDs: {string.Join(',', createCIs.Select(ci => ci.LayerIDForName))}");
                    // NOTE: a newly created CI cannot be checked with CIBasedAuthorizationService yet. That's why we don't do a .CanWriteToCI() check here

                    var changeset = new ChangesetProxy(userContext.User.InDatabase, userContext.TimeThreshold, changesetModel);

                    var createdCIIDs = new List<Guid>();
                    foreach (var ci in createCIs)
                    {
                        Guid ciid = await ciModel.CreateCI(userContext.Transaction);

                        await attributeModel.InsertCINameAttribute(ci.Name, ciid, ci.LayerIDForName, changeset, new DataOriginV1(DataOriginType.Manual), userContext.Transaction);

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
            this.layerBasedAuthorizationService = layerBasedAuthorizationService;

            CreateManage();
        }
    }
}
