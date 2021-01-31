using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface IEffectiveTraitModel
    {
        Task<IEnumerable<EffectiveTrait>> CalculateEffectiveTraitsForCI(MergedCI ci, IModelContext trans, TimeThreshold atTime);

        Task<EffectiveTrait?> CalculateEffectiveTraitForCI(MergedCI ci, ITrait trait, IModelContext trans, TimeThreshold atTime);

        Task<bool> DoesCIHaveTrait(MergedCI ci, ITrait trait, IModelContext trans, TimeThreshold atTime);

        Task<IEnumerable<MergedCI>> GetMergedCIsWithTrait(ITrait trait, LayerSet layerSet, ICIIDSelection ciidSelection, IModelContext trans, TimeThreshold atTime);

        Task<IDictionary<Guid, (MergedCI ci, EffectiveTrait et)>> CalculateEffectiveTraitsForTrait(ITrait trait, LayerSet layerSet, ICIIDSelection ciidSelection, IModelContext trans, TimeThreshold atTime);
    }
}
