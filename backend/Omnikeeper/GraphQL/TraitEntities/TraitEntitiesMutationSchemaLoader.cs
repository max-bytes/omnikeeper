using GraphQL;
using GraphQL.Types;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
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
        private readonly GraphQLMutation tet;
        private readonly IAttributeModel attributeModel;
        private readonly IRelationModel relationModel;
        private readonly IEffectiveTraitModel effectiveTraitModel;
        private readonly ICIModel ciModel;
        private readonly ILayerModel layerModel;
        private readonly IChangesetModel changesetModel;

        public TraitEntitiesMutationSchemaLoader(GraphQLMutation tet, IAttributeModel attributeModel, IRelationModel relationModel,
            IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, ILayerModel layerModel, IChangesetModel changesetModel)
        {
            this.tet = tet;
            this.attributeModel = attributeModel;
            this.relationModel = relationModel;
            this.effectiveTraitModel = effectiveTraitModel;
            this.ciModel = ciModel;
            this.layerModel = layerModel;
            this.changesetModel = changesetModel;
        }

        private async Task<EffectiveTrait> Upsert(Guid? ciid, (string name, IAttributeValue value, bool isID)[] attributeValues, (string predicateID, bool forward, Guid[] relatedCIIDs)[] relationValues, IModelContext trans, IChangesetProxy changeset, TraitEntityModel traitEntityModel, LayerSet layerset, string writeLayerID)
        {
            var finalCIID = (ciid.HasValue) ? ciid.Value : await ciModel.CreateCI(trans);

            var attributeFragments = attributeValues.Select(i => new BulkCIAttributeDataCIAndAttributeNameScope.Fragment(finalCIID, i.name, i.value));

            IList<(Guid thisCIID, string predicateID, Guid[] otherCIIDs)> incomingRelations = relationValues.Where(rv => !rv.forward).Select(rv => (finalCIID, rv.predicateID, rv.relatedCIIDs)).ToList();
            IList<(Guid thisCIID, string predicateID, Guid[] otherCIIDs)> outgoingRelations = relationValues.Where(rv => rv.forward).Select(rv => (finalCIID, rv.predicateID, rv.relatedCIIDs)).ToList();
            var t = await traitEntityModel.InsertOrUpdate(finalCIID, attributeFragments, outgoingRelations, incomingRelations, layerset, writeLayerID, new DataOriginV1(DataOriginType.Manual), changeset, trans);
            return t.et;
        }

        public void Init(TypeContainer typeContainer)
        {
            foreach (var elementTypeContainer in typeContainer.ElementTypes)
            {
                var traitID = elementTypeContainer.Trait.ID;

                var traitEntityModel = new TraitEntityModel(elementTypeContainer.Trait, effectiveTraitModel, ciModel, attributeModel, relationModel);

                var upsertByCIIDMutationName = TraitEntityTypesNameGenerator.GenerateUpsertByCIIDMutationName(traitID);
                tet.FieldAsync(upsertByCIIDMutationName, elementTypeContainer.ElementWrapper,
                    arguments: new QueryArguments(
                        new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" },
                        new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "writeLayer" },
                        new QueryArgument(new NonNullGraphType(elementTypeContainer.UpsertInputType)) { Name = "input" },
                        new QueryArgument(new GuidGraphType()) { Name = "ciid" }),
                    resolve: async context =>
                    {
                        var layerStrings = context.GetArgument<string[]>("layers")!;
                        var writeLayerID = context.GetArgument<string>("writeLayer")!;
                        var ciid = context.GetArgument<Guid?>("ciid");

                        var userContext = await context.SetupUserContext()
                            .WithTimeThreshold(TimeThreshold.BuildLatest(), context.Path)
                            .WithTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate())
                            .WithLayersetAsync(async trans => await layerModel.BuildLayerSet(layerStrings, trans), context.Path);

                        var layerset = userContext.GetLayerSet(context.Path);
                        var timeThreshold = userContext.GetTimeThreshold(context.Path);
                        var trans = userContext.Transaction;

                        if (context.GetArgument(typeof(object), "input") is not IDictionary<string, object> upsertInputCollection)
                            throw new Exception("Invalid input object for upsert detected");

                        var (inputAttributeValues, inputRelationValues) = TraitEntityHelper.InputDictionary2AttributeAndRelationTuples(upsertInputCollection, elementTypeContainer.Trait);

                        var changeset = new ChangesetProxy(userContext.User.InDatabase, userContext.GetTimeThreshold(context.Path), changesetModel);

                        var et = await Upsert(ciid, inputAttributeValues, inputRelationValues, trans, changeset, traitEntityModel, layerset, writeLayerID);

                        return et;
                    });

                var deleteByCIIDMutationName = TraitEntityTypesNameGenerator.GenerateDeleteByCIIDMutationName(traitID);
                tet.FieldAsync(deleteByCIIDMutationName, new BooleanGraphType(),
                    arguments: new QueryArguments(
                        new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" },
                        new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "writeLayer" },
                        new QueryArgument(new NonNullGraphType(new GuidGraphType())) { Name = "ciid" }
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
                            .WithTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate())
                            .WithLayersetAsync(async trans => await layerModel.BuildLayerSet(layerStrings, trans), context.Path);

                        var layerset = userContext.GetLayerSet(context.Path);
                        var timeThreshold = userContext.GetTimeThreshold(context.Path);
                        var trans = userContext.Transaction;

                        var changeset = new ChangesetProxy(userContext.User.InDatabase, userContext.GetTimeThreshold(context.Path), changesetModel);
                        var removed = await traitEntityModel.TryToDelete(ciid, layerset, writeLayerID, new DataOriginV1(DataOriginType.Manual), changeset, trans);

                        return removed;
                    });

                if (elementTypeContainer.IDInputType != null) // only add *byDataID-mutations for trait entities that have an ID
                {
                    var upsertByDataIDMutationName = TraitEntityTypesNameGenerator.GenerateUpsertByDataIDMutationName(traitID);
                    tet.FieldAsync(upsertByDataIDMutationName, elementTypeContainer.ElementWrapper,
                        arguments: new QueryArguments(
                            new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" },
                            new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "writeLayer" },
                            new QueryArgument(new NonNullGraphType(elementTypeContainer.UpsertInputType)) { Name = "input" }),
                        resolve: async context =>
                        {
                            var layerStrings = context.GetArgument<string[]>("layers")!;
                            var writeLayerID = context.GetArgument<string>("writeLayer")!;

                            var userContext = await context.SetupUserContext()
                                .WithTimeThreshold(TimeThreshold.BuildLatest(), context.Path)
                                .WithTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate())
                                .WithLayersetAsync(async trans => await layerModel.BuildLayerSet(layerStrings, trans), context.Path);

                            var layerset = userContext.GetLayerSet(context.Path);
                            var timeThreshold = userContext.GetTimeThreshold(context.Path);
                            var trans = userContext.Transaction;

                            if (context.GetArgument(typeof(object), "input") is not IDictionary<string, object> upsertInputCollection)
                                throw new Exception("Invalid input object for upsert detected");

                            var (inputAttributeValues, inputRelationValues) = TraitEntityHelper.InputDictionary2AttributeAndRelationTuples(upsertInputCollection, elementTypeContainer.Trait);
                            var idAttributeValues = TraitEntityHelper.InputDictionary2IDAttributeTuples(upsertInputCollection, elementTypeContainer.Trait);

                            if (idAttributeValues.IsEmpty())
                            {
                                throw new Exception("Cannot mutate trait entity that does not have proper ID field(s)");
                            }

                            var currentCIID = await traitEntityModel.GetSingleCIIDByAttributeValueTuples(idAttributeValues.Select(i => (i.name, i.value)).ToArray(), layerset, trans, timeThreshold);

                            var changeset = new ChangesetProxy(userContext.User.InDatabase, userContext.GetTimeThreshold(context.Path), changesetModel);

                            var et = await Upsert(currentCIID, inputAttributeValues, inputRelationValues, trans, changeset, traitEntityModel, layerset, writeLayerID);

                            return et;
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
                                .WithTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate())
                                .WithLayersetAsync(async trans => await layerModel.BuildLayerSet(layerStrings, trans), context.Path);

                            var layerset = userContext.GetLayerSet(context.Path);
                            var timeThreshold = userContext.GetTimeThreshold(context.Path);
                            var trans = userContext.Transaction;

                            var idCollection = context.GetArgument(typeof(object), "id") as IDictionary<string, object>;

                            if (idCollection == null)
                                throw new Exception("Invalid input object for trait entity ID detected");

                            var idAttributeValues = TraitEntityHelper.InputDictionary2IDAttributeTuples(idCollection, elementTypeContainer.Trait);

                            // TODO: use data loader?
                            var foundCIID = await traitEntityModel.GetSingleCIIDByAttributeValueTuples(idAttributeValues, layerset, trans, timeThreshold);

                            if (!foundCIID.HasValue)
                                return false;

                            var changeset = new ChangesetProxy(userContext.User.InDatabase, userContext.GetTimeThreshold(context.Path), changesetModel);
                            var removed = await traitEntityModel.TryToDelete(foundCIID.Value, layerset, writeLayerID, new DataOriginV1(DataOriginType.Manual), changeset, trans);

                            return removed;
                        });
                }
            }
        }
    }
}
