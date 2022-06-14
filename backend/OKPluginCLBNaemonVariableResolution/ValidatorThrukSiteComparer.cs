using Microsoft.Extensions.Logging;
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
    public class ValidatorThrukSiteComparer : IValidator
    {
        private readonly GenericTraitEntityModel<ThrukHost, (string name, string peerKey)> thrukHostModel;
        private readonly GenericTraitEntityModel<ThrukService, (string name, string peerKey, string description)> thrukServiceModel;

        public string Name => GetType().Name!;

        public ValidatorThrukSiteComparer(GenericTraitEntityModel<ThrukHost, (string name, string peerKey)> thrukHostModel, GenericTraitEntityModel<ThrukService, (string name, string peerKey, string description)> thrukServiceModel)
        {
            this.thrukHostModel = thrukHostModel;
            this.thrukServiceModel = thrukServiceModel;
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
            var sourceLayerSet = new LayerSet(cfg.SourceLayers);

            using var trans = modelContextBuilder.BuildImmediate();

            var thrukHosts = await thrukHostModel.GetByCIID(AllCIIDsSelection.Instance, sourceLayerSet, trans, timeThreshold);
            var thrukServices = await thrukServiceModel.GetByCIID(AllCIIDsSelection.Instance, sourceLayerSet, trans, timeThreshold);

            foreach (var sitePair in cfg.Pairs)
            {
                var peerKeyA = sitePair.A;
                var peerKeyB = sitePair.B;

                logger.LogDebug($"Comparing thruk sites {peerKeyA} and {peerKeyB}");

                var thrukHostsA = thrukHosts.Where(kv => kv.Value.PeerKey == peerKeyA).ToList();
                var thrukHostsB = thrukHosts.Where(kv => kv.Value.PeerKey == peerKeyB).ToList();

                // match hosts in A and B
                var matchedHosts = new List<(KeyValuePair<Guid, ThrukHost> a, KeyValuePair<Guid, ThrukHost> b)>();
                for (int i = thrukHostsA.Count - 1; i >= 0; i--)
                {
                    var thrukHostA = thrukHostsA[i];
                    for (int j = thrukHostsB.Count - 1; j >= 0; j--)
                    {
                        var thrukHostB = thrukHostsB[j];
                        if (thrukHostA.Value.Name == thrukHostB.Value.Name)
                        {
                            matchedHosts.Add((thrukHostA, thrukHostB));
                            thrukHostsA.RemoveAt(i);
                            thrukHostsB.RemoveAt(j);
                        }
                    }
                }

                // create issues for remaining hosts in A and B
                foreach (var thrukHostA in thrukHostsA)
                    issueAccumulator.TryAdd("host_only_in_one_site", thrukHostA.Value.Name, $"Thruk host is only present in site {peerKeyA}, missing in site {peerKeyB}", thrukHostA.Key);
                foreach (var thrukHostB in thrukHostsB)
                    issueAccumulator.TryAdd("host_only_in_one_site", thrukHostB.Value.Name, $"Thruk host is only present in site {peerKeyB}, missing in site {peerKeyA}", thrukHostB.Key);

                // compare matching hosts, check for differences
                var jsonComparer = new JsonElementComparer();
                foreach (var (a, b) in matchedHosts)
                {
                    // compare custom variables
                    CompareVariables(a, b, peerKeyA, peerKeyB, issueAccumulator, jsonComparer);

                    // compare CMDB-CI
                    if (a.Value.CMDBCI != b.Value.CMDBCI)
                        issueAccumulator.TryAdd("host_cmdb_ci_different", a.Value.Name, $"Thruk host's related cmdb-CIs are different for site {peerKeyB} and site {peerKeyA}", a.Key, b.Key);

                    // compare services
                    var servicesA = a.Value.Services.Select(ciid => new KeyValuePair<Guid, ThrukService>(ciid, thrukServices[ciid])).ToList();
                    var servicesB = b.Value.Services.Select(ciid => new KeyValuePair<Guid, ThrukService>(ciid, thrukServices[ciid])).ToList();
                    for (int i = servicesA.Count - 1; i >= 0; i--)
                    {
                        var serviceA = servicesA[i];
                        for (int j = servicesB.Count - 1; j >= 0; j--)
                        {
                            var serviceB = servicesB[j];
                            if (serviceA.Value.Description == serviceB.Value.Description)
                            { // NOTE: we treat services on the same host in two sites the same if their descriptions match
                                servicesA.RemoveAt(i);
                                servicesB.RemoveAt(j);
                            }
                        }
                    }
                    // remaining services don't have a match -> report
                    foreach (var serviceA in servicesA)
                        issueAccumulator.TryAdd("service_only_in_one_site", $"{serviceA.Value.Description}@{serviceA.Value.HostName}", $"Thruk service is only present in site {peerKeyA}, missing in site {peerKeyB}", serviceA.Key);
                    foreach (var serviceB in servicesB)
                        issueAccumulator.TryAdd("service_only_in_one_site", $"{serviceB.Value.Description}@{serviceB.Value.HostName}", $"Thruk service is only present in site {peerKeyB}, missing in site {peerKeyA}", serviceB.Key);
                }
            }

            return true;
        }

        private void CompareVariables(KeyValuePair<Guid, ThrukHost> a, KeyValuePair<Guid, ThrukHost> b, string peerKeyA, string peerKeyB, IIssueAccumulator issueAccumulator, JsonElementComparer jsonComparer)
        {
            //if (!jsonComparer.Equals(a.Value.CustomVariables.RootElement, b.Value.CustomVariables.RootElement))
            //    issueAccumulator.TryAdd("host_custom_variables_different", a.Value.Name, $"Thruk host custom variables are different for site {peerKeyB} and site {peerKeyA}", a.Key, b.Key);
            try
            {
                var customVariablesA = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(a.Value.CustomVariables.RootElement)!.ToList();
                var customVariablesB = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(b.Value.CustomVariables.RootElement)!.ToList();
                var (variablesOnlyInA, variablesOnlyInB, differentVariables) = MatchVariables(customVariablesA, customVariablesB, jsonComparer);

                foreach (var va in variablesOnlyInA)
                    issueAccumulator.TryAdd("variable_only_in_one_site", $"{va.Key}@{a.Value.Name}", $"Custom variable {va.Key} on host {a.Value.Name} is only present in site {peerKeyA}, missing in site {peerKeyB}", a.Key, b.Key);
                foreach (var vb in variablesOnlyInB)
                    issueAccumulator.TryAdd("variable_only_in_one_site", $"{vb.Key}@{b.Value.Name}", $"Custom variable {vb.Key} on host {a.Value.Name} is only present in site {peerKeyB}, missing in site {peerKeyA}", a.Key, b.Key);
                foreach (var d in differentVariables)
                        issueAccumulator.TryAdd("variable_value_different", d.a.Key, $"Custom variable {d.a.Key} on host {a.Value.Name} is different between site {peerKeyA} and {peerKeyB} (\"{d.a.Value.GetRawText()}\" vs. \"{d.b.Value.GetRawText()}\")", a.Key, b.Key);
            }
            catch (Exception e)
            {
                issueAccumulator.TryAdd("error_parsing_custom_variables", a.Value.Name, $"Thruk host custom variables could not be parsed: {e.Message}", a.Key, b.Key);
            }
        }

        public static (IList<KeyValuePair<string, JsonElement>> variablesOnlyInA, IList<KeyValuePair<string, JsonElement>> variablesOnlyInB, IEnumerable<(KeyValuePair<string, JsonElement> a, KeyValuePair<string, JsonElement> b)> differentVariables) 
            MatchVariables(IList<KeyValuePair<string, JsonElement>> customVariablesA, IList<KeyValuePair<string, JsonElement>> customVariablesB, JsonElementComparer jsonElementComparer)
        {
            var matchedVariables = new List<(KeyValuePair<string, JsonElement> a, KeyValuePair<string, JsonElement> b)>();
            for (int i = customVariablesA.Count - 1; i >= 0; i--)
            {
                var va = customVariablesA[i];
                for (int j = customVariablesB.Count - 1; j >= 0; j--)
                {
                    var vb = customVariablesB[j];
                    if (va.Key == vb.Key)
                    {
                        customVariablesA.RemoveAt(i);
                        customVariablesB.RemoveAt(j);
                        matchedVariables.Add((va, vb));
                    }
                }
            }

            var differentVariables = matchedVariables.Where(t => !jsonElementComparer.Equals(t.a.Value, t.b.Value));
                return (customVariablesA, customVariablesB, differentVariables);
        }

        public ISet<string> GetDependentLayerIDs(JsonDocument config, ILogger logger)
        {
            var cfg = ParseConfig(config);
            return cfg.SourceLayers.ToHashSet();
        }
    }

    public class SitePair
    {
        [JsonPropertyName("a")]
        public string A { get; set; } = "";

        [JsonPropertyName("b")]
        public string B { get; set; } = "";
    }

    public class Config
    {
        [JsonPropertyName("source_layers")]
        public string[] SourceLayers { get; set; } = Array.Empty<string>();

        [JsonPropertyName("pairs")]
        public SitePair[] Pairs { get; set; } = Array.Empty<SitePair>();
    }
}
