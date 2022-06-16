﻿using Microsoft.Extensions.Logging;
using Omnikeeper.Base.CLB;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OKPluginCLBNaemonVariableResolution
{
    public class ValidatorThrukVSMonmanComparer : IValidator
    {
        private readonly GenericTraitEntityModel<ThrukHost, (string name, string peerKey)> thrukHostModel;
        private readonly GenericTraitEntityModel<Target, string> targetModel;

        public string Name => GetType().Name!;

        public ValidatorThrukVSMonmanComparer(GenericTraitEntityModel<ThrukHost, (string name, string peerKey)> thrukHostModel, GenericTraitEntityModel<Target, string> targetModel)
        {
            this.thrukHostModel = thrukHostModel;
            this.targetModel = targetModel;
        }

        private Config ParseConfig(JsonDocument configJson)
        {
            var tmpCfg = JsonSerializer.Deserialize<Config>(configJson, new JsonSerializerOptions());

            if (tmpCfg == null)
                throw new Exception("Could not parse configuration");
            return tmpCfg;
        }

        public async Task<bool> Run(IReadOnlyDictionary<string, IReadOnlyList<Changeset>?> unprocessedChangesets, JsonDocument config, IModelContextBuilder modelContextBuilder, TimeThreshold timeThreshold, ILogger logger, IIssueAccumulator issueAccumulator)
        {
            var cfg = ParseConfig(config);
            var thrukLayerset = new LayerSet(cfg.ThrukLayers);
            var monmanLayerset = new LayerSet(cfg.MonmanLayers);

            logger.LogDebug($"Comparing thruk hosts in layerset [{thrukLayerset}] with monitoring targets in layerset [{monmanLayerset}]");

            using var trans = modelContextBuilder.BuildImmediate();

            var thrukHosts = await thrukHostModel.GetByCIID(AllCIIDsSelection.Instance, thrukLayerset, trans, timeThreshold);
            var targets = await targetModel.GetByCIID(AllCIIDsSelection.Instance, monmanLayerset, trans, timeThreshold);

            var jsonComparer = new JsonElementComparer();

            logger.LogDebug($"Comparing {thrukHosts.Count} thruk hosts with {targets.Count} monitoring targets");

            foreach (var thrukHost in thrukHosts)
            {
                var cmdbCI = thrukHost.Value.CMDBCI;
                logger.LogTrace($"Looking at thruk host {thrukHost.Value.Name}");
                if (!cmdbCI.HasValue)
                {
                    issueAccumulator.TryAdd("cmdb_ci_not_set", thrukHost.Value.Name, "Thruk host is not associated with a CMDB CI", thrukHost.Key);
                    continue;
                } else if (!targets.TryGetValue(cmdbCI.Value, out var target))
                {
                    issueAccumulator.TryAdd("cmdb_ci_not_monman_target", thrukHost.Value.Name, $"Thruk host associated with a CMDB CI ({cmdbCI.Value}) that is not a monman target", thrukHost.Key);
                } else
                {
                    // compare resolved variables of monman with active variables of thruk host
                    var thrukVariables = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(thrukHost.Value.CustomVariables.RootElement)!.ToList();
                    // monman variables need to be transformed into simple key-value pairs before comparison
                    var monManVariables = new List<KeyValuePair<string, JsonElement>>();
                    foreach (var o in target.ResolvedVariables.RootElement.EnumerateObject())
                    {
                        var variableName = o.Name;
                        var variableValue = o.Value.GetProperty("value");
                        monManVariables.Add(new KeyValuePair<string, JsonElement>(variableName, variableValue));
                    }

                    try
                    {
                        var (variablesOnlyInThruk, variablesOnlyInMonman, differentVariables) = ValidatorThrukSiteComparer.MatchVariables(thrukVariables, monManVariables, jsonComparer);

                        foreach (var va in variablesOnlyInThruk)
                            issueAccumulator.TryAdd("variable_only_in_thruk", $"{va.Key}@{thrukHost.Value.Name}", $"Variable {va.Key} is only present on thruk host {thrukHost.Value.Name}, missing in monman", thrukHost.Key, cmdbCI.Value);
                        foreach (var vb in variablesOnlyInMonman)
                            issueAccumulator.TryAdd("variable_only_in_monman", $"{vb.Key}@{thrukHost.Value.Name}", $"Variable {vb.Key} is only present on monman target {target.ID}, missing in thruk", thrukHost.Key, cmdbCI.Value);
                        foreach (var (a, b) in differentVariables)
                            issueAccumulator.TryAdd("variable_value_different", $"{a.Key}@{thrukHost.Value.Name}", $"Variable {a.Key} on thruk host {thrukHost.Value.Name} is different from variable on monman target {target.ID} ({a.Value.GetRawText()} vs. {b.Value.GetRawText()})", thrukHost.Key, cmdbCI.Value);
                    }
                    catch (Exception e)
                    {
                        issueAccumulator.TryAdd("error_parsing_variables", thrukHost.Key.ToString(), $"Variables could not be parsed: {e.Message}", thrukHost.Key, cmdbCI.Value);
                    }

                }
            }

            return true;
        }

        public ISet<string> GetDependentLayerIDs(JsonDocument config, ILogger logger)
        {
            var cfg = ParseConfig(config);
            return cfg.MonmanLayers.Concat(cfg.ThrukLayers).ToHashSet();
        }

        public class Config
        {
            [JsonPropertyName("thruk_layers")]
            public string[] ThrukLayers { get; set; } = Array.Empty<string>();

            [JsonPropertyName("monman_layers")]
            public string[] MonmanLayers { get; set; } = Array.Empty<string>();
        }
    }
}
