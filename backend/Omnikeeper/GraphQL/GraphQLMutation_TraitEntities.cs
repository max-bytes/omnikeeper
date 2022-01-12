using GraphQL;
using GraphQL.Types;
using Omnikeeper.Base.AttributeValues;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        private readonly IEffectiveTraitModel effectiveTraitModel;
        private readonly ICIModel ciModel;
        private readonly IDataLoaderService dataLoaderService;
        private readonly ILayerModel layerModel;

        public TraitEntitiesMutationSchemaLoader(GraphQLMutation tet, ITraitsProvider traitsProvider, IAttributeModel attributeModel,
            IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IDataLoaderService dataLoaderService, ILayerModel layerModel)
        {
            this.tet = tet;
            this.traitsProvider = traitsProvider;
            this.attributeModel = attributeModel;
            this.effectiveTraitModel = effectiveTraitModel;
            this.ciModel = ciModel;
            this.dataLoaderService = dataLoaderService;
            this.layerModel = layerModel;
        }

        private static string GenerateUpsertMutationName(string traitID) => "upsert_" + TraitEntitiesType.SanitizeMutationName(traitID);
        private static string GenerateUpsertTraitEntityInputGraphTypeName(ITrait trait) => TraitEntitiesType.SanitizeTypeName("TE_Upsert_Input_" + trait.ID);

        //private async Task<(T entity, Guid ciid)> GetSingleByDataID((string name, IAttributeValue value, bool isID)[] idAttributeValues, LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold)
        //{
        //    // TODO: improve performance by only fetching CIs with matching attribute values to begin with, not fetch ALL, then filter in code... maybe impossible
        //    var @as = NamedAttributesSelection.Build(idAttributeValues.Select(i => i.name).ToHashSet());
        //    var cisWithIDAttribute = await attributeModel.GetMergedAttributes(new AllCIIDsSelection(), @as, layerSet, trans, timeThreshold);
        //    var foundCIID = idAttributeInfos.FilterCIAttributesWithMatchingID(id, cisWithIDAttribute);

        //    if (foundCIID == default)
        //    { // no fitting entity found
        //        return default;
        //    }

        //    var ret = await GetSingleByCIID(foundCIID, layerSet, trans, timeThreshold);
        //    return ret;
        //}

        //private async Task<(EffectiveTrait et, Guid ciid)> GetSingleByCIID(Guid ciid, LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold)
        //{
        //    var ci = (await ciModel.GetMergedCIs(SpecificCIIDsSelection.Build(ciid), layerSet, false, NamedAttributesSelection.Build(relevantAttributesForTrait), trans, timeThreshold)).FirstOrDefault();
        //    if (ci == null) return default;
        //    var ciWithTrait = await effectiveTraitModel.GetEffectiveTraitForCI(ci, trait, layerSet, trans, timeThreshold);
        //    if (ciWithTrait == null) return default;
        //    return (ciWithTrait, ciid);
        //}

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

                schema.RegisterTypes(upsertInputType);

                var upsertMutationName = GenerateUpsertMutationName(traitID);

                // attribute names that are relevant for this trait
                var relevantAttributeNames = at.Value.RequiredAttributes.Select(a => a.AttributeTemplate.Name).Concat(at.Value.OptionalAttributes.Select(a => a.AttributeTemplate.Name)).ToHashSet();

                tet.FieldAsync<StringGraphType>(upsertMutationName, 
                    arguments: new QueryArguments(
                        new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" },
                        new QueryArgument(new NonNullGraphType(upsertInputType)) { Name = "input" }),
                    resolve: async context =>
                    {
                        var layerStrings = context.GetArgument<string[]>("layers")!;

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


                        //var current = await GetSingleByDataID(id, layerSet, trans, changesetProxy.TimeThreshold);

                        //var ciid = (current != default) ? current.ciid : await ciModel.CreateCI(trans);

                        //var changed = await attributeModel.BulkReplaceAttributes(new BulkCIAttributeDataCIAndAttributeNameScope(writeLayer, fragments, relevantCIs, relevantAttributeNames), changesetProxy, dataOrigin, trans, MaskHandlingForRemovalApplyNoMask.Instance);



                        return "todo";
                    });
            }
        }
    }
}
