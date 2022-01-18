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
using Omnikeeper.Base.Model.TraitBased;
using System.Collections.Immutable;
using Microsoft.Extensions.Logging;

namespace Omnikeeper.Model
{
    public class TraitsProvider : ITraitsProvider
    {
        private readonly GenericTraitEntityModel<RecursiveTrait, string> dataTraitModel;
        private readonly IMetaConfigurationModel metaConfigurationModel;
        private readonly IEnumerable<IPluginRegistration> loadedPlugins;
        private readonly ILogger<TraitsProvider> logger;

        public TraitsProvider(GenericTraitEntityModel<RecursiveTrait, string> dataTraitModel, IMetaConfigurationModel metaConfigurationModel, IEnumerable<IPluginRegistration> loadedPlugins, ILogger<TraitsProvider> logger)
        {
            this.dataTraitModel = dataTraitModel;
            this.metaConfigurationModel = metaConfigurationModel;
            this.loadedPlugins = loadedPlugins;
            this.logger = logger;
        }

        // TODO: caching of active trait sets
        public async Task<IDictionary<string, ITrait>> GetActiveTraits(IModelContext trans, TimeThreshold timeThreshold)
        {
            var pluginTraitSets = new Dictionary<string, IEnumerable<RecursiveTrait>>();
            foreach (var plugin in loadedPlugins)
            {
                try
                {
                    pluginTraitSets.Add($"okplugin-{plugin.Name}", plugin.DefinedTraits);
                } catch (Exception e)
                {
                    logger.LogError(e, $"Could not load defined traits from plugin {plugin.Name}");
                }
            }


            // TODO, NOTE: this merges non-DB trait sets, that are not historic and DB traits sets that are... what should we do here?
            var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(trans);
            var configuredRecursiveDataTraitSet = await dataTraitModel.GetAllByDataID(metaConfiguration.ConfigLayerset, trans, timeThreshold);
            var allTraitSets = new Dictionary<string, IEnumerable<RecursiveTrait>>() {
                { "core", CoreTraits.RecursiveTraits },
                { "data", configuredRecursiveDataTraitSet.Values }
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
            if (IDs.IsEmpty())
                return ImmutableDictionary<string, ITrait>.Empty;

            // TODO: can be done more efficiently?
            var ts = await GetActiveTraits(trans, timeThreshold);

            var foundTraits = ts.Where(t => IDs.Contains(t.Key)).ToDictionary(t => t.Key, t => t.Value);
            if (foundTraits.Count() < IDs.Count())
                throw new Exception($"Encountered unknown trait(s): {string.Join(",", IDs.Except(foundTraits.Select(t => t.Key)))}");
            return foundTraits;
        }
    }
}
