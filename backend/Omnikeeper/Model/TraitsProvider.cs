using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Npgsql;
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
        private readonly IRecursiveTraitModel traitModel;
        private readonly IServiceProvider sp;

        public TraitsProvider(IRecursiveTraitModel traitModel, IServiceProvider sp)
        {
            this.traitModel = traitModel;
            this.sp = sp;
        }

        // TODO: caching of active trait sets
        public async Task<TraitSet> GetActiveTraitSet(IModelContext trans, TimeThreshold timeThreshold)
        {
            var dbTraitSet = await traitModel.GetRecursiveTraitSet(trans, timeThreshold);

            var computeLayerBrains = sp.GetServices<IComputeLayerBrain>(); // HACK: we get the CLBs here and not in the constructor because that would lead to a circular dependency
            var nonDBTraitSets = new Dictionary<string, RecursiveTraitSet>();
            foreach (var clb in computeLayerBrains)
                nonDBTraitSets.Add($"CLB-{clb.Name}", clb.DefinedTraits);

            // TODO, NOTE: this merges non-DB trait sets, that are not historic and DB traits sets that are... what should we do here?
            var allTraitSets = new Dictionary<string, RecursiveTraitSet>() { { "default", dbTraitSet } };
            foreach (var kv in nonDBTraitSets)
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
            return TraitSet.Build(flattened);
        }

        public static MyJSONSerializer<RecursiveTraitSet> TraitSetSerializer = new MyJSONSerializer<RecursiveTraitSet>(() =>
        {
            var s = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Objects
            };
            s.Converters.Add(new StringEnumConverter());
            return s;
        });
    }
}
