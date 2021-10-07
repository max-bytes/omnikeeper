using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    // TODO: think about caching?
    public class RecursiveDataTraitModel : IDBasedTraitDataConfigBaseModel<RecursiveTrait, string>, IRecursiveDataTraitModel
    {
        public static readonly RecursiveTrait Trait = new RecursiveTrait("__meta.config.trait", new TraitOriginV1(TraitOriginType.Core),
            new List<TraitAttribute>() {
                new TraitAttribute("id", CIAttributeTemplate.BuildFromParams("trait.id", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null), new CIAttributeValueConstraintTextRegex(IDValidations.TraitIDRegex))),
                new TraitAttribute("required_attributes", CIAttributeTemplate.BuildFromParams("trait.required_attributes", AttributeValueType.JSON, true, new CIAttributeValueConstraintArrayLength(1, null)))
            },
            new List<TraitAttribute>()
            {
                new TraitAttribute("optional_attributes", CIAttributeTemplate.BuildFromParams("trait.optional_attributes", AttributeValueType.JSON, true)),
                new TraitAttribute("required_relations", CIAttributeTemplate.BuildFromParams("trait.required_relations", AttributeValueType.JSON, true)),
                new TraitAttribute("optional_relations", CIAttributeTemplate.BuildFromParams("trait.optional_relations", AttributeValueType.JSON, true)),
                new TraitAttribute("required_traits", CIAttributeTemplate.BuildFromParams("trait.required_traits", AttributeValueType.Text, true)),
                new TraitAttribute("name", CIAttributeTemplate.BuildFromParams(ICIModel.NameAttribute, AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
            }
        );
        public static readonly GenericTrait TraitFlattened = RecursiveTraitService.FlattenSingleRecursiveTrait(Trait);

        public RecursiveDataTraitModel(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IBaseAttributeModel baseAttributeModel, IBaseRelationModel baseRelationModel)
            : base(TraitFlattened, effectiveTraitModel, ciModel, baseAttributeModel, baseRelationModel)
        {
        }

        public async Task<RecursiveTrait> GetRecursiveTrait(string id, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            IDValidations.ValidateTraitIDThrow(id);

            return await Get(id, layerSet, timeThreshold, trans);
        }

        public async Task<(Guid, RecursiveTrait)> TryToGetRecursiveTrait(string id, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            IDValidations.ValidateTraitIDThrow(id);

            return await TryToGet(id, layerSet, timeThreshold, trans);
        }

        public async Task<IEnumerable<RecursiveTrait>> GetRecursiveTraits(LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold)
        {
            var traits = await GetAll(layerSet, trans, timeThreshold);
            return traits.Values;
        }

        protected override (RecursiveTrait dc, string id) EffectiveTrait2DC(EffectiveTrait et)
        {
            var traitID = TraitConfigDataUtils.ExtractMandatoryScalarTextAttribute(et, "id");

            var requiredAttributes = TraitConfigDataUtils.ExtractMandatoryArrayJSONAttribute(et, "required_attributes", TraitAttribute.Serializer);
            var optionalAttributes = TraitConfigDataUtils.ExtractOptionalArrayJSONAttribute(et, "optional_attributes", TraitAttribute.Serializer, new List<TraitAttribute>());
            var requiredRelations = TraitConfigDataUtils.ExtractOptionalArrayJSONAttribute(et, "required_relations", TraitRelation.Serializer, new List<TraitRelation>());
            var optionalRelations = TraitConfigDataUtils.ExtractOptionalArrayJSONAttribute(et, "optional_relations", TraitRelation.Serializer, new List<TraitRelation>());

            var requiredTraits = TraitConfigDataUtils.ExtractOptionalArrayTextAttribute(et, "required_traits", new string[0]);

            return (new RecursiveTrait(traitID, new TraitOriginV1(TraitOriginType.Data), requiredAttributes, optionalAttributes, requiredRelations, optionalRelations, requiredTraits), traitID);
        }

        protected override IAttributeValue ID2AttributeValue(string id) => new AttributeScalarValueText(id);

        public async Task<(RecursiveTrait recursiveTrait, bool changed)> InsertOrUpdate(string id, IEnumerable<TraitAttribute> requiredAttributes, IEnumerable<TraitAttribute>? optionalAttributes, 
            IEnumerable<TraitRelation>? requiredRelations, IEnumerable<TraitRelation>? optionalRelations, IEnumerable<string>? requiredTraits, 
            LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans)
        {
            return await InsertOrUpdateAttributes(id, layerSet, writeLayerID, dataOrigin, changesetProxy, trans,
                ("trait.id", new AttributeScalarValueText(id)),
                ("trait.required_attributes", AttributeArrayValueJSON.Build(requiredAttributes.Select(a => TraitAttribute.Serializer.SerializeToJObject(a)))),
                (optionalAttributes != null) ? ("trait.optional_attributes", AttributeArrayValueJSON.Build(optionalAttributes.Select(a => TraitAttribute.Serializer.SerializeToJObject(a)))) : default,
                (requiredRelations != null) ? ("trait.required_relations", AttributeArrayValueJSON.Build(requiredRelations.Select(a => TraitRelation.Serializer.SerializeToJObject(a)))) : default,
                (optionalRelations != null) ? ("trait.optional_relations", AttributeArrayValueJSON.Build(optionalRelations.Select(a => TraitRelation.Serializer.SerializeToJObject(a)))) : default,
                (requiredTraits != null) ? ("trait.required_traits", AttributeArrayValueText.BuildFromString(requiredTraits)) : default,
                (ICIModel.NameAttribute, new AttributeScalarValueText($"Trait - {id}"))
            );
        }

        public async Task<bool> TryToDelete(string id, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans)
        {
            return await TryToDelete(id, layerSet, writeLayerID, dataOrigin, changesetProxy, trans,
                "trait.id",
                "trait.required_attributes",
                "trait.optional_attributes",
                "trait.required_relations",
                "trait.optional_relations",
                "trait.required_traits",
                ICIModel.NameAttribute
            );
        }
    }
}
