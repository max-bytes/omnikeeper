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
using DotLiquid.Util;
using System.Reflection;

namespace MonitoringPlugin
{
    public class CLBNaemonMonitoring : CLBBase
    {
        private readonly IRelationModel relationModel;
        private readonly ICIModel ciModel;
        private readonly ITraitModel traitModel;

        public CLBNaemonMonitoring(ICIModel ciModel, IAttributeModel atributeModel, ILayerModel layerModel, ITraitModel traitModel, IRelationModel relationModel,
            IPredicateModel predicateModel, IChangesetModel changesetModel, IUserInDatabaseModel userModel, NpgsqlConnection conn)
            : base(atributeModel, layerModel, predicateModel, changesetModel, userModel, conn)
        {
            this.ciModel = ciModel;
            this.relationModel = relationModel;
            this.traitModel = traitModel;
        }

        private readonly string hasMonitoringModulePredicate = "has_monitoring_module";
        private readonly string isMonitoredByPredicate = "is_monitored_by";
        private readonly string belongsToNaemonContactgroup = "belongs_to_naemon_contactgroup";
        public override string[] RequiredPredicates => new string[]
        {
            hasMonitoringModulePredicate,
            isMonitoredByPredicate,
            belongsToNaemonContactgroup
        };

        private readonly Trait moduleTrait = Trait.Build("naemon_service_module", new List<TraitAttribute>() {
            TraitAttribute.Build("template",
                CIAttributeTemplate.BuildFromParams("monitoring.naemon.config_template", AttributeValueType.MultilineText, null, CIAttributeValueConstraintTextLength.Build(1, null))
            )
        });
        private readonly Trait naemonInstanceTrait = Trait.Build("naemon_instance", new List<TraitAttribute>() {
            TraitAttribute.Build("name",
                CIAttributeTemplate.BuildFromParams("monitoring.naemon.instance_name", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
            )
        });
        private readonly Trait contactgroupTrait = Trait.Build("naemon_contactgroup", new List<TraitAttribute>() {
            TraitAttribute.Build("name",
                CIAttributeTemplate.BuildFromParams("monitoring.naemon.contactgroup_name", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
            )
        });

        public override Trait[] DefinedTraits => new Trait[] { moduleTrait, naemonInstanceTrait, contactgroupTrait };

        public override async Task<bool> Run(Layer targetLayer, IChangesetProxy changesetProxy, CLBErrorHandler errorHandler, NpgsqlTransaction trans, ILogger logger)
        {
            logger.LogDebug("Start clbMonitoring");
            var layerSetMonitoringDefinitionsOnly = await layerModel.BuildLayerSet(new[] { "Monitoring Definitions" }, trans);

            var timeThreshold = TimeThreshold.BuildLatestAtTime(changesetProxy.Timestamp);

            // TODO: make configurable
            var layerSetAll = await layerModel.BuildLayerSet(new[] { "CMDB", "Inventory Scan", "Monitoring Definitions" }, trans);

            var allHasMonitoringModuleRelations = await relationModel.GetMergedRelationsWithPredicateID(layerSetMonitoringDefinitionsOnly, false, hasMonitoringModulePredicate, trans, timeThreshold);

            // prepare contact groups
            var contactGroupRelations = await relationModel.GetMergedRelationsWithPredicateID(layerSetMonitoringDefinitionsOnly, false, belongsToNaemonContactgroup, trans, timeThreshold);

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

                var monitoringModuleET = await traitModel.CalculateEffectiveTraitForCI(monitoringModuleCI, moduleTrait, trans, timeThreshold);
                if (monitoringModuleET == null)
                {
                    logger.LogError($"Expected CI {monitoringModuleCI.ID} to have trait \"{moduleTrait.Name}\"");
                    await errorHandler.LogError(monitoringModuleCI.ID, "error", $"Expected this CI to have trait \"{moduleTrait.Name}\"");
                    continue;
                }
                logger.LogDebug("  Fetched effective traits");
                var templateStr = (monitoringModuleET.TraitAttributes["template"].Attribute.Value as AttributeValueTextScalar).Value;

                // create template context based on monitored CI, so that the templates can access all the related variables
                var context = ScribanVariableService.CreateCIBasedTemplateContext(monitoredCIs[p.FromCIID], layerSetAll, timeThreshold, null, ciModel, relationModel);

                logger.LogDebug("  Parse/Render config segments");
                // template parsing and rendering
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
                }
                logger.LogDebug("  Processed mm relation");
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
            await relationModel.BulkReplaceRelations(BulkRelationDataPredicateScope.Build(isMonitoredByPredicate, targetLayer.ID, monitoredByCIIDFragments.ToArray()), changesetProxy, trans);
            logger.LogDebug("Assigned CIs to naemon instances");


            logger.LogDebug("Writing final naemon config");

            // write final naemon config
            var naemonInstance2MonitoredCILookup = monitoredByCIIDFragments.GroupBy(t => t.To).ToDictionary(t => t.Key, t => t.Select(t => t.From));
            var monitoringConfigs = new List<BulkCIAttributeDataLayerScope.Fragment>();

            // TODO: this needs to be made much more straightforward!
            var contactGroupCIIDs = contactGroupRelations.Select(r => r.ToCIID);
            var contactGroupCIs = (await ciModel.GetMergedCIs(layerSetAll, false, trans, timeThreshold, contactGroupCIIDs)).ToDictionary(t => t.ID);
            var contactGroups = contactGroupRelations.GroupBy(r => r.FromCIID).ToDictionary(t => t.Key, t => t.Select(tt => contactGroupCIs[tt.ToCIID]));
            var contactGroupNames = new Dictionary<Guid, string>();
            foreach(var ci in contactGroups.Values.SelectMany(t => t).Distinct())
            {
                var et = await traitModel.CalculateEffectiveTraitForCI(ci, contactgroupTrait, trans, timeThreshold);
                if (et != null)
                {
                    var name = (et.TraitAttributes["name"].Attribute.Value as AttributeValueTextScalar).Value;
                    contactGroupNames.Add(ci.ID, name);
                }
            }

            foreach (var kv in naemonInstance2MonitoredCILookup)
            {
                var naemonInstance = kv.Key;
                var cis = kv.Value;
                var templates = renderedTemplatesPerCI.Where(f => cis.Contains(f.ciid));

                // convert templates to naemon hosts, ready to serialize into json
                var naemonHosts = templates.GroupBy(t => t.ciid)
                    .Select(t =>
                    {
                        var hostTemplate = t.SelectMany(t => t.hostTemplates).FirstOrDefault();
                        // look up contactgroups for host
                        IEnumerable<string> cts = new string[0];
                        if (hostTemplate != null && contactGroups.TryGetValue(hostTemplate.ContactgroupSource, out var ctCIs))
                            cts = ctCIs.Select(ctCI => contactGroupNames[ctCI.ID]);
                        var naemonHost = new NaemonHost()
                        {
                            Name = monitoredCIs[t.Key].Name,
                            ID = t.Key,
                            Contactgroups = cts.ToArray(),
                            // we pick the first host command we can find
                            Command = hostTemplate?.Command.ToFullCommandString() ?? "",
                            // TODO, HACK: handle duplicates in description
                            Services = t.SelectMany(t => t.serviceTemplates).ToDictionary(t => t.Description, t =>
                            {
                                // look up contactgroups for service
                                IEnumerable<string> cts = new string[0];
                                if (hostTemplate != null && contactGroups.TryGetValue(t.ContactgroupSource, out var ctCIs))
                                    cts = ctCIs.Select(ctCI => contactGroupNames[ctCI.ID]);
                                return new NaemonService() { 
                                    Command = t.Command.ToFullCommandString(),
                                    Contactgroups = cts.ToArray()
                                };
                            })
                        };
                        return naemonHost;
                    }).ToList();

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
        public Guid ContactgroupSource { get; set; }
        [JsonProperty(Required = Required.Always)]
        public string Description { get; set; }
        [JsonProperty(Required = Required.Always)]
        public NaemonCommandTemplate Command { get; set; }
        public string type { get; } = "service";
    }
    internal class NaemonHostTemplate : INaemonFragmentTemplate
    {
        [JsonProperty(Required = Required.Always)]
        public Guid ContactgroupSource { get; set; }
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
        public string[] Contactgroups { get; set; }
        public Guid ID { get; set; }
        public string Command { get; set; }
        public IDictionary<string, NaemonService> Services { get; set; }
    }

    internal class NaemonService
    {
        public string Command { get; set; }
        public string[] Contactgroups { get; set; }
    }
}
