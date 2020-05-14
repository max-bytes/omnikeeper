using Scriban;
using Landscape.Base;
using Landscape.Base.Entity;
using Landscape.Base.Model;
using Landscape.Base.Templating;
using Landscape.Base.Utils;
using LandscapeRegistry.Entity.AttributeValues;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Scriban.Runtime;
using static Landscape.Base.Templating.ScribanVariableService;
using YamlDotNet.Core;
using System.IO;
using YamlDotNet.RepresentationModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using JsonSubTypes;

namespace MonitoringPlugin
{
    public class CLBNaemonMonitoring : CLBBase
    {
        private readonly IRelationModel relationModel;
        private readonly ICIModel ciModel;
        private readonly ITraitModel traitModel;

        public CLBNaemonMonitoring(ICIModel ciModel, IAttributeModel atributeModel, ILayerModel layerModel, ITraitModel traitModel, IRelationModel relationModel, IChangesetModel changesetModel, IUserInDatabaseModel userModel, NpgsqlConnection conn)
            : base(atributeModel, layerModel, changesetModel, userModel, conn)
        {
            this.ciModel = ciModel;
            this.relationModel = relationModel;
            this.traitModel = traitModel;
        }

        public override async Task<bool> Run(Layer targetLayer, IChangesetProxy changesetProxy, CLBErrorHandler errorHandler, NpgsqlTransaction trans, ILogger logger)
        {
            logger.LogDebug("Start clbMonitoring");
            var layerSetMonitoringDefinitionsOnly = await layerModel.BuildLayerSet(new[] { "Monitoring Definitions" }, trans);

            var timeThreshold = TimeThreshold.BuildLatest(); // TODO: can we really work with a single timethreshold?

            // TODO: make configurable
            var layerSetAll = await layerModel.BuildLayerSet(new[] { "CMDB", "Inventory Scan", "Monitoring Definitions" }, trans);

            var allHasMonitoringModuleRelations = await relationModel.GetMergedRelationsWithPredicateID(layerSetMonitoringDefinitionsOnly, false, "has_monitoring_module", trans, timeThreshold);

            // prepare list of all monitored cis
            var monitoredCIIDs = allHasMonitoringModuleRelations.Select(r => r.FromCIID).Distinct();
            var monitoredCIs = (await ciModel.GetMergedCIs(layerSetAll, true, trans, timeThreshold, monitoredCIIDs))
                .ToDictionary(ci => ci.ID);

            // prepare list of all monitoring modules
            var monitoringModuleCIIDs = allHasMonitoringModuleRelations.Select(r => r.ToCIID).Distinct();
            var monitoringModuleCIs = (await ciModel.GetMergedCIs(layerSetMonitoringDefinitionsOnly, false, trans, timeThreshold, monitoringModuleCIIDs))
                .ToDictionary(ci => ci.ID);

            logger.LogDebug("Prep");

            // find and parse commands, insert into monitored CIs
            var renderedTemplateSegments = new List<(Guid ciid, string moduleName, string templateSegment)>();
            foreach (var p in allHasMonitoringModuleRelations)
            {
                logger.LogDebug("Process mm relation...");

                var monitoringModuleCI = monitoringModuleCIs[p.ToCIID];
                var monitoringModuleET = await traitModel.CalculateEffectiveTraitSetForCI(monitoringModuleCI, trans, timeThreshold); // TODO: move outside of loop, prefetch
                if (!monitoringModuleET.EffectiveTraits.ContainsKey("naemon_service_module"))
                {
                    logger.LogError($"Expected CI {monitoringModuleCI.ID} to have trait \"naemon_service_module\"");
                    await errorHandler.LogError(monitoringModuleCI.ID, "error", "Expected this CI to have trait \"naemon_service_module\"");
                    continue;
                }
                logger.LogDebug("  Fetched effective traits");

                var monitoredCI = monitoredCIs[p.FromCIID];
                var monitoringCommandsAV = monitoringModuleET.EffectiveTraits["naemon_service_module"].TraitAttributes["config_template"].Attribute.Value;
                //var monitoringCommands = new string[0];
                var monitoringCommands = monitoringCommandsAV switch
                {
                    AttributeValueTextArray ata => ata.Values,
                    AttributeValueTextScalar ats => new string[] { ats.Value },
                    _ => new string[0], // TODO: error handling
                };

                // create template context based on monitored CI, so that the templates can access all the related variables
                var context = ScribanVariableService.CreateCIBasedTemplateContext(monitoredCI, layerSetAll, timeThreshold, null, ciModel, relationModel);

                logger.LogDebug("  Parse/Render config segments");
                // template parsing and rendering
                foreach (var templateStr in monitoringCommands)
                {
                    try
                    {
                        logger.LogDebug($"  Parsing template:\n{templateStr}");

                        var template = Scriban.Template.Parse(templateStr);
                        string templateSegment = template.Render(context);
                        logger.LogDebug($"  Rendered template:\n{templateSegment}");
                        renderedTemplateSegments.Add((p.FromCIID, monitoringModuleCI.Name, templateSegment));
                    }
                    catch (Exception e)
                    {
                        logger.LogError($"Error parsing or rendering command from monitoring module \"{monitoringModuleCI.ID}\": {e.Message}");
                        await errorHandler.LogError(monitoringModuleCI.ID, "error", $"Error parsing or rendering command: {e.Message}");
                        continue;
                    }
                }
                logger.LogDebug("Processed mm relation");
            }

            var parseErrors = new List<(Guid ciid, string template, string error)>();
            var renderedTemplatesPerCI = renderedTemplateSegments.GroupBy(t => t.ciid)
                .Select(tt =>
                {
                    var fragments = tt.SelectMany(ttt =>
                    {
                        try
                        {
                            return JsonConvert.DeserializeObject<INaemonFragmentTemplate[]>(ttt.templateSegment);
                        }
                        catch (Exception e)
                        {
                            parseErrors.Add((ciid: tt.Key, ttt.templateSegment, error: $"Could not parse service template: {e.Message}"));
                            return new INaemonFragmentTemplate[0];
                        }
                    }).Where(ttt => ttt != null).ToList();

                    var values = tt.Select(ttt => ttt.templateSegment).ToArray();
                    try
                    {
                        var attributeValue = AttributeValueJSONArray.Build(values);
                        return (ciid: tt.Key, attributeValue,
                            hostTemplates: fragments.Select(t => t as NaemonHostTemplate).Where(t => t != null),
                            serviceTemplates: fragments.Select(t => t as NaemonServiceTemplate).Where(t => t != null));
                    }
                    catch (Exception e)
                    {
                        parseErrors.Add((ciid: tt.Key, string.Join(',', values), error: e.Message));
                        return (ciid: tt.Key, null, hostTemplates: null, serviceTemplates: null);
                    }
                }).Where(tt => tt.attributeValue != null).ToList();

            if (parseErrors.Count > 0)
            {
                foreach (var (ciid, commandStr, error) in parseErrors)
                {
                    logger.LogError($"Error parsing the following command fragment:\n{commandStr}\nError: {error}");
                    await errorHandler.LogError(ciid, "error", $"Error parsing the following command fragment:\n{commandStr}\nError: {error}");
                }
            }
            
            var fragments = renderedTemplatesPerCI.Select(t => BulkCIAttributeDataLayerScope.Fragment.Build("", t.attributeValue, t.ciid));
            await attributeModel.BulkReplaceAttributes(BulkCIAttributeDataLayerScope.Build("monitoring.naemon.rendered_config", targetLayer.ID, fragments), changesetProxy, trans);

            logger.LogDebug("Updated executed commands per monitored CI");

            // assign monitored cis to naemon instances
            var monitoredByCIIDFragments = new List<BulkRelationDataPredicateScope.Fragment>();
            var naemonInstancesTS = await traitModel.CalculateEffectiveTraitSetsForTraitName("naemon_instance", layerSetAll, trans, timeThreshold);
            foreach (var naemonInstance in naemonInstancesTS)
                foreach (var monitoredCI in monitoredCIs)
                    monitoredByCIIDFragments.Add(BulkRelationDataPredicateScope.Fragment.Build(monitoredCI.Value.ID, naemonInstance.UnderlyingCI.ID));
            await relationModel.BulkReplaceRelations(BulkRelationDataPredicateScope.Build("is_monitored_by", targetLayer.ID, monitoredByCIIDFragments.ToArray()), changesetProxy, trans);

            logger.LogDebug("Assigned CIs to naemon instances");

            logger.LogDebug("Writing final naemon config");

            // write final naemon config
            var naemonInstance2MonitoredCILookup = monitoredByCIIDFragments.GroupBy(t => t.To).ToDictionary(t => t.Key, t => t.Select(t => t.From));
            var monitoringConfigs = new List<BulkCIAttributeDataLayerScope.Fragment>();
            foreach (var kv in naemonInstance2MonitoredCILookup)
            {
                var naemonInstance = kv.Key;
                var cis = kv.Value;
                var templates = renderedTemplatesPerCI.Where(f => cis.Contains(f.ciid));

                // convert templates to naemon hosts, ready to serialize into json
                var naemonHosts = templates.GroupBy(t => t.ciid)
                    .Select(t =>
                    {
                        var naemonHost = new NaemonHost()
                        {
                            Name = monitoredCIs[t.Key].Name,
                            ID = t.Key,
                            // we pick the first host command we can find
                            Command = t.SelectMany(t => t.hostTemplates).FirstOrDefault()?.Command.ToFullCommandString() ?? "",
                            // TODO, HACK: handle duplicates in description
                            Services = t.SelectMany(t => t.serviceTemplates).ToDictionary(t => t.Description, t => new NaemonService() { Command = t.Command.ToFullCommandString() })//new Dictionary<string, NaemonService>()
                        };
                        return naemonHost;
                    });

                monitoringConfigs.Add(BulkCIAttributeDataLayerScope.Fragment.Build("", AttributeValueJSONArray.Build(
                    naemonHosts.Select(t => JsonConvert.SerializeObject(t, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() })).ToArray()), naemonInstance));

                //var finalConfigYamlNode = new YamlMappingNode(
                //    templates.Select(t => (ciName: monitoredCIs[t.ciid].Name, t.yamlValue))
                //.GroupBy(t => t.ciName) // do this to work around duplicates in names, TODO: proper de-duplication
                //.Select(tt => (ciName: tt.Key, tt.FirstOrDefault().yamlValue))
                //.Select(t => KeyValuePair.Create<YamlNode, YamlNode>(new YamlScalarNode(t.ciName) { Style = ScalarStyle.DoubleQuoted }, t.yamlValue.Value.RootNode))
                //);
                //var finalConfig = new YamlDocument(finalConfigYamlNode);

                //monitoringConfigs.Add(BulkCIAttributeDataLayerScope.Fragment.Build("", AttributeValueYAMLArray.Build(
                //    templates.Select(t => t.yamlValue.Value).ToArray(), templates.Select(t => t.yamlValueStr).ToArray()), naemonInstance));
            }
            await attributeModel.BulkReplaceAttributes(BulkCIAttributeDataLayerScope.Build("monitoring.naemon.config", targetLayer.ID, monitoringConfigs), changesetProxy, trans);

