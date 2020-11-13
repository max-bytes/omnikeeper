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
        Task<IEnumerable<EffectiveTraitSet>> CalculateEffectiveTraitSetForCIs(IEnumerable<MergedCI> cis, string[] traitNames, IModelContext trans, TimeThreshold atTime);
        Task<EffectiveTraitSet> CalculateEffectiveTraitSetForCI(MergedCI ci, IModelContext trans, TimeThreshold atTime);

        Task<EffectiveTrait?> CalculateEffectiveTraitForCI(MergedCI ci, Trait trait, IModelContext trans, TimeThreshold atTime);

        Task<IEnumerable<MergedCI>?> CalculateMergedCIsWithTrait(string traitName, LayerSet layerSet, IModelContext trans, TimeThreshold atTime, Func<Guid, bool>? ciFilter = null);
        Task<IEnumerable<MergedCI>> CalculateMergedCIsWithTrait(Trait trait, LayerSet layerSet, IModelContext trans, TimeThreshold atTime, Func<Guid, bool>? ciFilter = null);

        Task<IDictionary<Guid, EffectiveTrait>?> CalculateEffectiveTraitsForTraitName(string traitName, LayerSet layerSet, IModelContext trans, TimeThreshold atTime, Func<Guid, bool>? ciFilter = null);
        Task<IDictionary<Guid, EffectiveTrait>> CalculateEffectiveTraitsForTrait(Trait trait, LayerSet layerSet, IModelContext trans, TimeThreshold atTime, Func<Guid, bool>? ciFilter = null);

    }
}
