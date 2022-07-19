using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Types;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.GraphQL;
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
        private readonly ICIIDModel ciidModel;
        private readonly IDataLoaderService dataLoaderService;
        private readonly IEffectiveTraitModel effectiveTraitModel;
        private readonly ICIModel ciModel;
        private readonly ILayerModel layerModel;
        private readonly IChangesetModel changesetModel;
        private readonly ILayerBasedAuthorizationService layerBasedAuthorizationService;

        public TraitEntitiesMutationSchemaLoader(IAttributeModel attributeModel, IRelationModel relationModel, ICIIDModel ciidModel, 
            IDataLoaderService dataLoaderService,
            IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, ILayerModel layerModel, IChangesetModel changesetModel, ILayerBasedAuthorizationService layerBasedAuthorizationService)
        {
            this.attributeModel = attributeModel;
            this.relationModel = relationModel;
            this.ciidModel = ciidModel;
            this.dataLoaderService = dataLoaderService;
            this.effectiveTraitModel = effectiveTraitModel;
            this.ciModel = ciModel;
            this.layerModel = layerModel;
            this.changesetModel = changesetModel;
            this.layerBasedAuthorizationService = layerBasedAuthorizationService;
        }

        // NOTE: expects the CI to exist already
        private async Task<EffectiveTrait> Upsert(Guid finalCIID,
            (TraitAttribute attribute, IAttributeValue value)[] attributeValues, 
            (TraitRelation traitRelation, Guid[] relatedCIIDs)[] relationValues,
            string? ciName, IModelContext trans, IChangesetProxy changeset, TraitEntityModel traitEntityModel, LayerSet layerset, string writeLayerID)
        {
            var attributeFragments = attributeValues.Select(i => new BulkCIAttributeDataCIAndAttributeNameScope.Fragment(finalCIID, i.attribute.AttributeTemplate.Name, i.value));
            var incomingRelationValues = relationValues.Where(rv => !rv.traitRelation.RelationTemplate.DirectionForward).ToList();
            var outgoingRelationValues = relationValues.Where(rv => rv.traitRelation.RelationTemplate.DirectionForward).ToList();
            IList<(Guid thisCIID, string predicateID, Guid[] otherCIIDs)> incomingRelations = incomingRelationValues.Select(rv => (finalCIID, rv.traitRelation.RelationTemplate.PredicateID, rv.relatedCIIDs)).ToList();
            IList<(Guid thisCIID, string predicateID, Guid[] otherCIIDs)> outgoingRelations = outgoingRelationValues.Select(rv => (finalCIID, rv.traitRelation.RelationTemplate.PredicateID, rv.relatedCIIDs)).ToList();
            ISet<string>? relevantIncomingPredicateIDs = incomingRelationValues.Select(rv => rv.traitRelation.RelationTemplate.PredicateID).ToHashSet();
            ISet<string>? relevantOutgoingPredicateIDs = outgoingRelationValues.Select(rv => rv.traitRelation.RelationTemplate.PredicateID).ToHashSet();
            var t = await traitEntityModel.InsertOrUpdate(finalCIID, attributeFragments, outgoingRelations, incomingRelations, relevantOutgoingPredicateIDs, relevantIncomingPredicateIDs, ciName, layerset, writeLayerID, new DataOriginV1(DataOriginType.Manual), changeset, trans, MaskHandlingForRemovalApplyNoMask.Instance);
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

                        var et = await Upsert(ciid, input.AttributeValues, input.RelationValues, ciName, trans, changeset, traitEntityModel, layerset, writeLayerID);

                        userContext.CommitAndStartNewTransaction(mc => mc.BuildImmediate());

                        return et;
                    });

                tet.FieldAsync(TraitEntityTypesNameGenerator.GenerateInsertNewMutationName(traitID), elementTypeContainer.ElementWrapper,
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

                        var insertInput = context.GetArgument<UpsertInput>("input");

                        // check if the trait entity has an ID, and if so, check that there is no existing entity with that ID
                        Guid finalCIID;
                        if (elementTypeContainer.IDInputType != null)
                        {
                            var idAttributeTuples = insertInput.AttributeValues.Where(t => t.traitAttribute.AttributeTemplate.IsID.GetValueOrDefault(false)).Select(t => (t.traitAttribute.AttributeTemplate.Name, t.value)).ToArray();
                            var currentCIIDs = await TraitEntityHelper.GetMatchingCIIDsByAttributeValues(attributeModel, idAttributeTuples, layerset, trans, timeThreshold);

                            if (!currentCIIDs.IsEmpty())
                            { // there is already a CI with that ID attributes

                                // check if any of the found CIIDs actually fulfill the trait, only throw exception if so
                                // if not, keep going and use the found CIID for the new trait entity
                                var (bestMatchingCIID, bestMatchingET) = await TraitEntityHelper.GetSingleBestMatchingCI(currentCIIDs, traitEntityModel, layerset, trans, timeThreshold);

                                if (bestMatchingET != null)
                                    throw new Exception($"A trait entity with that data ID already exists; CIID: {bestMatchingCIID})");
                                else
                                    finalCIID = bestMatchingCIID;
                            } else
                            {
                                finalCIID = await ciModel.CreateCI(trans);
                            }
                        } else
                        {
                            finalCIID = await ciModel.CreateCI(trans);
                        }

                        var changeset = new ChangesetProxy(userContext.User.InDatabase, userContext.GetTimeThreshold(context.Path), changesetModel);

                        var et = await Upsert(finalCIID, insertInput.AttributeValues, insertInput.RelationValues, ciName, trans, changeset, traitEntityModel, layerset, writeLayerID);

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

                if (elementTypeContainer.FilterInputType != null)
                {
                    tet.FieldAsync(TraitEntityTypesNameGenerator.GenerateUpsertSingleByFilterMutationName(traitID), elementTypeContainer.ElementWrapper,
                        arguments: new QueryArguments(
                            new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" },
                            new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "writeLayer" },
                            new QueryArgument(new NonNullGraphType(elementTypeContainer.UpsertInputType)) { Name = "input" },
                            new QueryArgument(elementTypeContainer.FilterInputType) { Name = "filter" },
                            new QueryArgument<StringGraphType> { Name = "ciName" }),
                        resolve: async context =>
                        {
                            var layerStrings = context.GetArgument<string[]>("layers")!;
                            var writeLayerID = context.GetArgument<string>("writeLayer")!;
                            var ciName = context.GetArgument<string?>("ciName", null);
                            var filter = context.GetArgument<FilterInput>("filter");
                            var input = context.GetArgument<UpsertInput>("input");

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

                            var matchingCIIDs = filter.Apply(attributeModel, relationModel, ciidModel, dataLoaderService, layerset, trans, timeThreshold);
                            return matchingCIIDs.Then(async matchingCIIDs =>
                            {
                                var ciids = await matchingCIIDs.GetCIIDsAsync(async () => await ciidModel.GetCIIDs(trans));
                                Guid finalCIID;
                                if (ciids.IsEmpty())
                                    finalCIID = await ciModel.CreateCI(trans);
                                else
                                {
                                    // if the matchingCIIDs contains any CIs that have the trait, we need to use this preferably, not just the first CIID (which might NOT have the trait)
                                    // only if there are no CIs that fulfill the trait, we can use the first one ordered by CIID only
                                    var (bestMatchingCIID, _) = await TraitEntityHelper.GetSingleBestMatchingCI(ciids, traitEntityModel, layerset, trans, timeThreshold);
                                    finalCIID = bestMatchingCIID;
                                }

                                var changeset = new ChangesetProxy(userContext.User.InDatabase, userContext.GetTimeThreshold(context.Path), changesetModel);

                                var et = await Upsert(finalCIID, input.AttributeValues, input.RelationValues, ciName, trans, changeset, traitEntityModel, layerset, writeLayerID);

                                userContext.CommitAndStartNewTransaction(mc => mc.BuildImmediate());

                                return et;
                            });
                        });


                    tet.FieldAsync(TraitEntityTypesNameGenerator.GenerateDeleteSingleByFilterMutationName(traitID), new BooleanGraphType(),
                        arguments: new QueryArguments(
                            new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" },
                            new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "writeLayer" },
                            new QueryArgument(elementTypeContainer.FilterInputType) { Name = "filter" }
                        ),
                        description: @"Note on the return value: only returns true if the trait entity was present 
                                (and found through the filter) first, and it is not present anymore after the deletion at that CIID.",
                            resolve: async context =>
                            {
                                var layerStrings = context.GetArgument<string[]>("layers")!;
                                var writeLayerID = context.GetArgument<string>("writeLayer")!;
                                var filter = context.GetArgument<FilterInput>("filter");

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

                                var matchingCIIDs = filter.Apply(attributeModel, relationModel, ciidModel, dataLoaderService, layerset, trans, timeThreshold);
                                return matchingCIIDs.Then(async matchingCIIDs =>
                                {
                                    var ciids = await matchingCIIDs.GetCIIDsAsync(async () => await ciidModel.GetCIIDs(trans));
                                    if (ciids.IsEmpty())
                                        return false;

                                    // if the ciids contains any CIs that have the trait, we need to use this, not just the first CIID (which might NOT have the trait)
                                    // otherwise, return
                                    var (bestMatchingCIID, bestMatchingET) = await TraitEntityHelper.GetSingleBestMatchingCI(ciids, traitEntityModel, layerset, trans, timeThreshold);
                                    if (bestMatchingET == null)
                                        return false;

                                    var changeset = new ChangesetProxy(userContext.User.InDatabase, userContext.GetTimeThreshold(context.Path), changesetModel);
                                    var removed = await traitEntityModel.TryToDelete(bestMatchingCIID, layerset, writeLayerID, new DataOriginV1(DataOriginType.Manual), changeset, trans, MaskHandlingForRemovalApplyNoMask.Instance);

                                    userContext.CommitAndStartNewTransaction(mc => mc.BuildImmediate());

                                    return removed;
                                });
                            });
                }

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

                            var idAttributeTuples = input.AttributeValues.Where(t => t.traitAttribute.AttributeTemplate.IsID.GetValueOrDefault(false)).Select(t => (t.traitAttribute.AttributeTemplate.Name, t.value)).ToArray();
                            if (idAttributeTuples.IsEmpty())
                            {
                                throw new Exception("Cannot mutate trait entity that does not have proper ID field(s)");
                            }

                            var currentCIIDs = await TraitEntityHelper.GetMatchingCIIDsByAttributeValues(attributeModel, idAttributeTuples, layerset, trans, timeThreshold);

                            // if the currentCIIDs contains any CIs that have the trait, we need to use this preferably, not just the first CIID (which might NOT have the trait)
                            // only if there are no CIs that fulfill the trait, we can use the first one ordered by CIID only
                            Guid finalCIID;
                            if (currentCIIDs.IsEmpty())
                                finalCIID = await ciModel.CreateCI(trans);
                            else
                            {
                                var (bestMatchingCIID, _) = await TraitEntityHelper.GetSingleBestMatchingCI(currentCIIDs, traitEntityModel, layerset, trans, timeThreshold);
                                finalCIID = bestMatchingCIID;
                            }

                            var changeset = new ChangesetProxy(userContext.User.InDatabase, userContext.GetTimeThreshold(context.Path), changesetModel);

                            var et = await Upsert(finalCIID, input.AttributeValues, input.RelationValues, ciName, trans, changeset, traitEntityModel, layerset, writeLayerID);
                            userContext.CommitAndStartNewTransaction(mc => mc.BuildImmediate());
                            return et;
                        });

                    var deleteByDataIDMutationName = TraitEntityTypesNameGenerator.GenerateDeleteByDataIDMutationName(traitID);
                    tet.FieldAsync(deleteByDataIDMutationName, new BooleanGraphType(),
                        arguments: new QueryArguments(
                            new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" },
                            new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "writeLayer" },
                            new QueryArgument(new NonNullGraphType(elementTypeContainer.IDInputType)) { Name = "id" }
                        ),
                        description: @"Note on the return value: only returns true if the trait entity was present 
                            (and found through its ID) first, and it is not present anymore after the deletion at that CIID.",
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
                            var idAttributeTuples = id.IDAttributeValues.Select(t => (t.traitAttribute.AttributeTemplate.Name, t.value)).ToArray();
                            var foundCIIDs = await TraitEntityHelper.GetMatchingCIIDsByAttributeValues(attributeModel, idAttributeTuples, layerset, trans, timeThreshold);
                                
                            Guid finalCIID;
                            if (foundCIIDs.IsEmpty())
                                return false;
                            else
                            {
                                // if the foundCIIDs contains any CIs that have the trait, we need to use this, not just the first CIID (which might NOT have the trait)
                                // if there are no CIs that fulfill the trait, we return as there is nothing to delete
                                var (bestMatchingCIID, bestMatchingET) = await TraitEntityHelper.GetSingleBestMatchingCI(foundCIIDs, traitEntityModel, layerset, trans, timeThreshold);

                                if (bestMatchingET == null)
                                    return false;
                                else
                                    finalCIID = bestMatchingCIID;
                            }

                            var changeset = new ChangesetProxy(userContext.User.InDatabase, userContext.GetTimeThreshold(context.Path), changesetModel);
                            var removed = await traitEntityModel.TryToDelete(finalCIID, layerset, writeLayerID, new DataOriginV1(DataOriginType.Manual), changeset, trans, MaskHandlingForRemovalApplyNoMask.Instance);

                            userContext.CommitAndStartNewTransaction(mc => mc.BuildImmediate());

                            return removed;
                        });
                }

                // relation mutations
                foreach (var tr in elementTypeContainer.Trait.OptionalRelations)
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
