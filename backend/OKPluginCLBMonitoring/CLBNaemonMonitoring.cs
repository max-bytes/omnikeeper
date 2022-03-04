using JsonSubTypes;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Omnikeeper.Base.AttributeValues;
using Omnikeeper.Base.CLB;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Templating;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OKPluginCLBMonitoring
{
    public class CLBNaemonMonitoring : CLBBase
    {
        private readonly IRelationModel relationModel;
        private readonly ICIModel ciModel;
        private readonly IAttributeModel attributeModel;
        private readonly ILayerModel layerModel;
        private readonly IEffectiveTraitModel traitModel;

        public CLBNaemonMonitoring(ICIModel ciModel, IAttributeModel attributeModel, ILayerModel layerModel, IEffectiveTraitModel traitModel, IRelationModel relationModel,
            ILatestLayerChangeModel latestLayerChangeModel) : base(latestLayerChangeModel)
        {
            this.ciModel = ciModel;
            this.attributeModel = attributeModel;
            this.layerModel = layerModel;
            this.relationModel = relationModel;
            this.traitModel = traitModel;
        }

        private readonly string hasMonitoringModulePredicate = "has_monitoring_module";
        private readonly string isMonitoredByPredicate = "is_monitored_by";
        private readonly string belongsToNaemonContactgroup = "belongs_to_naemon_contactgroup";

        private void LogError(Guid ciid, string name, string message)
        {
            // TODO
        }
        
        public override async Task<bool> Run(Layer targetLayer, JObject config, IChangesetProxy changesetProxy, IModelContext trans, ILogger logger)
        {
            logger.LogDebug("Start clbMonitoring");

            // TODO: make configurable
            var layerSetMonitoringDefinitionsOnly = await layerModel.BuildLayerSet(new[] { "Monitoring Definitions" }, trans);
            // TODO: make configurable
            var layerSetAll = await layerModel.BuildLayerSet(new[] { "CMDB", "Inventory Scan", "Monitoring Definitions" }, trans);


            var allHasMonitoringModuleRelations = await relationModel.GetMergedRelations(RelationSelectionWithPredicate.Build(hasMonitoringModulePredicate), layerSetMonitoringDefinitionsOnly, trans, changesetProxy.TimeThreshold, MaskHandlingForRetrievalGetMasks.Instance, GeneratedDataHandlingInclude.Instance);

            // prepare contact groups
            var cgr = new ContactgroupResolver(relationModel, ciModel, traitModel, logger, (Guid ciid, string name, string message) => LogError(ciid, name, message));
            await cgr.Setup(layerSetAll, belongsToNaemonContactgroup, Traits.ContactgroupFlattened, trans, changesetProxy.TimeThreshold);

            // prepare list of all monitored cis
            var monitoredCIIDs = allHasMonitoringModuleRelations.Select(r => r.Relation.FromCIID).ToHashSet();
            if (monitoredCIIDs.IsEmpty()) return true;
            var monitoredCIs = (await ciModel.GetMergedCIs(SpecificCIIDsSelection.Build(monitoredCIIDs), layerSetAll, true, AllAttributeSelection.Instance, trans, changesetProxy.TimeThreshold))
                .ToDictionary(ci => ci.ID);

            // prepare list of all monitoring modules
            var monitoringModuleCIIDs = allHasMonitoringModuleRelations.Select(r => r.Relation.ToCIID).ToHashSet();
            if (monitoringModuleCIIDs.IsEmpty()) return true;
            var monitoringModuleCIs = (await ciModel.GetMergedCIs(SpecificCIIDsSelection.Build(monitoringModuleCIIDs), layerSetMonitoringDefinitionsOnly, false, AllAttributeSelection.Instance, trans, changesetProxy.TimeThreshold))
                .ToDictionary(ci => ci.ID);


            logger.LogDebug("Prep");

            // find and parse commands, insert into monitored CIs
            var renderedTemplateSegments = new List<(Guid ciid, string? moduleName, string templateSegment)>();
            foreach (var p in allHasMonitoringModuleRelations)
            {
                logger.LogDebug("Process mm relation...");

                var monitoringModuleCI = monitoringModuleCIs[p.Relation.ToCIID];

                var monitoringModuleET = await traitModel.GetEffectiveTraitForCI(monitoringModuleCI, Traits.ModuleFlattened, layerSetMonitoringDefinitionsOnly, trans, changesetProxy.TimeThreshold);
                if (monitoringModuleET == null)
                {
                    logger.LogError($"Expected CI {monitoringModuleCI.ID} to have trait \"{Traits.ModuleFlattened.ID}\"");
                    LogError(monitoringModuleCI.ID, "error", $"Expected this CI to have trait \"{Traits.ModuleFlattened.ID}\"");
                    continue;
                }
                logger.LogDebug("  Fetched effective traits");
                var templateStr = (monitoringModuleET.TraitAttributes["template"].Attribute.Value as AttributeScalarValueText)?.Value;

                // create template context based on monitored CI, so that the templates can access all the related variables
                var context = ScribanVariableService.CreateComplexCIBasedTemplateContext(monitoredCIs[p.Relation.FromCIID], layerSetAll, changesetProxy.TimeThreshold, trans, ciModel, relationModel);

                logger.LogDebug("  Parse/Render config segments");
                // template parsing and rendering
                try
                {
                    logger.LogDebug($"  Parsing template:\n{templateStr}");

                    var template = Scriban.Template.Parse(templateStr);
                    string templateSegment = template.Render(context);
                    logger.LogDebug($"  Rendered template:\n{templateSegment}");
                    renderedTemplateSegments.Add((p.Relation.FromCIID, monitoringModuleCI.CIName, templateSegment));
                }
                catch (Exception e)
                {
                    logger.LogError($"Error parsing or rendering command from monitoring module \"{monitoringModuleCI.ID}\": {e.Message}");
                    LogError(monitoringModuleCI.ID, "error", $"Error parsing or rendering command: {e.Message}");
                }
                logger.LogDebug("  Processed mm relation");
            }

            var parseErrors = new List<(Guid ciid, string? template, string? error)>();
            IEnumerable<(Guid ciid, AttributeArrayValueJSON attributeValue, IEnumerable<NaemonHostTemplate> hostTemplates, IEnumerable<NaemonServiceTemplate> serviceTemplates)>? renderedTemplatesPerCI = renderedTemplateSegments.GroupBy(t => t.ciid)
                .Select(tt =>
                {
                    var fragments = tt.SelectMany(ttt =>
                    {
                        try
                        {
                            var r = JsonConvert.DeserializeObject<INaemonFragmentTemplate[]>(ttt.templateSegment);
                            if (r == null) return new INaemonFragmentTemplate[0];
                            return r;
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
                        var attributeValue = AttributeArrayValueJSON.BuildFromString(values);
                        return (ciid: tt.Key, attributeValue,
                            hostTemplates: fragments.Select(t => t as NaemonHostTemplate).WhereNotNull(),
                            serviceTemplates: fragments.Select(t => t as NaemonServiceTemplate).WhereNotNull());
                    }
                    catch (Exception e)
                    {
                        parseErrors.Add((ciid: tt.Key, string.Join(',', values), error: e.Message));
                        return default;
                    }
                }).Where(tt => tt.attributeValue != null).ToList();

            if (parseErrors.Count > 0)
            {
                foreach (var (ciid, commandStr, error) in parseErrors)
                {
                    logger.LogError($"Error parsing the following command fragment:\n{commandStr}\nError: {error}");
                    LogError(ciid, "error", $"Error parsing the following command fragment:\n{commandStr}\nError: {error}");
                }
            }

            // TODO: mask handling
            var maskHandling = MaskHandlingForRemovalApplyNoMask.Instance;
            var otherLayersValueHandling = OtherLayersValueHandlingForceWrite.Instance;

            var fragments = renderedTemplatesPerCI.Select(t => new BulkCIAttributeDataLayerScope.Fragment("", t.attributeValue, t.ciid));
            await attributeModel.BulkReplaceAttributes(new BulkCIAttributeDataLayerScope("naemon.intermediate_config", targetLayer.ID, fragments),
                changesetProxy, new DataOriginV1(DataOriginType.ComputeLayer), trans, maskHandling, otherLayersValueHandling);

            logger.LogDebug("Updated executed commands per monitored CI");

            // assign monitored cis to naemon instances
            var monitoredByCIIDFragments = new List<BulkRelationDataPredicateScope.Fragment>();
            var naemonInstancesCIs = await ciModel.GetMergedCIs(new AllCIIDsSelection(), layerSetAll, false, AllAttributeSelection.Instance, trans, changesetProxy.TimeThreshold); // TODO: reduce attributes to trait relevant
            var naemonInstancesTS = await traitModel.GetEffectiveTraitsForTrait(Traits.NaemonInstanceFlattened, naemonInstancesCIs, layerSetAll, trans, changesetProxy.TimeThreshold);
            foreach (var naemonInstanceTS in naemonInstancesTS)
                foreach (var monitoredCI in monitoredCIs.Values)
                    if (CanCIBeMonitoredByNaemonInstance(monitoredCI, naemonInstanceTS.Value))
                        monitoredByCIIDFragments.Add(new BulkRelationDataPredicateScope.Fragment(monitoredCI.ID, naemonInstanceTS.Key, false));
            await relationModel.BulkReplaceRelations(new BulkRelationDataPredicateScope(isMonitoredByPredicate, targetLayer.ID, monitoredByCIIDFragments.ToArray()), changesetProxy, new DataOriginV1(DataOriginType.ComputeLayer), trans, maskHandling, otherLayersValueHandling);
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
                        var hostTemplate = t.SelectMany(t => t.hostTemplates).FirstOrDefault();
                        // look up contactgroups for host
                        string[] hostContactgroups = new string[0];
                        if (hostTemplate != null)
                            hostContactgroups = cgr.CalculateContactgroupsOfCI(hostTemplate.ContactgroupSource).ToArray();
                        var naemonHost = new NaemonHost(monitoredCIs[t.Key].CIName ?? "", hostContactgroups,
                            t.Key,
                            // we pick the first host command we can find
                            hostTemplate?.Command.ToFullCommandString() ?? "",
                            // TODO, HACK: handle duplicates in description
                            t.SelectMany(t => t.serviceTemplates).ToDictionary(t => t.Description, t =>
                            {
                                return new NaemonService(
                                    t.Command.ToFullCommandString(),
                                    cgr.CalculateContactgroupsOfCI(t.ContactgroupSource).ToArray()
                                );
                            })
                        );
                        return naemonHost;
                    }).ToList();

                monitoringConfigs.Add(new BulkCIAttributeDataLayerScope.Fragment("", AttributeArrayValueJSON.BuildFromString(
                    naemonHosts.Select(t => JsonConvert.SerializeObject(t, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() })).ToArray()), naemonInstance));

                //var finalConfigYamlNode = new YamlMappingNode(
                //    templates.Select(t => (ciName: monitoredCIs[t.ciid].Name, t.yamlValue))
                //.GroupBy(t => t.ciName) // do this to work around duplicates in names, TODO: proper de-duplication
                //.Select(tt => (ciName: tt.Key, tt.FirstOrDefault().yamlValue))
                //.Select(t => KeyValuePair.Create<YamlNode, YamlNode>(new YamlScalarNode(t.ciName) { Style = ScalarStyle.DoubleQuoted }, t.yamlValue.Value.RootNode))
                //);
                //var finalConfig = new YamlDocument(finalConfigYamlNode);

                //monitoringConfigs.Add(new BulkCIAttributeDataLayerScope.Fragment("", AttributeValueYAMLArray.Build(
                //    templates.Select(t => t.yamlValue.Value).ToArray(), templates.Select(t => t.yamlValueStr).ToArray()), naemonInstance));
            }
            await attributeModel.BulkReplaceAttributes(new BulkCIAttributeDataLayerScope("naemon.config", targetLayer.ID, monitoringConfigs),
                changesetProxy, new DataOriginV1(DataOriginType.ComputeLayer), trans, MaskHandlingForRemovalApplyNoMask.Instance, OtherLayersValueHandlingForceWrite.Instance);

            logger.LogDebug("End clbMonitoring");
            return true;
        }

        private bool CanCIBeMonitoredByNaemonInstance(MergedCI monitoredCI, EffectiveTrait naemonInstanceET)
        {
            naemonInstanceET.TraitAttributes.TryGetValue("capabilities", out var naemonCapabilitiesA);
            naemonInstanceET.TraitAttributes.TryGetValue("requirements", out var naemonRequirementsA);
            // TODO: would be better to use a trait than accessing the attribute directly
            monitoredCI.MergedAttributes.TryGetValue("naemon.capabilities", out var ciCapabilitiesA);
            monitoredCI.MergedAttributes.TryGetValue("naemon.requirements", out var ciRequirementsA);
            var naemonCapabilities = naemonCapabilitiesA?.TryReadValueTextArray() ?? new string[0];
            var naemonRequirements = naemonRequirementsA?.TryReadValueTextArray() ?? new string[0];
            var ciCapabilities = ciCapabilitiesA?.TryReadValueTextArray() ?? new string[0];
            var ciRequirements = ciRequirementsA?.TryReadValueTextArray() ?? new string[0];

            if (!naemonRequirements.IsSubsetOf(ciCapabilities)) // ci must fulfill all of the naemon's requirements
                return false;
            if (!ciRequirements.IsSubsetOf(naemonCapabilities)) // naemon must fulfill all of the ci's requirements
                return false;

            return true;
        }

        private class ContactgroupResolver
        {
            private readonly IRelationModel relationModel;
            private readonly ICIModel ciModel;
            private readonly IEffectiveTraitModel traitModel;
            private readonly ILogger logger;
            private readonly Action<Guid, string, string> logErrorF;
            private Dictionary<Guid, IEnumerable<MergedCI>> contactGroupsMap = new Dictionary<Guid, IEnumerable<MergedCI>>();
            private readonly Dictionary<Guid, string> contactGroupNames = new Dictionary<Guid, string>();

            public ContactgroupResolver(IRelationModel relationModel, ICIModel ciModel, IEffectiveTraitModel traitModel, ILogger logger, Action<Guid, string, string> logErrorF)
            {
                this.relationModel = relationModel;
                this.ciModel = ciModel;
                this.traitModel = traitModel;
                this.logger = logger;
                this.logErrorF = logErrorF;
            }

            public async Task Setup(LayerSet layerSetAll, string belongsToNaemonContactgroup, ITrait contactgroupTrait, IModelContext trans, TimeThreshold timeThreshold)
            {
                var contactGroupRelations = await relationModel.GetMergedRelations(RelationSelectionWithPredicate.Build(belongsToNaemonContactgroup), layerSetAll, trans, timeThreshold, MaskHandlingForRetrievalGetMasks.Instance, GeneratedDataHandlingInclude.Instance);
                if (contactGroupRelations.IsEmpty())
                {
                    contactGroupsMap = new Dictionary<Guid, IEnumerable<MergedCI>>();
                }
                else
                {
                    var contactGroupCIs = (await ciModel.GetMergedCIs(SpecificCIIDsSelection.Build(contactGroupRelations.Select(r => r.Relation.ToCIID).ToHashSet()), layerSetAll, false, AllAttributeSelection.Instance, trans, timeThreshold)).ToDictionary(t => t.ID);
                    contactGroupsMap = contactGroupRelations.GroupBy(r => r.Relation.FromCIID).ToDictionary(t => t.Key, t => t.Select(tt => contactGroupCIs[tt.Relation.ToCIID]));
                    foreach (var ci in contactGroupsMap.Values.SelectMany(t => t).Distinct())
                    {
                        var et = await traitModel.GetEffectiveTraitForCI(ci, contactgroupTrait, layerSetAll, trans, timeThreshold);
                        if (et != null)
                        {
                            var name = (et.TraitAttributes["name"].Attribute.Value as AttributeScalarValueText)?.Value;
                            if (name != null)
                                contactGroupNames.Add(ci.ID, name);
                            else
                                logger.LogError($"Expected CI {ci.ID} with trait to have proper contactgroup name");

                        }
                        else
                        {
                            logger.LogError($"Expected CI {ci.ID} to have trait \"{contactgroupTrait.ID}\"");
                            logErrorF(ci.ID, "error", $"Expected this CI to have trait \"{contactgroupTrait.ID}\"");
                        }
                    }
                }
            }

            public IEnumerable<string> CalculateContactgroupsOfCI(Guid sourceCIID)
            {
                if (contactGroupsMap.TryGetValue(sourceCIID, out var ctCIs))
                {
                    foreach (var ctCI in ctCIs)
                    {
                        if (contactGroupNames.TryGetValue(ctCI.ID, out var name))
                            yield return name;
                        else
                            logger.LogError($"Could not find contactgroup-name of CI {ctCI.ID}. Is this CI actually a contactgroup?");
                    }
                } // else -> sourceCI has no contact groups associated
            }
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
        public NaemonServiceTemplate(Guid contactgroupSource, string description, NaemonCommandTemplate command)
        {
            ContactgroupSource = contactgroupSource;
            Description = description;
            Command = command;
        }

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
        public NaemonHostTemplate(Guid contactgroupSource, NaemonCommandTemplate command)
        {
            ContactgroupSource = contactgroupSource;
            Command = command;
        }
        [JsonProperty(Required = Required.Always)]
        public Guid ContactgroupSource { get; set; }
        [JsonProperty(Required = Required.Always)]
        public NaemonCommandTemplate Command { get; set; }
        public string type { get; } = "host";
    }

    internal class NaemonCommandTemplate
    {
        public NaemonCommandTemplate(string executable, string parameters)
        {
            Executable = executable;
            Parameters = parameters;
        }
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
        public NaemonHost(string name, string[] contactgroups, Guid iD, string command, IDictionary<string, NaemonService> services)
        {
            Name = name;
            Contactgroups = contactgroups;
            ID = iD;
            Command = command;
            Services = services;
        }
        public string Name { get; set; }
        public string[] Contactgroups { get; set; }
        public Guid ID { get; set; }
        public string Command { get; set; }
        public IDictionary<string, NaemonService> Services { get; set; }
    }

    internal class NaemonService
    {
        public NaemonService(string command, string[] contactgroups)
        {
            Command = command;
            Contactgroups = contactgroups;
        }

        public string Command { get; set; }
        public string[] Contactgroups { get; set; }
    }
}
