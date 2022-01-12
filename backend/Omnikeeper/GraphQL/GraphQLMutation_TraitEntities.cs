using GraphQL;
using GraphQL.Types;
using Omnikeeper.Base.AttributeValues;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly ITraitsProvider traitsProvider;
        private readonly IAttributeModel attributeModel;
        private readonly IRelationModel relationModel;
        private readonly IEffectiveTraitModel effectiveTraitModel;
        private readonly ICIModel ciModel;
        private readonly IDataLoaderService dataLoaderService;
        private readonly ILayerModel layerModel;
        private readonly IChangesetModel changesetModel;

        public TraitEntitiesMutationSchemaLoader(GraphQLMutation tet, ITraitsProvider traitsProvider, IAttributeModel attributeModel, IRelationModel relationModel,
            IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IDataLoaderService dataLoaderService, ILayerModel layerModel, IChangesetModel changesetModel)
        {
            this.tet = tet;
            this.traitsProvider = traitsProvider;
            this.attributeModel = attributeModel;
            this.relationModel = relationModel;
            this.effectiveTraitModel = effectiveTraitModel;
            this.ciModel = ciModel;
            this.dataLoaderService = dataLoaderService;
            this.layerModel = layerModel;
            this.changesetModel = changesetModel;
        }

        private static string GenerateUpsertMutationName(string traitID) => "upsert_" + TraitEntitiesType.SanitizeMutationName(traitID);
        private static string GenerateUpsertTraitEntityInputGraphTypeName(ITrait trait) => TraitEntitiesType.SanitizeTypeName("TE_Upsert_Input_" + trait.ID);

        public class UpsertInputType : InputObjectGraphType
        {
            public UpsertInputType(ITrait at)
            {
                Name = GenerateUpsertTraitEntityInputGraphTypeName(at);

                foreach (var ta in at.RequiredAttributes)
                {
                    var graphType = AttributeValueHelper.AttributeValueType2GraphQLType(ta.AttributeTemplate.Type.GetValueOrDefault(AttributeValueType.Text), ta.AttributeTemplate.IsArray.GetValueOrDefault(false));
                    AddField(new FieldType()
                    {
                        Name = TraitEntitiesType.GenerateTraitAttributeFieldName(ta),
                        ResolvedType = new NonNullGraphType(graphType)
                    });
                }
                foreach (var ta in at.OptionalAttributes)
                {
                    var graphType = AttributeValueHelper.AttributeValueType2GraphQLType(ta.AttributeTemplate.Type.GetValueOrDefault(AttributeValueType.Text), ta.AttributeTemplate.IsArray.GetValueOrDefault(false));
                    AddField(new FieldType()
                    {
                        Name = TraitEntitiesType.GenerateTraitAttributeFieldName(ta),
                        ResolvedType = graphType
                    });
                }
                // TODO: add relations
            }
        }

        public async Task Init(IModelContext trans, ISchema schema)
        {
            var timeThreshold = TimeThreshold.BuildLatest();
            var activeTraits = await traitsProvider.GetActiveTraits(trans, timeThreshold);

            foreach (var at in activeTraits)
            {
                var traitID = at.Key;
                if (traitID == TraitEmpty.StaticID) // ignore the empty trait
                    continue;

                var upsertInputType = new UpsertInputType(at.Value);

                // TODO: we should re-use the types from the queries
                var tt = new ElementType(at.Value);
                var ttWrapper = new ElementWrapperType(at.Value, tt, traitsProvider, dataLoaderService, ciModel);

                schema.RegisterTypes(upsertInputType, tt, ttWrapper);

                var upsertMutationName = GenerateUpsertMutationName(traitID);

                var traitEntityModel = new TraitEntityModel(at.Value, effectiveTraitModel, ciModel, attributeModel, relationModel);

                tet.FieldAsync(upsertMutationName, ttWrapper,
                    arguments: new QueryArguments(
                        new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" },
                        new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "writeLayer" },
                        new QueryArgument(new NonNullGraphType(upsertInputType)) { Name = "input" }),
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

                        var inputAttributeValues = TraitEntitiesType.InputDictionary2AttributeTuples(upsertInputCollection, at.Value);
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

                // TODO: deletes
            }
        }
    }
}
