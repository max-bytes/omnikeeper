using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Model
{
    // TODO: think about caching?
    public class PredicateModel : TraitDataConfigBaseModel<Predicate>, IPredicateModel
    {
        public PredicateModel(IBaseConfigurationModel baseConfigurationModel, IEffectiveTraitModel effectiveTraitModel)
            : base(CoreTraits.PredicateFlattened, baseConfigurationModel, effectiveTraitModel)
        {}

        public async Task<Predicate> GetPredicate(string id, TimeThreshold timeThreshold, IModelContext trans)
        {
            IDValidations.ValidatePredicateIDThrow(id);

            return await Get(id, timeThreshold, trans);
        }

        public async Task<(Guid,Predicate)> TryToGetPredicate(string id, TimeThreshold timeThreshold, IModelContext trans)
        {
            IDValidations.ValidatePredicateIDThrow(id);

            return await TryToGet(id, timeThreshold, trans);
        }

        protected override (Predicate, string) EffectiveTrait2DC(EffectiveTrait et)
        {
            var predicateID = TraitConfigDataUtils.ExtractMandatoryScalarTextAttribute(et, "id");
            var wordingFrom = TraitConfigDataUtils.ExtractMandatoryScalarTextAttribute(et, "wording_from");
            var wordingTo = TraitConfigDataUtils.ExtractMandatoryScalarTextAttribute(et, "wording_to");
            return (new Predicate(predicateID, wordingFrom, wordingTo), predicateID);
        }

        public async Task<IDictionary<string, Predicate>> GetPredicates(IModelContext trans, TimeThreshold timeThreshold)
        {
            return await GetAll(trans, timeThreshold);
        }
    }
}
