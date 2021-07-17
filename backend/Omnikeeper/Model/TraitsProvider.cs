﻿using Microsoft.Extensions.DependencyInjection;
using Omnikeeper.Base.CLB;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Model
{
    public class TraitsProvider : ITraitsProvider
    {
        private readonly IRecursiveTraitModel traitModel;
        private readonly IRecursiveDataTraitModel dataTraitModel;
        private readonly IServiceProvider sp;

        public TraitsProvider(IRecursiveTraitModel traitModel, IRecursiveDataTraitModel dataTraitModel, IServiceProvider sp)
        {
            this.traitModel = traitModel;
            this.dataTraitModel = dataTraitModel;
            this.sp = sp;
        }

        // TODO: caching of active trait sets
        public async Task<TraitSet> GetActiveTraitSet(IModelContext trans, TimeThreshold timeThreshold)
        {
            var computeLayerBrains = sp.GetServices<IComputeLayerBrain>(); // HACK: we get the CLBs here and not in the constructor because that would lead to a circular dependency
            var clbTraitSets = new Dictionary<string, RecursiveTraitSet>();
            foreach (var clb in computeLayerBrains)
                clbTraitSets.Add($"CLB-{clb.Name}", clb.DefinedTraits);

            // TODO, NOTE: this merges non-DB trait sets, that are not historic and DB traits sets that are... what should we do here?
            var configuredRecursiveTraitSet = await traitModel.GetRecursiveTraitSet(trans, timeThreshold);
            var configuredRecursiveDataTraitSet = await dataTraitModel.GetRecursiveDataTraitSet(trans, timeThreshold);
            var allTraitSets = new Dictionary<string, RecursiveTraitSet>() {
                { "core", CoreTraits.Traits },
                { "configuration", configuredRecursiveTraitSet },
                { "data", configuredRecursiveDataTraitSet }
            };
            foreach (var kv in clbTraitSets)
                allTraitSets.Add(kv.Key, kv.Value);

            // TODO: this merges the traits from all sources/sets, but it does so non-deterministicly
            var ret = new Dictionary<string, RecursiveTrait>();
            foreach (var kv in allTraitSets)
            {
                var source = kv.Key;
                foreach (var kv2 in kv.Value.Traits)
                    ret[kv2.Key] = kv2.Value;
            }

            var flattened = RecursiveTraitService.FlattenDependentTraits(ret);

            var finalTraits = new List<ITrait>(flattened);
            finalTraits.Add(new TraitEmpty()); // mix in empty trait
            return TraitSet.Build(finalTraits);
        }

        public async Task<ITrait?> GetActiveTrait(string traitName, IModelContext trans, TimeThreshold timeThreshold)
        {
            // TODO: can be done more efficiently? here we get ALL traits, just to select a single one... but the flattening is necessary
            var ts = await GetActiveTraitSet(trans, timeThreshold);

            if (ts.Traits.TryGetValue(traitName, out var trait))
                return trait;
            return null;
        }
    }
}
