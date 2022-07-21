using Microsoft.Extensions.Logging;
using Omnikeeper.Base.CLB;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Omnikeeper.Validators
{
    public class ValidatorTraits : IValidator
    {
        private readonly IMetaConfigurationModel metaConfigurationModel;
        private readonly ITraitsProvider traitsProvider;

        public ValidatorTraits(IMetaConfigurationModel metaConfigurationModel, ITraitsProvider traitsProvider)
        {
            this.metaConfigurationModel = metaConfigurationModel;
            this.traitsProvider = traitsProvider;
        }

        public string Name => GetType().Name!;

        public async Task<ISet<string>> GetDependentLayerIDs(JsonDocument config, ILogger logger, IModelContextBuilder modelContextBuilder)
        {
            using var trans = modelContextBuilder.BuildImmediate();
            var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(trans);

            return metaConfiguration.ConfigLayers.ToHashSet();
        }

        public async Task<bool> Run(IReadOnlyDictionary<string, IReadOnlyList<Changeset>?> unprocessedChangesets, JsonDocument config, IModelContextBuilder modelContextBuilder, TimeThreshold timeThreshold, ILogger logger, IIssueAccumulator issueAccumulator)
        {
            using var trans = modelContextBuilder.BuildImmediate();

            // test building process for errors
            var traits = await traitsProvider.GetActiveTraits(trans, timeThreshold, 
                (errorStr) => issueAccumulator.TryAdd("trait-building", errorStr.GetHashCode().ToString(), errorStr));

            // check trait hints for non-existing traits
            foreach(var kv in traits)
            {
                var trait = kv.Value;
                foreach(var relation in trait.OptionalRelations)
                {
                    foreach(var traitHint in relation.RelationTemplate.TraitHints)
                    {
                        if (!traits.ContainsKey(traitHint))
                            issueAccumulator.TryAdd("trait-relation-hints", $"{trait.ID}-{relation.Identifier}-{traitHint}", $"Trait hint for trait relation \"{relation.Identifier}\" of trait \"{trait.ID}\" is invalid: trait \"{traitHint}\" does not exist");
                    }
                }
            }
            return true;
        }
    }
}
