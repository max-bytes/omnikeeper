using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Types;
using Omnikeeper.Base.Authz;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.GraphQL;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.TraitBased;
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
        private readonly ChangesetDataModel changesetDataModel;
        private readonly IAuthzFilterManager authzFilterManager;
        private readonly ICIModel ciModel;
        private readonly ILayerModel layerModel;

        public TraitEntitiesMutationSchemaLoader(IAttributeModel attributeModel, IRelationModel relationModel, ICIIDModel ciidModel, 
            IDataLoaderService dataLoaderService, ChangesetDataModel changesetDataModel, IAuthzFilterManager authzFilterManager,
            ICIModel ciModel, ILayerModel layerModel)
        {
            this.attributeModel = attributeModel;
            this.relationModel = relationModel;
            this.ciidModel = ciidModel;
            this.dataLoaderService = dataLoaderService;
            this.changesetDataModel = changesetDataModel;
            this.authzFilterManager = authzFilterManager;
            this.ciModel = ciModel;
            this.layerModel = layerModel;
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
            var t = await traitEntityModel.InsertOrUpdate(finalCIID, attributeFragments, outgoingRelations, incomingRelations, relevantOutgoingPredicateIDs, relevantIncomingPredicateIDs, ciName, layerset, writeLayerID, changeset, trans, MaskHandlingForRemovalApplyNoMask.Instance);
            return t.et;
        }

        public void Init(GraphQLMutation tet, TypeContainer typeContainer)
        {
            foreach (var elementTypeContainer in typeContainer.ElementTypes)
            {
                var traitID = elementTypeContainer.Trait.ID;

                tet.Field(TraitEntityTypesNameGenerator.GenerateUpdateByCIIDMutationName(traitID), elementTypeContainer.ElementWrapper)
                    .Arguments(
                        new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" },
                        new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "writeLayer" },
                        new QueryArgument(new NonNullGraphType(elementTypeContainer.UpsertInput)) { Name = "input" },
                        new QueryArgument<NonNullGraphType<GuidGraphType>> { Name = "ciid" },
                        new QueryArgument<StringGraphType> { Name = "ciName" })
                    .ResolveAsync(async context =>
                    {
                        var layerStrings = context.GetArgument<string[]>("layers")!;
                        var writeLayerID = context.GetArgument<string>("writeLayer")!;
                        var ciid = context.GetArgument<Guid>("ciid");
                        var ciName = context.GetArgument<string?>("ciName", null);

                        var userContext = await context.GetUserContext()
                            .WithLayersetAsync(async trans => await layerModel.BuildLayerSet(layerStrings, trans), context.Path);

                        var layerset = userContext.GetLayerSet(context.Path);
                        var timeThreshold = userContext.GetTimeThreshold(context.Path);
                        var trans = userContext.Transaction;

                        var input = context.GetArgument<UpsertInput>("input");

                        // check if entity actually exists at that CI, error if not
                        var existingEntity = await elementTypeContainer.TraitEntityModel.GetSingleByCIID(ciid, layerset, trans, timeThreshold);
                        if (existingEntity == null)
                        {
                            throw new Exception($"Cannot update entity at CI with ID {ciid}: entity does not exist at that CI");
                        }

                        if (await authzFilterManager.ApplyPreFilterForMutation(new PreUpdateContextForTraitEntities(ciid, elementTypeContainer.Trait), writeLayerID, userContext, context.Path) is AuthzFilterResultDeny d)
                            throw new ExecutionError(d.Reason);

                        var et = await Upsert(ciid, input.AttributeValues, input.RelationValues, ciName, trans, userContext.ChangesetProxy, elementTypeContainer.TraitEntityModel, layerset, writeLayerID);

                        if (await authzFilterManager.ApplyPostFilterForMutation(new PostUpdateContextForTraitEntities(ciid, elementTypeContainer.Trait), writeLayerID, userContext, context.Path) is AuthzFilterResultDeny dPost)
                            throw new ExecutionError(dPost.Reason);

                        userContext.CommitAndStartNewTransactionIfLastMutationAndNoErrors(context, mc => mc.BuildImmediate());

                        return et;
                    });

                tet.Field(TraitEntityTypesNameGenerator.GenerateInsertNewMutationName(traitID), elementTypeContainer.ElementWrapper)
                    .Arguments(
                        new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" },
                        new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "writeLayer" },
                        new QueryArgument(new NonNullGraphType(elementTypeContainer.UpsertInput)) { Name = "input" },
                        new QueryArgument<StringGraphType> { Name = "ciName" })
                    .ResolveAsync(async context =>
                    {
                        var layerStrings = context.GetArgument<string[]>("layers")!;
                        var writeLayerID = context.GetArgument<string>("writeLayer")!;
                        var ciName = context.GetArgument<string?>("ciName", null);

                        var userContext = await context.GetUserContext()
                            .WithLayersetAsync(async trans => await layerModel.BuildLayerSet(layerStrings, trans), context.Path);

                        var layerset = userContext.GetLayerSet(context.Path);
                        var timeThreshold = userContext.GetTimeThreshold(context.Path);
                        var trans = userContext.Transaction;

                        if (await authzFilterManager.ApplyPreFilterForMutation(new PreInsertNewContextForTraitEntities(elementTypeContainer.Trait), writeLayerID, userContext, context.Path) is AuthzFilterResultDeny d)
                            throw new ExecutionError(d.Reason);

                        var insertInput = context.GetArgument<UpsertInput>("input");

                        // check if the trait entity has an ID, and if so, check that there is no existing entity with that ID
                        Guid finalCIID;
                        if (elementTypeContainer.IDInput != null)
                        {
                            var idAttributeTuples = insertInput.AttributeValues.Where(t => t.traitAttribute.AttributeTemplate.IsID.GetValueOrDefault(false)).Select(t => (t.traitAttribute.AttributeTemplate.Name, t.value)).ToArray();
                            var currentCIIDs = await TraitEntityHelper.GetMatchingCIIDsByAttributeValues(attributeModel, idAttributeTuples, layerset, trans, timeThreshold);

                            if (!currentCIIDs.IsEmpty())
                            { // there is already a CI with that ID attributes

                                // check if any of the found CIIDs actually fulfill the trait, only throw exception if so
                                // if not, keep going and use the found CIID for the new trait entity
                                var (bestMatchingCIID, bestMatchingET) = await TraitEntityHelper.GetSingleBestMatchingCI(currentCIIDs, elementTypeContainer.TraitEntityModel, layerset, trans, timeThreshold);

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

                        var et = await Upsert(finalCIID, insertInput.AttributeValues, insertInput.RelationValues, ciName, trans, userContext.ChangesetProxy, elementTypeContainer.TraitEntityModel, layerset, writeLayerID);

                        if (await authzFilterManager.ApplyPostFilterForMutation(new PostInsertNewContextForTraitEntities(finalCIID, elementTypeContainer.Trait), writeLayerID, userContext, context.Path) is AuthzFilterResultDeny dPost)
                            throw new ExecutionError(dPost.Reason);

                        userContext.CommitAndStartNewTransactionIfLastMutationAndNoErrors(context, mc => mc.BuildImmediate());

                        return et;
                    });

                var deleteByCIIDMutationName = TraitEntityTypesNameGenerator.GenerateDeleteByCIIDMutationName(traitID);
                tet.Field(deleteByCIIDMutationName, new BooleanGraphType())
                    .Arguments(
                        new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" },
                        new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "writeLayer" },
                        new QueryArgument<NonNullGraphType<GuidGraphType>> { Name = "ciid" }
                    )
                    .Description(@"Note on the return value: unlike deleteByDataID*, deleteByCIID* return true whether or not there was a trait entity at the
                        specified CIID or not. There is no check beforehand if the trait entity exists.")
                    .ResolveAsync(async context =>
                    {
                        var layerStrings = context.GetArgument<string[]>("layers")!;
                        var writeLayerID = context.GetArgument<string>("writeLayer")!;
                        var ciid = context.GetArgument<Guid>("ciid")!;

                        var userContext = await context.GetUserContext()
                            .WithLayersetAsync(async trans => await layerModel.BuildLayerSet(layerStrings, trans), context.Path);

                        var layerset = userContext.GetLayerSet(context.Path);
                        var timeThreshold = userContext.GetTimeThreshold(context.Path);
                        var trans = userContext.Transaction;

                        if (await authzFilterManager.ApplyPreFilterForMutation(new PreDeleteContextForTraitEntities(ciid, elementTypeContainer.Trait), writeLayerID, userContext, context.Path) is AuthzFilterResultDeny d)
                            throw new ExecutionError(d.Reason);

                        var removed = await elementTypeContainer.TraitEntityModel.TryToDelete(ciid, layerset, writeLayerID, userContext.ChangesetProxy, trans, MaskHandlingForRemovalApplyNoMask.Instance);

                        if (await authzFilterManager.ApplyPostFilterForMutation(new PostDeleteContextForTraitEntities(ciid, elementTypeContainer.Trait), writeLayerID, userContext, context.Path) is AuthzFilterResultDeny dPost)
                            throw new ExecutionError(dPost.Reason);

                        userContext.CommitAndStartNewTransactionIfLastMutationAndNoErrors(context, mc => mc.BuildImmediate());

                        return removed;
                    });

                if (elementTypeContainer.FilterInput != null)
                {
                    tet.Field(TraitEntityTypesNameGenerator.GenerateUpsertSingleByFilterMutationName(traitID), elementTypeContainer.ElementWrapper)
                        .Arguments(
                            new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" },
                            new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "writeLayer" },
                            new QueryArgument(new NonNullGraphType(elementTypeContainer.UpsertInput)) { Name = "input" },
                            new QueryArgument(elementTypeContainer.FilterInput) { Name = "filter" },
                            new QueryArgument<StringGraphType> { Name = "ciName" })
                        .ResolveAsync(async context =>
                        {
                            var layerStrings = context.GetArgument<string[]>("layers")!;
                            var writeLayerID = context.GetArgument<string>("writeLayer")!;
                            var ciName = context.GetArgument<string?>("ciName", null);
                            var filter = context.GetArgument<FilterInput>("filter");
                            var input = context.GetArgument<UpsertInput>("input");

                            var userContext = await context.GetUserContext()
                                .WithLayersetAsync(async trans => await layerModel.BuildLayerSet(layerStrings, trans), context.Path);

                            var layerset = userContext.GetLayerSet(context.Path);
                            var timeThreshold = userContext.GetTimeThreshold(context.Path);
                            var trans = userContext.Transaction;

                            var matchingCIIDs = filter.Apply(AllCIIDsSelection.Instance, attributeModel, relationModel, ciidModel, dataLoaderService, layerset, trans, timeThreshold);
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
                                    var (bestMatchingCIID, _) = await TraitEntityHelper.GetSingleBestMatchingCI(ciids, elementTypeContainer.TraitEntityModel, layerset, trans, timeThreshold);
                                    finalCIID = bestMatchingCIID;
                                }

                                if (await authzFilterManager.ApplyPreFilterForMutation(new PreUpsertContextForTraitEntities(finalCIID, elementTypeContainer.Trait), writeLayerID, userContext, context.Path) is AuthzFilterResultDeny d)
                                    throw new ExecutionError(d.Reason);

                                var et = await Upsert(finalCIID, input.AttributeValues, input.RelationValues, ciName, trans, userContext.ChangesetProxy, elementTypeContainer.TraitEntityModel, layerset, writeLayerID);

                                if (await authzFilterManager.ApplyPostFilterForMutation(new PostUpsertContextForTraitEntities(finalCIID, elementTypeContainer.Trait), writeLayerID, userContext, context.Path) is AuthzFilterResultDeny dPost)
                                    throw new ExecutionError(dPost.Reason);

                                userContext.CommitAndStartNewTransactionIfLastMutationAndNoErrors(context, mc => mc.BuildImmediate());

                                return et;
                            });
                        });


                    tet.Field(TraitEntityTypesNameGenerator.GenerateDeleteSingleByFilterMutationName(traitID), new BooleanGraphType())
                        .Arguments(
                            new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" },
                            new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "writeLayer" },
                            new QueryArgument(elementTypeContainer.FilterInput) { Name = "filter" }
                        )
                        .Description(@"Note on the return value: only returns true if the trait entity was present 
                                (and found through the filter) first, and it is not present anymore after the deletion at that CIID.")
                        .ResolveAsync(async context =>
                            {
                                var layerStrings = context.GetArgument<string[]>("layers")!;
                                var writeLayerID = context.GetArgument<string>("writeLayer")!;
                                var filter = context.GetArgument<FilterInput>("filter");

                                var userContext = await context.GetUserContext()
                                    .WithLayersetAsync(async trans => await layerModel.BuildLayerSet(layerStrings, trans), context.Path);

                                var layerset = userContext.GetLayerSet(context.Path);
                                var timeThreshold = userContext.GetTimeThreshold(context.Path);
                                var trans = userContext.Transaction;

                                var matchingCIIDs = filter.Apply(AllCIIDsSelection.Instance, attributeModel, relationModel, ciidModel, dataLoaderService, layerset, trans, timeThreshold);
                                return matchingCIIDs.Then(async matchingCIIDs =>
                                {
                                    var ciids = await matchingCIIDs.GetCIIDsAsync(async () => await ciidModel.GetCIIDs(trans));
                                    if (ciids.IsEmpty())
                                        return false;

                                    // if the ciids contains any CIs that have the trait, we need to use this, not just the first CIID (which might NOT have the trait)
                                    // otherwise, return
                                    var (bestMatchingCIID, bestMatchingET) = await TraitEntityHelper.GetSingleBestMatchingCI(ciids, elementTypeContainer.TraitEntityModel, layerset, trans, timeThreshold);
                                    if (bestMatchingET == null)
                                        return false;

                                    if (await authzFilterManager.ApplyPreFilterForMutation(new PreDeleteContextForTraitEntities(bestMatchingCIID, elementTypeContainer.Trait), writeLayerID, userContext, context.Path) is AuthzFilterResultDeny d)
                                        throw new ExecutionError(d.Reason);

                                    var removed = await elementTypeContainer.TraitEntityModel.TryToDelete(bestMatchingCIID, layerset, writeLayerID, userContext.ChangesetProxy, trans, MaskHandlingForRemovalApplyNoMask.Instance);

                                    if (await authzFilterManager.ApplyPostFilterForMutation(new PostDeleteContextForTraitEntities(bestMatchingCIID, elementTypeContainer.Trait), writeLayerID, userContext, context.Path) is AuthzFilterResultDeny dPost)
                                        throw new ExecutionError(dPost.Reason);

                                    userContext.CommitAndStartNewTransactionIfLastMutationAndNoErrors(context, mc => mc.BuildImmediate());

                                    return removed;
                                });
                            });
                }

                if (elementTypeContainer.IDInput != null) // only add *byDataID-mutations for trait entities that have an ID
                {
                    var upsertByDataIDMutationName = TraitEntityTypesNameGenerator.GenerateUpsertByDataIDMutationName(traitID);
                    tet.Field(upsertByDataIDMutationName, elementTypeContainer.ElementWrapper)
                        .Arguments(
                            new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" },
                            new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "writeLayer" },
                            new QueryArgument(new NonNullGraphType(elementTypeContainer.UpsertInput)) { Name = "input" },
                            new QueryArgument<StringGraphType> { Name = "ciName" })
                        .ResolveAsync(async context =>
                        {
                            var layerStrings = context.GetArgument<string[]>("layers")!;
                            var writeLayerID = context.GetArgument<string>("writeLayer")!;
                            var ciName = context.GetArgument<string?>("ciName", null);

                            var userContext = await context.GetUserContext()
                                .WithLayersetAsync(async trans => await layerModel.BuildLayerSet(layerStrings, trans), context.Path);

                            var layerset = userContext.GetLayerSet(context.Path);
                            var timeThreshold = userContext.GetTimeThreshold(context.Path);
                            var trans = userContext.Transaction;

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
                                var (bestMatchingCIID, _) = await TraitEntityHelper.GetSingleBestMatchingCI(currentCIIDs, elementTypeContainer.TraitEntityModel, layerset, trans, timeThreshold);
                                finalCIID = bestMatchingCIID;
                            }

                            if (await authzFilterManager.ApplyPreFilterForMutation(new PreUpsertContextForTraitEntities(finalCIID, elementTypeContainer.Trait), writeLayerID, userContext, context.Path) is AuthzFilterResultDeny d)
                                throw new ExecutionError(d.Reason);

                            var et = await Upsert(finalCIID, input.AttributeValues, input.RelationValues, ciName, trans, userContext.ChangesetProxy, elementTypeContainer.TraitEntityModel, layerset, writeLayerID);

                            if (await authzFilterManager.ApplyPostFilterForMutation(new PostUpsertContextForTraitEntities(finalCIID, elementTypeContainer.Trait), writeLayerID, userContext, context.Path) is AuthzFilterResultDeny dPost)
                                throw new ExecutionError(dPost.Reason);

                            userContext.CommitAndStartNewTransactionIfLastMutationAndNoErrors(context, mc => mc.BuildImmediate());
                            return et;
                        })
                        .DeprecationReason($"Superseded by {TraitEntityTypesNameGenerator.GenerateUpsertSingleByFilterMutationName(traitID)}");

                    var deleteByDataIDMutationName = TraitEntityTypesNameGenerator.GenerateDeleteByDataIDMutationName(traitID);
                    tet.Field(deleteByDataIDMutationName, new BooleanGraphType())
                        .Arguments(
                            new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" },
                            new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "writeLayer" },
                            new QueryArgument(new NonNullGraphType(elementTypeContainer.IDInput)) { Name = "id" }
                        )
                        .Description(@"Note on the return value: only returns true if the trait entity was present 
                            (and found through its ID) first, and it is not present anymore after the deletion at that CIID.")
                        .ResolveAsync(async context =>
                        {
                            var layerStrings = context.GetArgument<string[]>("layers")!;
                            var writeLayerID = context.GetArgument<string>("writeLayer")!;

                            var userContext = await context.GetUserContext()
                                .WithLayersetAsync(async trans => await layerModel.BuildLayerSet(layerStrings, trans), context.Path);

                            var layerset = userContext.GetLayerSet(context.Path);
                            var timeThreshold = userContext.GetTimeThreshold(context.Path);
                            var trans = userContext.Transaction;

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
                                var (bestMatchingCIID, bestMatchingET) = await TraitEntityHelper.GetSingleBestMatchingCI(foundCIIDs, elementTypeContainer.TraitEntityModel, layerset, trans, timeThreshold);

                                if (bestMatchingET == null)
                                    return false;
                                else
                                    finalCIID = bestMatchingCIID;
                            }

                            if (await authzFilterManager.ApplyPreFilterForMutation(new PreDeleteContextForTraitEntities(finalCIID, elementTypeContainer.Trait), writeLayerID, userContext, context.Path) is AuthzFilterResultDeny d)
                                throw new ExecutionError(d.Reason);

                            var removed = await elementTypeContainer.TraitEntityModel.TryToDelete(finalCIID, layerset, writeLayerID, userContext.ChangesetProxy, trans, MaskHandlingForRemovalApplyNoMask.Instance);

                            if (await authzFilterManager.ApplyPostFilterForMutation(new PostDeleteContextForTraitEntities(finalCIID, elementTypeContainer.Trait), writeLayerID, userContext, context.Path) is AuthzFilterResultDeny dPost)
                                throw new ExecutionError(dPost.Reason);

                            userContext.CommitAndStartNewTransactionIfLastMutationAndNoErrors(context, mc => mc.BuildImmediate());

                            return removed;
                        })
                        .DeprecationReason($"Superseded by {TraitEntityTypesNameGenerator.GenerateDeleteSingleByFilterMutationName(traitID)}");
                }

                // changeset data insert
                tet.Field(TraitEntityTypesNameGenerator.GenerateInsertChangesetDataAsTraitEntityMutationName(traitID), elementTypeContainer.ElementWrapper)
                    .Arguments(
                        new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "layer" },
                        new QueryArgument(new NonNullGraphType(elementTypeContainer.UpsertInput)) { Name = "input" })
                    .ResolveAsync(async context =>
                    {
                        var writeLayerID = context.GetArgument<string>("layer")!;
                        var userContext = await context.GetUserContext()
                            .WithLayersetAsync(async trans => await layerModel.BuildLayerSet(new string[] { writeLayerID }, trans), context.Path);

                        var layerset = userContext.GetLayerSet(context.Path);
                        var timeThreshold = userContext.GetTimeThreshold(context.Path);
                        var trans = userContext.Transaction;
                        var changesetProxy = userContext.ChangesetProxy;

                        // TODO: mask handling
                        var maskHandling = MaskHandlingForRemovalApplyNoMask.Instance;

                        if (await authzFilterManager.ApplyPreFilterForMutation(new PreInsertChangesetDataContextForTraitEntities(elementTypeContainer.Trait), writeLayerID, userContext, context.Path) is AuthzFilterResultDeny d)
                            throw new ExecutionError(d.Reason);

                        if (elementTypeContainer.IDInput != null)
                            throw new Exception("Inserting trait entity that has ID as changeset data not supported");

                        var insertInput = context.GetArgument<UpsertInput>("input");

                        var correspondingChangeset = await changesetProxy.GetChangeset(writeLayerID, userContext.Transaction);

                        // insert changeset-data first
                        var (dc, changed, ciid) = await changesetDataModel.InsertOrUpdate(new ChangesetData(correspondingChangeset.ID.ToString()), new LayerSet(writeLayerID), writeLayerID, changesetProxy, userContext.Transaction, maskHandling);

                        // insert trait entity afterwards, no CI-name
                        var et = await Upsert(ciid, insertInput.AttributeValues, insertInput.RelationValues, null, trans, changesetProxy, elementTypeContainer.TraitEntityModel, layerset, writeLayerID);

                        if (await authzFilterManager.ApplyPostFilterForMutation(new PostInsertChangesetDataContextForTraitEntities(elementTypeContainer.Trait), writeLayerID, userContext, context.Path) is AuthzFilterResultDeny dPost)
                            throw new ExecutionError(dPost.Reason);

                        userContext.CommitAndStartNewTransactionIfLastMutationAndNoErrors(context, mc => mc.BuildImmediate());

                        return et;
                    });

                // relation mutations
                foreach (var tr in elementTypeContainer.Trait.OptionalRelations)
                {
                    // set complete relations set
                    tet.Field(TraitEntityTypesNameGenerator.GenerateSetRelationsByCIIDMutationName(traitID, tr), elementTypeContainer.ElementWrapper)
                        .Arguments(
                            new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" },
                            new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "writeLayer" },
                            new QueryArgument<NonNullGraphType<GuidGraphType>> { Name = "baseCIID" },
                            new QueryArgument<NonNullGraphType<ListGraphType<GuidGraphType>>> { Name = "relatedCIIDs" })
                        .ResolveAsync(async context =>
                        {
                            var layerStrings = context.GetArgument<string[]>("layers")!;
                            var writeLayerID = context.GetArgument<string>("writeLayer")!;

                            var userContext = await context.GetUserContext()
                                .WithLayersetAsync(async trans => await layerModel.BuildLayerSet(layerStrings, trans), context.Path);

                            var layerset = userContext.GetLayerSet(context.Path);
                            var timeThreshold = userContext.GetTimeThreshold(context.Path);
                            var trans = userContext.Transaction;

                            var baseCIID = context.GetArgument<Guid>("baseCIID")!;
                            var relatedCIIDs = context.GetArgument<Guid[]>("relatedCIIDs")!;

                            if (await authzFilterManager.ApplyPreFilterForMutation(new PreSetRelationsContextForTraitEntities(baseCIID, elementTypeContainer.Trait), writeLayerID, userContext, context.Path) is AuthzFilterResultDeny d)
                                throw new ExecutionError(d.Reason);

                            var t = await elementTypeContainer.TraitEntityModel.SetRelations(tr, baseCIID, relatedCIIDs, layerset, writeLayerID, userContext.ChangesetProxy, trans, MaskHandlingForRemovalApplyNoMask.Instance);

                            if (await authzFilterManager.ApplyPostFilterForMutation(new PostSetRelationsContextForTraitEntities(baseCIID, elementTypeContainer.Trait), writeLayerID, userContext, context.Path) is AuthzFilterResultDeny dPost)
                                throw new ExecutionError(dPost.Reason);

                            userContext.CommitAndStartNewTransactionIfLastMutationAndNoErrors(context, mc => mc.BuildImmediate());
                            return t.et;
                        });

                    // add related CIs to relations set
                    tet.Field(TraitEntityTypesNameGenerator.GenerateAddRelationsByCIIDMutationName(traitID, tr), elementTypeContainer.ElementWrapper)
                        .Arguments(
                            new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" },
                            new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "writeLayer" },
                            new QueryArgument<NonNullGraphType<GuidGraphType>> { Name = "baseCIID" },
                            new QueryArgument<NonNullGraphType<ListGraphType<GuidGraphType>>> { Name = "relatedCIIDsToAdd" })
                        .ResolveAsync(async context =>
                        {
                            var layerStrings = context.GetArgument<string[]>("layers")!;
                            var writeLayerID = context.GetArgument<string>("writeLayer")!;

                            var userContext = await context.GetUserContext()
                                .WithLayersetAsync(async trans => await layerModel.BuildLayerSet(layerStrings, trans), context.Path);

                            var layerset = userContext.GetLayerSet(context.Path);
                            var timeThreshold = userContext.GetTimeThreshold(context.Path);
                            var trans = userContext.Transaction;

                            var baseCIID = context.GetArgument<Guid>("baseCIID")!;
                            var relatedCIIDsToAdd = context.GetArgument<Guid[]>("relatedCIIDsToAdd")!;

                            if (await authzFilterManager.ApplyPreFilterForMutation(new PreAddRelationsContextForTraitEntities(baseCIID, elementTypeContainer.Trait), writeLayerID, userContext, context.Path) is AuthzFilterResultDeny d)
                                throw new ExecutionError(d.Reason);

                            var t = await elementTypeContainer.TraitEntityModel.AddRelations(tr, baseCIID, relatedCIIDsToAdd, layerset, writeLayerID, userContext.ChangesetProxy, trans, MaskHandlingForRemovalApplyNoMask.Instance);

                            if (await authzFilterManager.ApplyPostFilterForMutation(new PostAddRelationsContextForTraitEntities(baseCIID, elementTypeContainer.Trait), writeLayerID, userContext, context.Path) is AuthzFilterResultDeny dPost)
                                throw new ExecutionError(dPost.Reason);

                            userContext.CommitAndStartNewTransactionIfLastMutationAndNoErrors(context, mc => mc.BuildImmediate());
                            return t.et;
                        });

                    // remove related CIs from relations set
                    tet.Field(TraitEntityTypesNameGenerator.GenerateRemoveRelationsByCIIDMutationName(traitID, tr), elementTypeContainer.ElementWrapper)
                        .Arguments(
                            new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" },
                            new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "writeLayer" },
                            new QueryArgument<NonNullGraphType<GuidGraphType>> { Name = "baseCIID" },
                            new QueryArgument<NonNullGraphType<ListGraphType<GuidGraphType>>> { Name = "relatedCIIDsToRemove" })
                        .ResolveAsync(async context =>
                        {
                            var layerStrings = context.GetArgument<string[]>("layers")!;
                            var writeLayerID = context.GetArgument<string>("writeLayer")!;

                            var userContext = await context.GetUserContext()
                                .WithLayersetAsync(async trans => await layerModel.BuildLayerSet(layerStrings, trans), context.Path);

                            var layerset = userContext.GetLayerSet(context.Path);
                            var timeThreshold = userContext.GetTimeThreshold(context.Path);
                            var trans = userContext.Transaction;

                            var baseCIID = context.GetArgument<Guid>("baseCIID")!;
                            var relatedCIIDsToRemove = context.GetArgument<Guid[]>("relatedCIIDsToRemove")!;

                            if (await authzFilterManager.ApplyPreFilterForMutation(new PreRemoveRelationsContextForTraitEntities(baseCIID, elementTypeContainer.Trait), writeLayerID, userContext, context.Path) is AuthzFilterResultDeny d)
                                throw new ExecutionError(d.Reason);

                            var t = await elementTypeContainer.TraitEntityModel.RemoveRelations(tr, baseCIID, relatedCIIDsToRemove, layerset, writeLayerID, userContext.ChangesetProxy, trans, MaskHandlingForRemovalApplyNoMask.Instance);

                            if (await authzFilterManager.ApplyPostFilterForMutation(new PostRemoveRelationsContextForTraitEntities(baseCIID, elementTypeContainer.Trait), writeLayerID, userContext, context.Path) is AuthzFilterResultDeny dPost)
                                throw new ExecutionError(dPost.Reason);

                            userContext.CommitAndStartNewTransactionIfLastMutationAndNoErrors(context, mc => mc.BuildImmediate());
                            return t.et;
                        });
                }
            }
        }
    }
}
