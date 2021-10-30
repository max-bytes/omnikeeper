using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface IEffectiveTraitModel
    {
        Task<IEnumerable<MergedCI>> FilterCIsWithTrait(IEnumerable<MergedCI> cis, ITrait trait, LayerSet layers, IModelContext trans, TimeThreshold atTime);
        Task<IEnumerable<MergedCI>> FilterCIsWithoutTrait(IEnumerable<MergedCI> cis, ITrait trait, LayerSet layers, IModelContext trans, TimeThreshold atTime);

        Task<IDictionary<Guid, EffectiveTrait>> GetEffectiveTraitsForTrait(ITrait trait, IEnumerable<MergedCI> cis, LayerSet layerSet, IModelContext trans, TimeThreshold atTime);
    }

    public static class EffectiveTraitModelExtensions
    {
        public static async Task<EffectiveTrait?> GetEffectiveTraitForCI(this IEffectiveTraitModel model, MergedCI ci, ITrait trait, LayerSet layers, IModelContext trans, TimeThreshold atTime)
        {
            var r = await model.GetEffectiveTraitsForTrait(trait, new MergedCI[] { ci }, layers, trans, atTime);
            if (r.TryGetValue(ci.ID, out var outValue))
                return outValue;
            return null;
        }
    }
}
