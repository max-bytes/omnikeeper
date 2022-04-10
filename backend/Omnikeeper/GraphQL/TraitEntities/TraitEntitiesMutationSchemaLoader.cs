using GraphQL;
using GraphQL.Types;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.GraphQL.TraitEntities
{
    public class TraitEntitiesMutationSchemaLoader
    {
        private readonly IAttributeModel attributeModel;
        private readonly IRelationModel relationModel;
        private readonly IEffectiveTraitModel effectiveTraitModel;
        private readonly ICIModel ciModel;
        private readonly ILayerModel layerModel;
        private readonly IChangesetModel changesetModel;
        private readonly ILayerBasedAuthorizationService layerBasedAuthorizationService;

        public TraitEntitiesMutationSchemaLoader(IAttributeModel attributeModel, IRelationModel relationModel,
            IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, ILayerModel layerModel, IChangesetModel changesetModel, ILayerBasedAuthorizationService layerBasedAuthorizationService)
        {
            this.attributeModel = attributeModel;
            this.relationModel = relationModel;
            this.effectiveTraitModel = effectiveTraitModel;
            this.ciModel = ciModel;
            this.layerModel = layerModel;
            this.changesetModel = changesetModel;
            this.layerBasedAuthorizationService = layerBasedAuthorizationService;
        }

        private async Task<EffectiveTrait> InsertUsingNewCI((string name, IAttributeValue value, bool isID)[] attributeValues, (string predicateID, bool forward, Guid[] relatedCIIDs)[] relationValues, string? ciName, IModelContext trans, IChangesetProxy changeset, TraitEntityModel traitEntityModel, LayerSet layerset, string writeLayerID)
        {
            var finalCIID = await ciModel.CreateCI(trans);

            var attributeFragments = attributeValues.Select(i => new BulkCIAttributeDataCIAndAttributeNameScope.Fragment(finalCIID, i.name, i.value));
            IList<(Guid thisCIID, string predicateID, Guid[] otherCIIDs)> incomingRelations = relationValues.Where(rv => !rv.forward).Select(rv => (finalCIID, rv.predicateID, rv.relatedCIIDs)).ToList();
            IList<(Guid thisCIID, string predicateID, Guid[] otherCIIDs)> outgoingRelations = relationValues.Where(rv => rv.forward).Select(rv => (finalCIID, rv.predicateID, rv.relatedCIIDs)).ToList();
            var t = await traitEntityModel.InsertOrUpdateFull(finalCIID, attributeFragments, outgoingRelations, incomingRelations, ciName, layerset, writeLayerID, new DataOriginV1(DataOriginType.Manual), changeset, trans, MaskHandlingForRemovalApplyNoMask.Instance);
            return t.et;
        }

        private async Task<EffectiveTrait> Update(Guid ciid, (string name, IAttributeValue value, bool isID)[] attributeValues, string? ciName, IModelContext trans, IChangesetProxy changeset, TraitEntityModel traitEntityModel, LayerSet layerset, string writeLayerID)
        {
            var attributeFragments = attributeValues.Select(i => new BulkCIAttributeDataCIAndAttributeNameScope.Fragment(ciid, i.name, i.value));

            var t = await traitEntityModel.InsertOrUpdateAttributesOnly(ciid, attributeFragments, ciName, layerset, writeLayerID, new DataOriginV1(DataOriginType.Manual), changeset, trans, MaskHandlingForRemovalApplyNoMask.Instance);
            return t.et;
        }

        public void Init(GraphQLMutation tet, TypeContainer typeContainer)
        {
            foreach (var elementTypeContainer in typeContainer.ElementTypes)
            {
                var traitID = elementTypeContainer.Trait.ID;

                var traitEntityModel = new TraitEntityModel(elementTypeContainer.Trait, effectiveTraitModel, ciModel, attributeModel, relationModel);

                tet.FieldAsync(TraitEntityTypesNameGenerator.GenerateUpdateByCIIDMutationName(traitID), elementTypeContainer.ElementWrapper,
                    arguments: new QueryArguments(
                        new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" },
                        new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "writeLayer" },
                        new QueryArgument(new NonNullGraphType(elementTypeContainer.UpsertInputType)) { Name = "input" },
                        new QueryArgument<NonNullGraphType<GuidGraphType>> { Name = "ciid" },
                        new QueryArgument<StringGraphType> { Name = "ciName" }),
                    resolve: async context =>
                    {
                        var layerStrings = context.GetArgument<string[]>("layers")!;
                        var writeLayerID = context.GetArgument<string>("writeLayer")!;
                        var ciid = context.GetArgument<Guid>("ciid");
                        var ciName = context.GetArgument<string?>("ciName", null);

                        var userContext = await context.SetupUserContext()
                            .WithTimeThreshold(TimeThreshold.BuildLatest(), context.Path)
                            .WithTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate())
                            .WithLayersetAsync(async trans => await layerModel.BuildLayerSet(layerStrings, trans), context.Path);

                        var layerset = userContext.GetLayerSet(context.Path);
                        var timeThreshold = userContext.GetTimeThreshold(context.Path);
                        var trans = userContext.Transaction;

                        if (!layerBasedAuthorizationService.CanUserWriteToLayer(userContext.User, writeLayerID))
                            throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to write to the layerID: {writeLayerID}");
                        if (!layerBasedAuthorizationService.CanUserReadFromAllLayers(userContext.User, layerset))
                            throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to read from at least one of the following layerIDs: {string.Join(',', layerset)}");

                        var input = context.GetArgument<UpsertInput>("input");

                        var changeset = new ChangesetProxy(userContext.User.InDatabase, userContext.GetTimeThreshold(context.Path), changesetModel);

                        // check if entity actually exists at that CI, error if not
                        var existingEntity = await traitEntityModel.GetSingleByCIID(ciid, layerset, trans, timeThreshold);
                        if (existingEntity == null)
                        {
                            throw new Exception($"Cannot update entity at CI with ID {ciid}: entity does not exist at that CI");
                        }

                        var et = await Update(ciid, input.AttributeValues, ciName, trans, changeset, traitEntityModel, layerset, writeLayerID);

                        userContext.CommitAndStartNewTransaction(mc => mc.BuildImmediate());

                        return et;
                    });

                tet.FieldAsync(TraitEntityTypesNameGenerator.GenerateInsertNewMutationName(traitID), elementTypeContainer.ElementWrapper,
                    arguments: new QueryArguments(
                        new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" },
                        new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "writeLayer" },
                        new QueryArgument(new NonNullGraphType(elementTypeContainer.InsertInputType)) { Name = "input" },
                        new QueryArgument<StringGraphType> { Name = "ciName" }),
                    resolve: async context =>
                    {
                        var layerStrings = context.GetArgument<string[]>("layers")!;
                        var writeLayerID = context.GetArgument<string>("writeLayer")!;
                        var ciName = context.GetArgument<string?>("ciName", null);

                        var userContext = await context.SetupUserContext()
                            .WithTimeThreshold(TimeThreshold.BuildLatest(), context.Path)
                            .WithTransaction(modelContextBuilder => modelContextBuilder.BuildDeferred())
                            .WithLayersetAsync(async trans => await layerModel.BuildLayerSet(layerStrings, trans), context.Path);

                        var layerset = userContext.GetLayerSet(context.Path);
                        var timeThreshold = userContext.GetTimeThreshold(context.Path);
                        var trans = userContext.Transaction;

                        if (!layerBasedAuthorizationService.CanUserWriteToLayer(userContext.User, writeLayerID))
                            throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to write to the layerID: {writeLayerID}");
                        if (!layerBasedAuthorizationService.CanUserReadFromAllLayers(userContext.User, layerset))
                            throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to read from at least one of the following layerIDs: {string.Join(',', layerset)}");

                        var insertInput = context.GetArgument<InsertInput>("input");

                        // check if the trait entity has an ID, and if so, check that there is no existing entity with that ID
                        if (elementTypeContainer.IDInputType != null)
                        {
                            var idAttributeTuples = insertInput.AttributeValues.Where(t => t.isID).Select(t => (t.name, t.value)).ToArray();
                            var currentCIID = await TraitEntityHelper.GetMatchingCIIDByAttributeValues(attributeModel, idAttributeTuples, layerset, trans, timeThreshold);
                            if (currentCIID.HasValue)
                            { // there is already a trait entity with that ID -> error
                                throw new Exception($"A CI with that data ID already exists; CIID: {currentCIID.Value})");
                            }
                        }

                        var changeset = new ChangesetProxy(userContext.User.InDatabase, userContext.GetTimeThreshold(context.Path), changesetModel);

                        var et = await InsertUsingNewCI(insertInput.AttributeValues, insertInput.RelationValues, ciName, trans, changeset, traitEntityModel, layerset, writeLayerID);

                        userContext.CommitAndStartNewTransaction(mc => mc.BuildImmediate());

                        return et;
                    });

                var deleteByCIIDMutationName = TraitEntityTypesNameGenerator.GenerateDeleteByCIIDMutationName(traitID);
                tet.FieldAsync(deleteByCIIDMutationName, new BooleanGraphType(),
                    arguments: new QueryArguments(
                        new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" },
                        new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "writeLayer" },
                        new QueryArgument<NonNullGraphType<GuidGraphType>> { Name = "ciid" }
                    ),
                    description: @"Note on the return value: unlike deleteByDataID*, deleteByCIID* return true whether or not there was a trait entity at the
                        specified CIID or not. There is no check beforehand if the trait entity exists.",
                    resolve: async context =>
                    {
                        var layerStrings = context.GetArgument<string[]>("layers")!;
                        var writeLayerID = context.GetArgument<string>("writeLayer")!;
                        var ciid = context.GetArgument<Guid>("ciid")!;

                        var userContext = await context.SetupUserContext()
                            .WithTimeThreshold(TimeThreshold.BuildLatest(), context.Path)
                            .WithTransaction(modelContextBuilder => modelContextBuilder.BuildDeferred())
                            .WithLayersetAsync(async trans => await layerModel.BuildLayerSet(layerStrings, trans), context.Path);

                        var layerset = userContext.GetLayerSet(context.Path);
                        var timeThreshold = userContext.GetTimeThreshold(context.Path);
                        var trans = userContext.Transaction;

                        if (!layerBasedAuthorizationService.CanUserWriteToLayer(userContext.User, writeLayerID))
                            throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to write to the layerID: {writeLayerID}");
                        if (!layerBasedAuthorizationService.CanUserReadFromAllLayers(userContext.User, layerset))
                            throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to read from at least one of the following layerIDs: {string.Join(',', layerset)}");

                        var changeset = new ChangesetProxy(userContext.User.InDatabase, userContext.GetTimeThreshold(context.Path), changesetModel);
                        var removed = await traitEntityModel.TryToDelete(ciid, layerset, writeLayerID, new DataOriginV1(DataOriginType.Manual), changeset, trans, MaskHandlingForRemovalApplyNoMask.Instance);

                        userContext.CommitAndStartNewTransaction(mc => mc.BuildImmediate());

                        return removed;
                    });

                if (elementTypeContainer.IDInputType != null) // only add *byDataID-mutations for trait entities that have an ID
                {
                    var upsertByDataIDMutationName = TraitEntityTypesNameGenerator.GenerateUpsertByDataIDMutationName(traitID);
                    tet.FieldAsync(upsertByDataIDMutationName, elementTypeContainer.ElementWrapper,
                        arguments: new QueryArguments(
                            new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" },
                            new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "writeLayer" },
                            new QueryArgument(new NonNullGraphType(elementTypeContainer.UpsertInputType)) { Name = "input" },
                            new QueryArgument<StringGraphType> { Name = "ciName" }),
                        resolve: async context =>
                        {
                            var layerStrings = context.GetArgument<string[]>("layers")!;
                            var writeLayerID = context.GetArgument<string>("writeLayer")!;
                            var ciName = context.GetArgument<string?>("ciName", null);

                            var userContext = await context.SetupUserContext()
                                .WithTimeThreshold(TimeThreshold.BuildLatest(), context.Path)
                                .WithTransaction(modelContextBuilder => modelContextBuilder.BuildDeferred())
                                .WithLayersetAsync(async trans => await layerModel.BuildLayerSet(layerStrings, trans), context.Path);

                            var layerset = userContext.GetLayerSet(context.Path);
                            var timeThreshold = userContext.GetTimeThreshold(context.Path);
                            var trans = userContext.Transaction;

                            if (!layerBasedAuthorizationService.CanUserWriteToLayer(userContext.User, writeLayerID))
                                throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to write to the layerID: {writeLayerID}");
                            if (!layerBasedAuthorizationService.CanUserReadFromAllLayers(userContext.User, layerset))
                                throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to read from at least one of the following layerIDs: {string.Join(',', layerset)}");

                            var input = context.GetArgument<UpsertInput>("input");

                            var idAttributeTuples = input.AttributeValues.Where(t => t.isID).Select(t => (t.name, t.value)).ToArray();
                            if (idAttributeTuples.IsEmpty())
                            {
                                throw new Exception("Cannot mutate trait entity that does not have proper ID field(s)");
                            }

                            var currentCIID = await TraitEntityHelper.GetMatchingCIIDByAttributeValues(attributeModel, idAttributeTuples, layerset, trans, timeThreshold);

                            var changeset = new ChangesetProxy(userContext.User.InDatabase, userContext.GetTimeThreshold(context.Path), changesetModel);

                            if (currentCIID.HasValue)
                            {
                                var et = await Update(currentCIID.Value, input.AttributeValues, ciName, trans, changeset, traitEntityModel, layerset, writeLayerID);
                                userContext.CommitAndStartNewTransaction(mc => mc.BuildImmediate());
                                return et;
                            } else
                            {
                                var inputRelationValues = Array.Empty<(string predicateID, bool forward, Guid[] relatedCIIDs)>();
                                var et = await InsertUsingNewCI(input.AttributeValues, inputRelationValues, ciName, trans, changeset, traitEntityModel, layerset, writeLayerID);
                                userContext.CommitAndStartNewTransaction(mc => mc.BuildImmediate());
                                return et;
                            }
                        });

                    var deleteByDataIDMutationName = TraitEntityTypesNameGenerator.GenerateDeleteByDataIDMutationName(traitID);
                    tet.FieldAsync(deleteByDataIDMutationName, new BooleanGraphType(),
                        arguments: new QueryArguments(
                            new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" },
                            new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "writeLayer" },
                            new QueryArgument(new NonNullGraphType(elementTypeContainer.IDInputType)) { Name = "id" }
                        ),
                        description: @"Note on the return value: deleteByDataID* only returns true if the trait entity was present 
                            (and found through its ID) first, and it is not present anymore after the deletion.",
                        resolve: async context =>
                        {
                            var layerStrings = context.GetArgument<string[]>("layers")!;
                            var writeLayerID = context.GetArgument<string>("writeLayer")!;

                            var userContext = await context.SetupUserContext()
                                .WithTimeThreshold(TimeThreshold.BuildLatest(), context.Path)
                                .WithTransaction(modelContextBuilder => modelContextBuilder.BuildDeferred())
                                .WithLayersetAsync(async trans => await layerModel.BuildLayerSet(layerStrings, trans), context.Path);

                            var layerset = userContext.GetLayerSet(context.Path);
                            var timeThreshold = userContext.GetTimeThreshold(context.Path);
                            var trans = userContext.Transaction;

                            if (!layerBasedAuthorizationService.CanUserWriteToLayer(userContext.User, writeLayerID))
                                throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to write to the layerID: {writeLayerID}");
                            if (!layerBasedAuthorizationService.CanUserReadFromAllLayers(userContext.User, layerset))
                                throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to read from at least one of the following layerIDs: {string.Join(',', layerset)}");

                            var id = context.GetArgument<IDInput>("id");

                            // TODO: use data loader?
                            var foundCIID = await TraitEntityHelper.GetMatchingCIIDByAttributeValues(attributeModel, id.AttributeValues, layerset, trans, timeThreshold);

                            if (!foundCIID.HasValue)
                                return false;

                            var changeset = new ChangesetProxy(userContext.User.InDatabase, userContext.GetTimeThreshold(context.Path), changesetModel);
                            var removed = await traitEntityModel.TryToDelete(foundCIID.Value, layerset, writeLayerID, new DataOriginV1(DataOriginType.Manual), changeset, trans, MaskHandlingForRemovalApplyNoMask.Instance);

                            userContext.CommitAndStartNewTransaction(mc => mc.BuildImmediate());

                            return removed;
                        });
                }

                // relation mutations
                foreach(var tr in elementTypeContainer.Trait.OptionalRelations)
                {
                    // set complete relations set
                    tet.FieldAsync(TraitEntityTypesNameGenerator.GenerateSetRelationsByCIIDMutationName(traitID, tr), elementTypeContainer.ElementWrapper,
                        arguments: new QueryArguments(
                            new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" },
                            new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "writeLayer" },
                            new QueryArgument<NonNullGraphType<GuidGraphType>> { Name = "baseCIID" },
                            new QueryArgument<NonNullGraphType<ListGraphType<GuidGraphType>>> { Name = "relatedCIIDs" }),
                        resolve: async context =>
                        {
                            var layerStrings = context.GetArgument<string[]>("layers")!;
                            var writeLayerID = context.GetArgument<string>("writeLayer")!;

                            var userContext = await context.SetupUserContext()
                                .WithTimeThreshold(TimeThreshold.BuildLatest(), context.Path)
                                .WithTransaction(modelContextBuilder => modelContextBuilder.BuildDeferred())
                                .WithLayersetAsync(async trans => await layerModel.BuildLayerSet(layerStrings, trans), context.Path);

                            var layerset = userContext.GetLayerSet(context.Path);
                            var timeThreshold = userContext.GetTimeThreshold(context.Path);
                            var trans = userContext.Transaction;

                            if (!layerBasedAuthorizationService.CanUserWriteToLayer(userContext.User, writeLayerID))
                                throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to write to the layerID: {writeLayerID}");
                            if (!layerBasedAuthorizationService.CanUserReadFromAllLayers(userContext.User, layerset))
                                throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to read from at least one of the following layerIDs: {string.Join(',', layerset)}");

                            var changeset = new ChangesetProxy(userContext.User.InDatabase, userContext.GetTimeThreshold(context.Path), changesetModel);

                            var baseCIID = context.GetArgument<Guid>("baseCIID")!;
                            var relatedCIIDs = context.GetArgument<Guid[]>("relatedCIIDs")!;

                            var t = await traitEntityModel.SetRelations(tr, baseCIID, relatedCIIDs, layerset, writeLayerID, new DataOriginV1(DataOriginType.Manual), changeset, trans, MaskHandlingForRemovalApplyNoMask.Instance);
                            userContext.CommitAndStartNewTransaction(mc => mc.BuildImmediate());
                            return t.et;
                        });

                    // add related CIs to relations set
                    tet.FieldAsync(TraitEntityTypesNameGenerator.GenerateAddRelationsByCIIDMutationName(traitID, tr), elementTypeContainer.ElementWrapper,
                        arguments: new QueryArguments(
                            new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" },
                            new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "writeLayer" },
                            new QueryArgument<NonNullGraphType<GuidGraphType>> { Name = "baseCIID" },
                            new QueryArgument<NonNullGraphType<ListGraphType<GuidGraphType>>> { Name = "relatedCIIDsToAdd" }),
                        resolve: async context =>
                        {
                            var layerStrings = context.GetArgument<string[]>("layers")!;
                            var writeLayerID = context.GetArgument<string>("writeLayer")!;

                            var userContext = await context.SetupUserContext()
                                .WithTimeThreshold(TimeThreshold.BuildLatest(), context.Path)
                                .WithTransaction(modelContextBuilder => modelContextBuilder.BuildDeferred())
                                .WithLayersetAsync(async trans => await layerModel.BuildLayerSet(layerStrings, trans), context.Path);

                            var layerset = userContext.GetLayerSet(context.Path);
                            var timeThreshold = userContext.GetTimeThreshold(context.Path);
                            var trans = userContext.Transaction;

                            if (!layerBasedAuthorizationService.CanUserWriteToLayer(userContext.User, writeLayerID))
                                throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to write to the layerID: {writeLayerID}");
                            if (!layerBasedAuthorizationService.CanUserReadFromAllLayers(userContext.User, layerset))
                                throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to read from at least one of the following layerIDs: {string.Join(',', layerset)}");

                            var changeset = new ChangesetProxy(userContext.User.InDatabase, userContext.GetTimeThreshold(context.Path), changesetModel);

                            var baseCIID = context.GetArgument<Guid>("baseCIID")!;
                            var relatedCIIDsToAdd = context.GetArgument<Guid[]>("relatedCIIDsToAdd")!;

                            var t = await traitEntityModel.AddRelations(tr, baseCIID, relatedCIIDsToAdd, layerset, writeLayerID, new DataOriginV1(DataOriginType.Manual), changeset, trans, MaskHandlingForRemovalApplyNoMask.Instance);
                            userContext.CommitAndStartNewTransaction(mc => mc.BuildImmediate());
                            return t.et;
                        });

                    // remove related CIs from relations set
                    tet.FieldAsync(TraitEntityTypesNameGenerator.GenerateRemoveRelationsByCIIDMutationName(traitID, tr), elementTypeContainer.ElementWrapper,
                        arguments: new QueryArguments(
                            new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" },
                            new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "writeLayer" },
                            new QueryArgument<NonNullGraphType<GuidGraphType>> { Name = "baseCIID" },
                            new QueryArgument<NonNullGraphType<ListGraphType<GuidGraphType>>> { Name = "relatedCIIDsToRemove" }),
                        resolve: async context =>
                        {
                            var layerStrings = context.GetArgument<string[]>("layers")!;
                            var writeLayerID = context.GetArgument<string>("writeLayer")!;

                            var userContext = await context.SetupUserContext()
                                .WithTimeThreshold(TimeThreshold.BuildLatest(), context.Path)
                                .WithTransaction(modelContextBuilder => modelContextBuilder.BuildDeferred())
                                .WithLayersetAsync(async trans => await layerModel.BuildLayerSet(layerStrings, trans), context.Path);

                            var layerset = userContext.GetLayerSet(context.Path);
                            var timeThreshold = userContext.GetTimeThreshold(context.Path);
                            var trans = userContext.Transaction;

                            if (!layerBasedAuthorizationService.CanUserWriteToLayer(userContext.User, writeLayerID))
                                throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to write to the layerID: {writeLayerID}");
                            if (!layerBasedAuthorizationService.CanUserReadFromAllLayers(userContext.User, layerset))
                                throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to read from at least one of the following layerIDs: {string.Join(',', layerset)}");

                            var changeset = new ChangesetProxy(userContext.User.InDatabase, userContext.GetTimeThreshold(context.Path), changesetModel);

                            var baseCIID = context.GetArgument<Guid>("baseCIID")!;
                            var relatedCIIDsToRemove = context.GetArgument<Guid[]>("relatedCIIDsToRemove")!;

                            var t = await traitEntityModel.RemoveRelations(tr, baseCIID, relatedCIIDsToRemove, layerset, writeLayerID, new DataOriginV1(DataOriginType.Manual), changeset, trans, MaskHandlingForRemovalApplyNoMask.Instance);
                            userContext.CommitAndStartNewTransaction(mc => mc.BuildImmediate());
                            return t.et;
                        });
                }
            }
        }
    }
}
