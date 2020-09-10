using Landscape.Base.CLB;
using Landscape.Base.Entity;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LandscapeRegistry.Model
{
    public class TraitsProvider : ITraitsProvider
    {
        private readonly ITraitModel traitModel;
        private readonly IServiceProvider sp;

        public TraitsProvider(ITraitModel traitModel, IServiceProvider sp)
        {
            this.traitModel = traitModel;
            this.sp = sp;
        }

        public async Task<TraitSet> GetActiveTraitSet(NpgsqlTransaction trans, TimeThreshold timeThreshold)
        {
            var dbTraitSet = await traitModel.GetTraitSet(trans, timeThreshold);

            var computeLayerBrains = sp.GetServices<IComputeLayerBrain>(); // HACK: we get the CLBs here and not in the constructor because that would lead to a circular dependency
            var nonDBTraitSets = new Dictionary<string, TraitSet>();
            foreach (var clb in computeLayerBrains)
                nonDBTraitSets.Add($"CLB-{clb.Name}", clb.DefinedTraits);

            // TODO, NOTE: this merges non-DB trait sets, that are not historic and DB traits sets that are... what should we do here?
            var allTraitSets = new Dictionary<string, TraitSet>() { { "default", dbTraitSet } };
            foreach(var kv in nonDBTraitSets)
                allTraitSets.Add(kv.Key, kv.Value);

            // TODO: this merges the traits from all sources/sets, but it does so non-deterministicly
            var ret = new Dictionary<string, Trait>();
            foreach (var kv in allTraitSets)
            {
                var source = kv.Key;
                foreach (var kv2 in kv.Value.Traits)
                    ret[kv2.Key] = kv2.Value;
            }
            return TraitSet.Build(ret.Values);
        }

        public static MyJSONSerializer<TraitSet> TraitSetSerializer = new MyJSONSerializer<TraitSet>(() => {
            var s = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Objects
            };
            s.Converters.Add(new StringEnumConverter());
            return s;
        });
    }
}
