using GraphQL;
using GraphQL.Types;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Utils;
using Omnikeeper.GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using static Omnikeeper.GraphQL.Types.TraitEntitiesQuerySchemaLoader;
using static Omnikeeper.GraphQL.Types.TraitEntitiesType;

namespace Omnikeeper.GraphQL
{
    public partial class GraphQLMutation
    {
        public void CreateTraitEntities()
        {
        }
    }

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

        private static string GenerateUpsertMutationName(string traitID) => "upsert_" + SanitizeMutationName(traitID);
        private static string GenerateDeleteMutationName(string traitID) => "delete_" + SanitizeMutationName(traitID);

        public void Init(IEnumerable<ElementTypesContainer> typesContainers)
        {
            foreach (var typeContainer in typesContainers)
            {
                var traitID = typeContainer.Trait.ID;

                if (typeContainer.IDInputType == null)
                    continue; // Cannot add mutations for trait entities that do not have an ID

                var traitEntityModel = new TraitEntityModel(typeContainer.Trait, effectiveTraitModel, ciModel, attributeModel, relationModel);

                var upsertMutationName = GenerateUpsertMutationName(traitID);
                tet.FieldAsync(upsertMutationName, typeContainer.ElementWrapper,
                    arguments: new QueryArguments(
                        new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" },
                        new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "writeLayer" },
                        new QueryArgument(new NonNullGraphType(typeContainer.UpsertInputType)) { Name = "input" }),
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

                        var upsertInputCollection = context.GetArgument(typeof(object), "input") as IDictionary<string, object>;

                        if (upsertInputCollection == null)
                            throw new Exception("Invalid input object for upsert detected");

                        var inputAttributeValues = TraitEntitiesType.InputDictionary2AttributeTuples(upsertInputCollection, typeContainer.Trait);
                        var idAttributeValues = inputAttributeValues.Where(i => i.isID);

                        if (idAttributeValues.IsEmpty())
                        {
                            throw new Exception("Cannot mutate trait entity that does not have proper ID field(s)");
                        }

                        var currentCIID = await traitEntityModel.GetSingleCIIDByAttributeValueTuples(idAttributeValues.Select(i => (i.name, i.value)).ToArray(), layerset, trans, timeThreshold);

                        var ciid = (currentCIID.HasValue) ? currentCIID.Value : await ciModel.CreateCI(trans);

                        var changeset = new ChangesetProxy(userContext.User.InDatabase, userContext.GetTimeThreshold(context.Path), changesetModel);

                        var attributeFragments = inputAttributeValues.Select(i => new BulkCIAttributeDataCIAndAttributeNameScope.Fragment(ciid, i.name, i.value));

                        // TODO: relations
                        IList<(Guid thisCIID, string predicateID, IEnumerable<Guid> otherCIIDs)> incomingRelations = new List<(Guid thisCIID, string predicateID, IEnumerable<Guid> otherCIIDs)>();
                        IList<(Guid thisCIID, string predicateID, IEnumerable<Guid> otherCIIDs)> outgoingRelations = new List<(Guid thisCIID, string predicateID, IEnumerable<Guid> otherCIIDs)>();
                        var t = await traitEntityModel.InsertOrUpdate(ciid, attributeFragments, outgoingRelations, incomingRelations, layerset, writeLayerID, new DataOriginV1(DataOriginType.Manual), changeset, trans);

                        return t.et;
                    });

                var deleteMutationName = GenerateDeleteMutationName(traitID);
                tet.FieldAsync(deleteMutationName, new BooleanGraphType(),
                    arguments: new QueryArguments(
                        new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" },
                        new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "writeLayer" },
                        new QueryArgument(new NonNullGraphType(typeContainer.IDInputType)) { Name = "id" }
                    ),
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

                        var idAttributeValues = InputDictionary2AttributeTuples(idCollection, typeContainer.Trait)
                            .Where(t => t.isID)
                            .Select(t => (t.name, t.value))
                            .ToArray();

                        // TODO: use data loader?
                        var foundCIID = await traitEntityModel.GetSingleCIIDByAttributeValueTuples(idAttributeValues, layerset, trans, timeThreshold);

                        if (!foundCIID.HasValue)
                        {
                            return false;
                        }

                        var changeset = new ChangesetProxy(userContext.User.InDatabase, userContext.GetTimeThreshold(context.Path), changesetModel);
                        var removed = await traitEntityModel.TryToDelete(foundCIID.Value, layerset, writeLayerID, new DataOriginV1(DataOriginType.Manual), changeset, trans);

                        return removed;
                    });
            }
        }
    }
}
