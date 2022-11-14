using Microsoft.Extensions.Logging;
using Omnikeeper.Base.CLB;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.GraphQL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Omnikeeper.Validators
{
    public class ValidatorCIsShareEffectiveTrait : IValidator
    {
        private readonly ITraitsHolder traitsHolder;
        private readonly IEffectiveTraitModel effectiveTraitModel;
        private readonly ICIModel ciModel;

        public ValidatorCIsShareEffectiveTrait(ITraitsHolder traitsHolder, IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel)
        {
            this.traitsHolder = traitsHolder;
            this.effectiveTraitModel = effectiveTraitModel;
            this.ciModel = ciModel;
        }

        public string Name => GetType().Name!;

        private Configuration ParseConfig(JsonDocument configJson)
        {
            var tmpCfg = JsonSerializer.Deserialize<Configuration>(configJson, new JsonSerializerOptions());

            if (tmpCfg == null)
                throw new Exception("Could not parse configuration");
            return tmpCfg;
        }

        public Task<ISet<string>> GetDependentLayerIDs(JsonDocument config, ILogger logger, IModelContextBuilder modelContextBuilder)
        {
            var cfg = ParseConfig(config);
            return Task.FromResult<ISet<string>>(cfg.LayerSet.ToHashSet());
        }

        public async Task<bool> Run(IReadOnlyDictionary<string, IReadOnlyList<Changeset>?> unprocessedChangesets, JsonDocument config, IModelContextBuilder modelContextBuilder, TimeThreshold timeThreshold, ILogger logger, IIssueAccumulator issueAccumulator)
        {
            var cfg = ParseConfig(config);

            using var trans = modelContextBuilder.BuildImmediate();

            if (cfg.TraitA == cfg.TraitB)
            {
                issueAccumulator.TryAdd("config", cfg.TraitA, $"TraitA and TraitB must be different");
                return false;
            }
            var traitA = traitsHolder.GetTrait(cfg.TraitA);
            if (traitA == null)
            {
                issueAccumulator.TryAdd("config", cfg.TraitA, $"Couldn't find traitA with ID {cfg.TraitA}");
                return false;
            }
            var traitB = traitsHolder.GetTrait(cfg.TraitB);
            if (traitB == null)
            {
                issueAccumulator.TryAdd("config", cfg.TraitB, $"Couldn't find traitB with ID {cfg.TraitB}");
                return false;
            }

            var layerSet = new LayerSet(cfg.LayerSet);

            var relevantAttributesForTraits = traitA.GetRelevantAttributeNames()
                .Union(traitB.GetRelevantAttributeNames())
                .Union(new string[] { ICIModel.NameAttribute })
                .ToHashSet();

            var cis = await ciModel.GetMergedCIs(AllCIIDsSelection.Instance, layerSet, false, NamedAttributesSelection.Build(relevantAttributesForTraits), trans, timeThreshold);
            var cisWithTraitA = await effectiveTraitModel.GetEffectiveTraitsForTrait(traitA, cis, layerSet, trans, timeThreshold);
            var cisWithTraitB = await effectiveTraitModel.GetEffectiveTraitsForTrait(traitB, cis, layerSet, trans, timeThreshold);

            var ciNameDictionary = cis.ToDictionary(ci => ci.ID, ci => ci.CIName);

            var inAButNotInB = cisWithTraitA.Keys.Except(cisWithTraitB.Keys);
            var inBButNotInA = cisWithTraitB.Keys.Except(cisWithTraitA.Keys);
            foreach(var x in inAButNotInB)
                issueAccumulator.TryAdd($"missing-trait-{traitB.ID}", x.ToString(), $"CI \"{ciNameDictionary.GetValueOrDefault(x, () => "[UNNAMED]")}\" has trait {traitA.ID}, but not trait {traitB.ID}", x);
            foreach (var x in inBButNotInA)
                issueAccumulator.TryAdd($"missing-trait-{traitA.ID}", x.ToString(), $"CI \"{ciNameDictionary.GetValueOrDefault(x, () => "[UNNAMED]")}\" has trait {traitB.ID}, but not trait {traitA.ID}", x);

            return true;
        }


        public class Configuration
        {
            [JsonPropertyName("layerset")]
            public List<string> LayerSet { get; set; } = new List<string>();
            [JsonPropertyName("trait_a")]
            public string TraitA { get; set; } = "";
            [JsonPropertyName("trait_b")]
            public string TraitB { get; set; } = "";
        }
    }
}
