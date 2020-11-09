using Npgsql;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface IEffectiveTraitModel
    {
        Task<IEnumerable<EffectiveTrait>> CalculateEffectiveTraitsForCI(MergedCI ci, NpgsqlTransaction trans, TimeThreshold atTime);

        Task<EffectiveTrait> CalculateEffectiveTraitForCI(MergedCI ci, Trait trait, NpgsqlTransaction trans, TimeThreshold atTime);

        Task<bool> DoesCIHaveTrait(MergedCI ci, Trait trait, NpgsqlTransaction trans, TimeThreshold atTime);

        Task<IEnumerable<MergedCI>> GetMergedCIsWithTrait(Trait trait, LayerSet layerSet, NpgsqlTransaction trans, TimeThreshold atTime, Func<Guid, bool> ciFilter = null);

        [Obsolete("Use the variant that passes the trait (not the traitName) instead")]
        Task<IDictionary<Guid, (MergedCI ci, EffectiveTrait et)>> CalculateEffectiveTraitsForTraitName(string traitName, LayerSet layerSet, NpgsqlTransaction trans, TimeThreshold atTime, Func<Guid, bool> ciFilter = null);
        Task<IDictionary<Guid, (MergedCI ci, EffectiveTrait et)>> CalculateEffectiveTraitsForTrait(Trait trait, LayerSet layerSet, NpgsqlTransaction trans, TimeThreshold atTime, Func<Guid, bool> ciFilter = null);

    }
}
