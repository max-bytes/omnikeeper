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
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Model
{
    public class TraitsProvider : ITraitsProvider
    {
        private readonly RecursiveTraitModel dataTraitModel;
        private readonly IMetaConfigurationModel metaConfigurationModel;
        private readonly IChangesetModel changesetModel;
        private readonly IEnumerable<IPluginRegistration> loadedPlugins;
        private readonly ILogger<TraitsProvider> logger;

        public TraitsProvider(RecursiveTraitModel dataTraitModel,
            IMetaConfigurationModel metaConfigurationModel, IChangesetModel changesetModel, IEnumerable<IPluginRegistration> loadedPlugins, ILogger<TraitsProvider> logger)
        {
            this.dataTraitModel = dataTraitModel;
            this.metaConfigurationModel = metaConfigurationModel;
            this.changesetModel = changesetModel;
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

            var flattened = RecursiveTraitService.FlattenRecursiveTraits(ret, errorF);

            var finalTraits = new Dictionary<string, ITrait>();
            foreach (var kv in flattened)
                finalTraits.Add(kv.Key, kv.Value);
            var traitEmpty = new TraitEmpty();
            if (!finalTraits.TryAdd(traitEmpty.ID, traitEmpty)) // mix in empty trait
                errorF($"Could not add trait with ID {traitEmpty.ID}. A trait with that ID was already added.");
            return finalTraits;
        }

        // returns null if there are no data traits
        public async Task<DateTimeOffset?> GetLatestChangeToActiveDataTraits(IModelContext trans, TimeThreshold timeThreshold)
        {
            // check data traits changes through their changesets
            // TODO: this does not properly detect changes that only remove a trait
            var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(trans);
            var relevantChangesetIDs = await dataTraitModel.GetRelevantChangesetIDsForAll(metaConfiguration.ConfigLayerset, trans, timeThreshold);
            if (relevantChangesetIDs.IsEmpty())
                return null;
            var relevantChangesets = await changesetModel.GetChangesets(relevantChangesetIDs, trans);
            if (relevantChangesets.IsEmpty())
                return null;
            return relevantChangesets.Select(c => c.Timestamp).Max();
        }

        public async Task<ITrait?> GetActiveTrait(string traitID, IModelContext trans, TimeThreshold timeThreshold)
        {
            // TODO: can be done more efficiently? here we get ALL traits, just to select a single one... but the flattening is necessary
            var ts = await GetActiveTraits(trans, timeThreshold, _ => { });

            if (ts.TryGetValue(traitID, out var trait))
                return trait;
            return null;
        }

        public async Task<IDictionary<string, ITrait>> GetActiveTraitsByIDs(IEnumerable<string> IDs, IModelContext trans, TimeThreshold timeThreshold)
        {
            if (IDs.IsEmpty())
                return ImmutableDictionary<string, ITrait>.Empty;

            // TODO: can be done more efficiently?
            var ts = await GetActiveTraits(trans, timeThreshold, _ => { });

            var foundTraits = ts.Where(t => IDs.Contains(t.Key)).ToDictionary(t => t.Key, t => t.Value);
            if (foundTraits.Count() < IDs.Count())
                throw new Exception($"Encountered unknown trait(s): {string.Join(",", IDs.Except(foundTraits.Select(t => t.Key)))}");
            return foundTraits;
        }
    }
}
