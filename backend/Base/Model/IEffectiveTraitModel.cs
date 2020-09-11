﻿using Landscape.Base.Entity;
using Landscape.Base.Utils;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Landscape.Base.Model
{
    public interface IEffectiveTraitModel
    {
        Task<IEnumerable<EffectiveTraitSet>> CalculateEffectiveTraitSetForCIs(IEnumerable<MergedCI> cis, string[] traitNames, NpgsqlTransaction trans, TimeThreshold atTime);
        Task<EffectiveTraitSet> CalculateEffectiveTraitSetForCI(MergedCI ci, NpgsqlTransaction trans, TimeThreshold atTime);

        Task<EffectiveTrait> CalculateEffectiveTraitForCI(MergedCI ci, Trait trait, NpgsqlTransaction trans, TimeThreshold atTime);

        //Task<IEnumerable<Guid>> CalculateCIsWithEffectiveTrait(string traitName, LayerSet layerSet, NpgsqlTransaction trans, TimeThreshold atTime, Func<Guid, bool> ciFilter = null);

        Task<IDictionary<Guid, EffectiveTrait>> CalculateEffectiveTraitsForTraitName(string traitName, LayerSet layerSet, NpgsqlTransaction trans, TimeThreshold atTime, Func<Guid, bool> ciFilter = null);
        Task<IDictionary<Guid, EffectiveTrait>> CalculateEffectiveTraitsForTrait(Trait trait, LayerSet layerSet, NpgsqlTransaction trans, TimeThreshold atTime, Func<Guid, bool> ciFilter = null);

    }
}
