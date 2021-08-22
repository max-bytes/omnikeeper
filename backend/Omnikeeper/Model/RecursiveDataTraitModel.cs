using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Model
{
    // TODO: think about caching?
    public class RecursiveDataTraitModel : TraitDataConfigBaseModel<RecursiveTrait>, IRecursiveDataTraitModel
    {
        public RecursiveDataTraitModel(IEffectiveTraitModel effectiveTraitModel, IBaseConfigurationModel baseConfigurationModel)
            : base(CoreTraits.TraitFlattened, baseConfigurationModel, effectiveTraitModel)
        {
        }

        public async Task<RecursiveTrait> GetRecursiveTrait(string id, TimeThreshold timeThreshold, IModelContext trans)
        {
            IDValidations.ValidateTraitIDThrow(id);

            return await Get(id, timeThreshold, trans);
        }

        public async Task<(Guid, RecursiveTrait)> TryToGetRecursiveTrait(string id, TimeThreshold timeThreshold, IModelContext trans)
        {
            IDValidations.ValidateTraitIDThrow(id);

            return await TryToGet(id, timeThreshold, trans);
        }

        public async Task<IEnumerable<RecursiveTrait>> GetRecursiveTraits(IModelContext trans, TimeThreshold timeThreshold)
        {
            var traits = await GetAll(trans, timeThreshold);
            return traits.Values;
        }

        protected override (RecursiveTrait dc, string id) EffectiveTrait2DC(EffectiveTrait et)
        {
            var traitID = TraitConfigDataUtils.ExtractMandatoryScalarTextAttribute(et, "id");

            var requiredAttributes = TraitConfigDataUtils.ExtractMandatoryArrayJSONAttribute(et, "required_attributes", TraitAttribute.Serializer);
            var optionalAttributes = TraitConfigDataUtils.ExtractOptionalArrayJSONAttribute(et, "optional_attributes", TraitAttribute.Serializer, new List<TraitAttribute>());
            var requiredRelations = TraitConfigDataUtils.ExtractOptionalArrayJSONAttribute(et, "required_relation", TraitRelation.Serializer, new List<TraitRelation>());

            var requiredTraits = TraitConfigDataUtils.ExtractOptionalArrayTextAttribute(et, "required_traits", new string[0]);

            return (new RecursiveTrait(traitID, new TraitOriginV1(TraitOriginType.Data), requiredAttributes, optionalAttributes, requiredRelations, requiredTraits), traitID);
        }
    }
}
