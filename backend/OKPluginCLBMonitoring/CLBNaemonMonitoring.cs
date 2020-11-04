using Scriban;
using Omnikeeper.Base;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Templating;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Scriban.Runtime;
using static Omnikeeper.Base.Templating.ScribanVariableService;
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
using Omnikeeper.Base.AttributeValues;
using Omnikeeper.Base.CLB;

namespace OKPluginCLBMonitoring
{
    public class CLBNaemonMonitoring : CLBBase
    {
        private readonly IRelationModel relationModel;
        private readonly ICIModel ciModel;
        private readonly IEffectiveTraitModel traitModel;

        public CLBNaemonMonitoring(ICIModel ciModel, IBaseAttributeModel atributeModel, ILayerModel layerModel, IEffectiveTraitModel traitModel, IRelationModel relationModel,
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

        private readonly RecursiveTrait moduleRecursiveTrait = RecursiveTrait.Build("naemon_service_module", new List<TraitAttribute>() {
            TraitAttribute.Build("template",
                CIAttributeTemplate.BuildFromParams("naemon.config_template", AttributeValueType.MultilineText, null, CIAttributeValueConstraintTextLength.Build(1, null))
            )
        });

        private readonly RecursiveTrait naemonInstanceRecursiveTrait = RecursiveTrait.Build("naemon_instance", new List<TraitAttribute>() {
            TraitAttribute.Build("name",
                CIAttributeTemplate.BuildFromParams("naemon.instance_name", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
            )
        }, optionalAttributes: new List<TraitAttribute>()
        {
            TraitAttribute.Build("config",
                CIAttributeTemplate.BuildFromParams("naemon.config", AttributeValueType.JSON, true)
            ),
            TraitAttribute.Build("requirements", 
                CIAttributeTemplate.BuildFromParams("naemon.requirements", AttributeValueType.Text, true, CIAttributeValueConstraintTextLength.Build(1, null))
            ),
            TraitAttribute.Build("capabilities",
                CIAttributeTemplate.BuildFromParams("naemon.capabilities", AttributeValueType.Text, true, CIAttributeValueConstraintTextLength.Build(1, null))
            )
        });

        private readonly RecursiveTrait contactgroupRecursiveTrait = RecursiveTrait.Build("naemon_contactgroup", new List<TraitAttribute>() {
            TraitAttribute.Build("name",
                CIAttributeTemplate.BuildFromParams("naemon.contactgroup_name", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
            )
        });

        public override RecursiveTraitSet DefinedTraits => RecursiveTraitSet.Build(moduleRecursiveTrait, naemonInstanceRecursiveTrait, contactgroupRecursiveTrait);

        public override async Task<bool> Run(Layer targetLayer, IChangesetProxy changesetProxy, CLBErrorHandler errorHandler, NpgsqlTransaction trans, ILogger logger)
        {
            logger.LogDebug("Start clbMonitoring");

            // TODO: make configurable
            var layerSetMonitoringDefinitionsOnly = await layerModel.BuildLayerSet(new[] { "Monitoring Definitions" }, trans);
            // TODO: make configurable
            var layerSetAll = await layerModel.BuildLayerSet(new[] { "CMDB", "Inventory Scan", "Monitoring Definitions" }, trans);

            var naemonInstanceTrait = TraitSet.Traits["naemon_instance"];
            var contactgroupTrait = TraitSet.Traits["naemon_contactgroup"];
            var moduleTrait = TraitSet.Traits["naemon_service_module"];

            var timeThreshold = TimeThreshold.BuildLatestAtTime(changesetProxy.Timestamp);

            var allHasMonitoringModuleRelations = await relationModel.GetMergedRelations(new RelationSelectionWithPredicate(hasMonitoringModulePredicate), layerSetMonitoringDefinitionsOnly, trans, timeThreshold);

            // prepare contact groups
            var cgr = new ContactgroupResolver(relationModel, ciModel, traitModel, logger, errorHandler);
            await cgr.Setup(layerSetAll, belongsToNaemonContactgroup, contactgroupTrait, trans, timeThreshold);

            // prepare list of all monitored cis
            var monitoredCIIDs = allHasMonitoringModuleRelations.Select(r => r.Relation.FromCIID).Distinct();
            if (monitoredCIIDs.IsEmpty()) return true;
            var monitoredCIs = (await ciModel.GetMergedCIs(SpecificCIIDsSelection.Build(monitoredCIIDs), layerSetAll, true, trans, timeThreshold))
                .ToDictionary(ci => ci.ID);

            // prepare list of all monitoring modules
            var monitoringModuleCIIDs = allHasMonitoringModuleRelations.Select(r => r.Relation.ToCIID).Distinct();
            if (monitoringModuleCIIDs.IsEmpty()) return true;
            var monitoringModuleCIs = (await ciModel.GetMergedCIs(SpecificCIIDsSelection.Build(monitoringModuleCIIDs), layerSetMonitoringDefinitionsOnly, false, trans, timeThreshold))
                .ToDictionary(ci => ci.ID);


            logger.LogDebug("Prep");

            // find and parse commands, insert into monitored CIs
            var renderedTemplateSegments = new List<(Guid ciid, string moduleName, string templateSegment)>();
            foreach (var p in allHasMonitoringModuleRelations)
            {
                logger.LogDebug("Process mm relation...");

                var monitoringModuleCI = monitoringModuleCIs[p.Relation.ToCIID];

                var monitoringModuleET = await traitModel.CalculateEffectiveTraitForCI(monitoringModuleCI, moduleTrait, trans, timeThreshold);
                if (monitoringModuleET == null)
                {
                    logger.LogError($"Expected CI {monitoringModuleCI.ID} to have trait \"{moduleTrait.Name}\"");
                    await errorHandler.LogError(monitoringModuleCI.ID, "error", $"Expected this CI to have trait \"{moduleTrait.Name}\"");
                    continue;
                }
                logger.LogDebug("  Fetched effective traits");
                var templateStr = (monitoringModuleET.TraitAttributes["template"].Attribute.Value as AttributeScalarValueText).Value;

                // create template context based on monitored CI, so that the templates can access all the related variables
                var context = ScribanVariableService.CreateCIBasedTemplateContext(monitoredCIs[p.Relation.FromCIID], layerSetAll, timeThreshold, null, ciModel, relationModel);

                logger.LogDebug("  Parse/Render config segments");
                // template parsing and rendering
                try
                {
                    logger.LogDebug($"  Parsing template:\n{templateStr}");

                    var template = Scriban.Template.Parse(templateStr);
                    string templateSegment = template.Render(context);
                    logger.LogDebug($"  Rendered template:\n{templateSegment}");
                    renderedTemplateSegments.Add((p.Relation.FromCIID, monitoringModuleCI.Name, templateSegment));
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
            await attributeModel.BulkReplaceAttributes(BulkCIAttributeDataLayerScope.Build("naemon.intermediate_config", targetLayer.ID, fragments), changesetProxy, trans);

            logger.LogDebug("Updated executed commands per monitored CI");

            // assign monitored cis to naemon instances
            var monitoredByCIIDFragments = new List<BulkRelationDataPredicateScope.Fragment>();
            var naemonInstancesTS = await traitModel.CalculateEffectiveTraitsForTrait(naemonInstanceTrait, layerSetAll, trans, timeThreshold);
            foreach (var naemonInstanceTS in naemonInstancesTS)
                foreach (var monitoredCI in monitoredCIs.Values)
                    if (CanCIBeMonitoredByNaemonInstance(monitoredCI, naemonInstanceTS.Value))
                        monitoredByCIIDFragments.Add(BulkRelationDataPredicateScope.Fragment.Build(monitoredCI.ID, naemonInstanceTS.Key));
            await relationModel.BulkReplaceRelations(BulkRelationDataPredicateScope.Build(isMonitoredByPredicate, targetLayer.ID, monitoredByCIIDFragments.ToArray()), changesetProxy, trans);
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
                        var naemonHost = new NaemonHost()
                        {
                            Name = monitoredCIs[t.Key].Name,
                            ID = t.Key,
                            Contactgroups = hostContactgroups,
                            // we pick the first host command we can find
                            Command = hostTemplate?.Command.ToFullCommandString() ?? "",
                            // TODO, HACK: handle duplicates in description
                            Services = t.SelectMany(t => t.serviceTemplates).ToDictionary(t => t.Description, t =>
                            {
                                return new NaemonService() { 
                                    Command = t.Command.ToFullCommandString(),
                                    Contactgroups = cgr.CalculateContactgroupsOfCI(t.ContactgroupSource).ToArray()
                                };
                            })
                        };
                        return naemonHost;
                    }).ToList();

                monitoringConfigs.Add(BulkCIAttributeDataLayerScope.Fragment.Build("", AttributeArrayValueJSON.BuildFromString(
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
            await attributeModel.BulkReplaceAttributes(BulkCIAttributeDataLayerScope.Build("naemon.config", targetLayer.ID, monitoringConfigs), changesetProxy, trans);

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
            private readonly CLBErrorHandler errorHandler;
            private Dictionary<Guid, IEnumerable<MergedCI>> contactGroupsMap;
            private readonly Dictionary<Guid, string> contactGroupNames = new Dictionary<Guid, string>();

            public ContactgroupResolver(IRelationModel relationModel, ICIModel ciModel, IEffectiveTraitModel traitModel, ILogger logger, CLBErrorHandler errorHandler)
            {
                this.relationModel = relationModel;
                this.ciModel = ciModel;
                this.traitModel = traitModel;
                this.logger = logger;
                this.errorHandler = errorHandler;
            }

            public async Task Setup(LayerSet layerSetAll, string belongsToNaemonContactgroup, Trait contactgroupTrait, NpgsqlTransaction trans, TimeThreshold timeThreshold)
            {
                var contactGroupRelations = await relationModel.GetMergedRelations(new RelationSelectionWithPredicate(belongsToNaemonContactgroup), layerSetAll, trans, timeThreshold);
                if (contactGroupRelations.IsEmpty())
                {
                    contactGroupsMap = new Dictionary<Guid, IEnumerable<MergedCI>>();
                }
                else
                {
                    var contactGroupCIs = (await ciModel.GetMergedCIs(SpecificCIIDsSelection.Build(contactGroupRelations.Select(r => r.Relation.ToCIID).Distinct()), layerSetAll, false, trans, timeThreshold)).ToDictionary(t => t.ID);
                    contactGroupsMap = contactGroupRelations.GroupBy(r => r.Relation.FromCIID).ToDictionary(t => t.Key, t => t.Select(tt => contactGroupCIs[tt.Relation.ToCIID]));
                    foreach (var ci in contactGroupsMap.Values.SelectMany(t => t).Distinct())
                    {
                        var et = await traitModel.CalculateEffectiveTraitForCI(ci, contactgroupTrait, trans, timeThreshold);
                        if (et != null)
                        {
                            var name = (et.TraitAttributes["name"].Attribute.Value as AttributeScalarValueText).Value;
                            contactGroupNames.Add(ci.ID, name);
                        }
                        else
                        {
                            logger.LogError($"Expected CI {ci.ID} to have trait \"{contactgroupTrait.Name}\"");
                            await errorHandler.LogError(ci.ID, "error", $"Expected this CI to have trait \"{contactgroupTrait.Name}\"");
                        }
                    }
                }
            }

            public IEnumerable<string> CalculateContactgroupsOfCI(Guid sourceCIID)
            {
                if (contactGroupsMap.TryGetValue(sourceCIID, out var ctCIs))
                {
                    foreach(var ctCI in ctCIs)
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
