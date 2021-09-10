using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Model
{
    // TODO: think about caching?
    public class PredicateModel : TraitDataConfigBaseModel<Predicate, string>, IPredicateModel
    {
        public PredicateModel(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IBaseAttributeModel baseAttributeModel, IBaseRelationModel baseRelationModel)
            : base(CoreTraits.PredicateFlattened, effectiveTraitModel, ciModel, baseAttributeModel, baseRelationModel)
        { }

        public async Task<Predicate> GetPredicate(string id, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            IDValidations.ValidatePredicateIDThrow(id);

            return await Get(id, layerSet, timeThreshold, trans);
        }

        public async Task<(Guid, Predicate)> TryToGetPredicate(string id, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            IDValidations.ValidatePredicateIDThrow(id);

            return await TryToGet(id, layerSet, timeThreshold, trans);
        }

        public async Task<IDictionary<string, Predicate>> GetPredicates(LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold)
        {
            return await GetAll(layerSet, trans, timeThreshold);
        }

        protected override (Predicate, string) EffectiveTrait2DC(EffectiveTrait et, MergedCI ci)
        {
            var predicateID = TraitConfigDataUtils.ExtractMandatoryScalarTextAttribute(et, "id");
            var wordingFrom = TraitConfigDataUtils.ExtractMandatoryScalarTextAttribute(et, "wording_from");
            var wordingTo = TraitConfigDataUtils.ExtractMandatoryScalarTextAttribute(et, "wording_to");
            return (new Predicate(predicateID, wordingFrom, wordingTo), predicateID);
        }

        protected override IAttributeValue ID2AttributeValue(string id) => new AttributeScalarValueText(id);

        public async Task<(Predicate predicate, bool changed)> InsertOrUpdate(string id, string wordingFrom, string wordingTo, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans)
        {
            return await InsertOrUpdateAttributes(id, layerSet, writeLayerID, dataOrigin, changesetProxy, trans,
                ("predicate.id", new AttributeScalarValueText(id)),
                ("predicate.wording_from", new AttributeScalarValueText(wordingFrom)),
                ("predicate.wording_to", new AttributeScalarValueText(wordingTo)),
                (ICIModel.NameAttribute, new AttributeScalarValueText($"Predicate - {id}"))
            );
        }

        public async Task<bool> TryToDelete(string id, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans)
        {
            return await TryToDelete(id, layerSet, writeLayerID, dataOrigin, changesetProxy, trans,
                "predicate.id",
                "predicate.wording_from",
                "predicate.wording_to",
                ICIModel.NameAttribute
            );
        }
    }
}
