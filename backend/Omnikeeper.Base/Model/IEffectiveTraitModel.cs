using Npgsql;
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

        Task<EffectiveTrait?> CalculateEffectiveTraitForCI(MergedCI ci, Trait trait, IModelContext trans, TimeThreshold atTime);

        Task<bool> DoesCIHaveTrait(MergedCI ci, Trait trait, IModelContext trans, TimeThreshold atTime);

        Task<IEnumerable<MergedCI>> GetMergedCIsWithTrait(Trait trait, LayerSet layerSet, IModelContext trans, TimeThreshold atTime, Func<Guid, bool>? ciFilter = null);

        Task<IDictionary<Guid, (MergedCI ci, EffectiveTrait et)>> CalculateEffectiveTraitsForTrait(Trait trait, LayerSet layerSet, IModelContext trans, TimeThreshold atTime, Func<Guid, bool>? ciFilter = null);
    }
}