            logger.LogDebug("End clbMonitoring");
            return true;
        }
    }

    [JsonConverter(typeof(JsonSubtypes), "type")]
    [JsonSubtypes.KnownSubType(typeof(NaemonServiceTemplate), "service")]
    [JsonSubtypes.KnownSubType(typeof(NaemonHostTemplate), "host")]
    internal interface INaemonFragmentTemplate
    {
        public string type { get; }
    }

    internal class NaemonServiceTemplate : INaemonFragmentTemplate
    {
        [JsonProperty(Required = Required.Always)]
        public string Description { get; set; }
        [JsonProperty(Required = Required.Always)]
        public Guid ContactgroupSource { get; set; }
        [JsonProperty(Required = Required.Always)]
        public NaemonCommandTemplate Command { get; set; }
        public string type { get; } = "service";
    }
    internal class NaemonHostTemplate : INaemonFragmentTemplate
    {
        [JsonProperty(Required = Required.Always)]
        public NaemonCommandTemplate Command { get; set; }
        public string type { get; } = "host";
    }

    internal class NaemonCommandTemplate
    {
        [JsonProperty(Required = Required.Always)]
        public string Executable { get; set; }
        public string Parameters { get; set; }

        public string ToFullCommandString()
        {
            return $"{Executable} {Parameters}";
        }
    }

    internal class NaemonHost
    {
        public string Name { get; set; }
        public Guid ID { get; set; }
        public string Command { get; set; }
        public IDictionary<string, NaemonService> Services { get; set; }
    }

    internal class NaemonService
    {
        public string Command { get; set; }
    }
}
