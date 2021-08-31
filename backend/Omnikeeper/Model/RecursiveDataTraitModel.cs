using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Model
{
    // TODO: think about caching?
    public class RecursiveDataTraitModel : TraitDataConfigBaseModel<RecursiveTrait, string>, IRecursiveDataTraitModel
    {
        public RecursiveDataTraitModel(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IBaseAttributeModel baseAttributeModel, IBaseRelationModel baseRelationModel)
            : base(CoreTraits.TraitFlattened, effectiveTraitModel, ciModel, baseAttributeModel, baseRelationModel)
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

        protected override (RecursiveTrait dc, string id) EffectiveTrait2DC(EffectiveTrait et, MergedCI ci)
        {
            var traitID = TraitConfigDataUtils.ExtractMandatoryScalarTextAttribute(et, "id");

            var requiredAttributes = TraitConfigDataUtils.ExtractMandatoryArrayJSONAttribute(et, "required_attributes", TraitAttribute.Serializer);
            var optionalAttributes = TraitConfigDataUtils.ExtractOptionalArrayJSONAttribute(et, "optional_attributes", TraitAttribute.Serializer, new List<TraitAttribute>());
            var requiredRelations = TraitConfigDataUtils.ExtractOptionalArrayJSONAttribute(et, "required_relation", TraitRelation.Serializer, new List<TraitRelation>());

            var requiredTraits = TraitConfigDataUtils.ExtractOptionalArrayTextAttribute(et, "required_traits", new string[0]);

            return (new RecursiveTrait(traitID, new TraitOriginV1(TraitOriginType.Data), requiredAttributes, optionalAttributes, requiredRelations, requiredTraits), traitID);
        }

        protected override IAttributeValue ID2AttributeValue(string id) => new AttributeScalarValueText(id);

        public async Task<(RecursiveTrait recursiveTrait, bool changed)> InsertOrUpdate(string id, IEnumerable<TraitAttribute> requiredAttributes, IEnumerable<TraitAttribute>? optionalAttributes, IEnumerable<TraitRelation>? requiredRelations, IEnumerable<string>? requiredTraits, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans)
        {
            return await InsertOrUpdateAttributes(id, layerSet, writeLayerID, dataOrigin, changesetProxy, trans,
                ("trait.id", new AttributeScalarValueText(id)),
                ("trait.required_attributes", AttributeArrayValueJSON.Build(requiredAttributes.Select(a => TraitAttribute.Serializer.SerializeToJObject(a)))),
                (optionalAttributes != null) ? ("trait.optional_attributes", AttributeArrayValueJSON.Build(optionalAttributes.Select(a => TraitAttribute.Serializer.SerializeToJObject(a)))) : default,
                (requiredRelations != null) ? ("trait.required_relations", AttributeArrayValueJSON.Build(optionalAttributes.Select(a => TraitAttribute.Serializer.SerializeToJObject(a)))) : default,
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
                "trait.required_traits",
                ICIModel.NameAttribute
            );
        }
    }
}
