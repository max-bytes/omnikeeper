using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Plugins;
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
        private readonly RecursiveTraitModel dataTraitModel;
        private readonly IMetaConfigurationModel metaConfigurationModel;
        private readonly IEnumerable<IPluginRegistration> loadedPlugins;
        private readonly ILogger<TraitsProvider> logger;

        public TraitsProvider(RecursiveTraitModel dataTraitModel,
            IMetaConfigurationModel metaConfigurationModel, IEnumerable<IPluginRegistration> loadedPlugins, ILogger<TraitsProvider> logger)
        {
            this.dataTraitModel = dataTraitModel;
            this.metaConfigurationModel = metaConfigurationModel;
            this.loadedPlugins = loadedPlugins;
            this.logger = logger;
        }

        public async Task<IDictionary<string, ITrait>> GetActiveTraits(IModelContext trans, TimeThreshold timeThreshold, Action<string> errorF)
        {
            var pluginTraitSets = new Dictionary<string, IEnumerable<RecursiveTrait>>();
            foreach (var plugin in loadedPlugins)
            {
                try
                {
                    pluginTraitSets.Add($"okplugin-{plugin.Name}", plugin.DefinedTraits);
                }
                catch (Exception e)
                {
                    var errorStr = $"Could not load defined traits from plugin {plugin.Name}: {e.Message}";
                    errorF(errorStr);
                    logger.LogError(e, errorStr);
                }
            }


            // TODO, NOTE: this merges non-DB trait sets, that are not historic and DB traits sets that are... what should we do here?
            var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(trans);
            var configuredRecursiveDataTraitSet = await dataTraitModel.GetByDataID(AllCIIDsSelection.Instance, metaConfiguration.ConfigLayerset, trans, timeThreshold);
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
                {
                    if (!ret.TryAdd(rt.ID, rt))
                        errorF($"Could not add trait with ID {rt.ID} from source {source}. A trait with that ID was already added.");
                }
            }

            var finalTraits = new Dictionary<string, ITrait>();

            try
            {
                var flattened = RecursiveTraitService.FlattenRecursiveTraits(ret, errorF);
                foreach (var kv in flattened)
                    finalTraits.Add(kv.Key, kv.Value);
            }
            catch (Exception e)
            {
                var errorStr = $"Could not flatten recursive traits: {e.Message}";
                errorF(errorStr);
                logger.LogError(e, errorStr);
            }

            var traitEmpty = new TraitEmpty();
            if (!finalTraits.TryAdd(traitEmpty.ID, traitEmpty)) // mix in empty trait
                errorF($"Could not add trait with ID {traitEmpty.ID}. A trait with that ID was already added.");
            return finalTraits;
        }

        // returns null if there are no data traits
        public async Task<DateTimeOffset?> GetLatestChangeToActiveDataTraits(IModelContext trans, TimeThreshold timeThreshold)
        {
            // check data traits changes through their changesets
            var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(trans);
            var latestChangeset = await dataTraitModel.GetLatestRelevantChangesetOverallHeuristic(AllCIIDsSelection.Instance, metaConfiguration.ConfigLayerset, trans, timeThreshold);
            return latestChangeset?.Timestamp;
        }
    }
}
