using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Plugins;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Model
{
    public class TraitsProvider : ITraitsProvider
    {
        private readonly IRecursiveDataTraitModel dataTraitModel;
        private readonly IBaseConfigurationModel baseConfigurationModel;
        private readonly IEnumerable<IPluginRegistration> loadedPlugins;

        public TraitsProvider(IRecursiveDataTraitModel dataTraitModel, IBaseConfigurationModel baseConfigurationModel, IEnumerable<IPluginRegistration> loadedPlugins)
        {
            this.dataTraitModel = dataTraitModel;
            this.baseConfigurationModel = baseConfigurationModel;
            this.loadedPlugins = loadedPlugins;
        }

        // TODO: caching of active trait sets
        public async Task<IDictionary<string, ITrait>> GetActiveTraits(IModelContext trans, TimeThreshold timeThreshold)
        {
            var pluginTraitSets = new Dictionary<string, IEnumerable<RecursiveTrait>>();
            foreach (var plugin in loadedPlugins)
                pluginTraitSets.Add($"okplugin-{plugin.Name}", plugin.DefinedTraits);


            // TODO, NOTE: this merges non-DB trait sets, that are not historic and DB traits sets that are... what should we do here?
            var baseConfiguration = await baseConfigurationModel.GetConfigOrDefault(trans);
            var configuredRecursiveDataTraitSet = await dataTraitModel.GetRecursiveTraits(new LayerSet(baseConfiguration.ConfigLayerset), trans, timeThreshold);
            var allTraitSets = new Dictionary<string, IEnumerable<RecursiveTrait>>() {
                { "core", CoreTraits.RecursiveTraits },
                { "data", configuredRecursiveDataTraitSet }
            };
            foreach (var kv in pluginTraitSets)
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

        public async Task<IDictionary<string, ITrait>> GetActiveTraitsByIDs(IEnumerable<string> IDs, IModelContext trans, TimeThreshold timeThreshold)
        {
            // TODO: can be done more efficiently?
            var ts = await GetActiveTraits(trans, timeThreshold);

            var foundTraits = ts.Where(t => IDs.Contains(t.Key)).ToDictionary(t => t.Key, t => t.Value);
            if (foundTraits.Count() < IDs.Count())
                throw new Exception($"Encountered unknown trait(s): {string.Join(",", IDs.Except(foundTraits.Select(t => t.Key)))}");
            return foundTraits;
        }
    }
}
