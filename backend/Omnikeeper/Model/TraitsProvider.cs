using Microsoft.Extensions.DependencyInjection;
using Omnikeeper.Base.CLB;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Model
{
    public class TraitsProvider : ITraitsProvider
    {
        private readonly IRecursiveDataTraitModel dataTraitModel;
        private readonly IServiceProvider sp;

        public TraitsProvider(IRecursiveDataTraitModel dataTraitModel, IServiceProvider sp)
        {
            this.dataTraitModel = dataTraitModel;
            this.sp = sp;
        }

        // TODO: caching of active trait sets
        public async Task<IDictionary<string, ITrait>> GetActiveTraits(IModelContext trans, TimeThreshold timeThreshold)
        {
            var computeLayerBrains = sp.GetServices<IComputeLayerBrain>(); // HACK: we get the CLBs here and not in the constructor because that would lead to a circular dependency
            var clbTraitSets = new Dictionary<string, IEnumerable<RecursiveTrait>>();
            foreach (var clb in computeLayerBrains)
                clbTraitSets.Add($"CLB-{clb.Name}", clb.DefinedTraits);

            // TODO, NOTE: this merges non-DB trait sets, that are not historic and DB traits sets that are... what should we do here?
            var configuredRecursiveDataTraitSet = await dataTraitModel.GetRecursiveTraits(trans, timeThreshold);
            var allTraitSets = new Dictionary<string, IEnumerable<RecursiveTrait>>() {
                { "core", CoreTraits.RecursiveTraits },
                { "data", configuredRecursiveDataTraitSet }
            };
            foreach (var kv in clbTraitSets)
                allTraitSets.Add(kv.Key, kv.Value);

            // TODO: this merges the traits from all sources/sets, but it does so non-deterministicly
            var ret = new Dictionary<string, RecursiveTrait>();
            foreach (var kv in allTraitSets)
            {
                var source = kv.Key;
                foreach (var rt in kv.Value)
                    ret[rt.ID] = rt;
            }

            var flattened = RecursiveTraitService.FlattenRecursiveTraits(ret);

            var finalTraits = new Dictionary<string, ITrait>();
            foreach (var kv in flattened)
                finalTraits.Add(kv.Key, kv.Value);
            var traitEmpty = new TraitEmpty();
            finalTraits.Add(traitEmpty.ID, traitEmpty); // mix in empty trait
            return finalTraits;
        }

        public async Task<ITrait?> GetActiveTrait(string traitID, IModelContext trans, TimeThreshold timeThreshold)
        {
            // TODO: can be done more efficiently? here we get ALL traits, just to select a single one... but the flattening is necessary
            var ts = await GetActiveTraits(trans, timeThreshold);

            if (ts.TryGetValue(traitID, out var trait))
                return trait;
            return null;
        }
    }
}
