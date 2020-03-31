using DotLiquid;
using Landscape.Base;
using Landscape.Base.Model;
using Landscape.Base.Templating;
using LandscapePrototype.Entity;
using LandscapePrototype.Entity.AttributeValues;
using LandscapePrototype.Model;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestPlugin
{
    public class CLBMonitoring : CLBBase
    {
        private readonly IRelationModel relationModel;

        public CLBMonitoring(ICIModel ciModel, ILayerModel layerModel, IRelationModel relationModel, IChangesetModel changesetModel, IUserInDatabaseModel userModel, NpgsqlConnection conn) 
            : base(ciModel, layerModel, changesetModel, userModel, conn)
        {
            this.relationModel = relationModel;
        }

        public override async Task<bool> Run(long layerID, Changeset changeset, CLBErrorHandler errorHandler, NpgsqlTransaction trans)
        {
            var layerSetMonitoringDefinitionsOnly = await layerModel.BuildLayerSet(new[] { "Monitoring Definitions" }, trans);
            var layerSetAll = await layerModel.BuildLayerSet(new[] { "CMDB", "Inventory Scan", "Monitoring Definitions" }, trans);
            
            var allHasMonitoringModuleRelations = await relationModel.GetRelationsWithPredicateID(layerSetMonitoringDefinitionsOnly, false, "has_monitoring_module", trans);

            // prepare list of all monitored cis
            var monitoredCIIDs = allHasMonitoringModuleRelations.Select(r => r.FromCIID).Distinct();
            var monitoredCIs = (await ciModel.GetMergedCIs(layerSetAll, true, trans, DateTimeOffset.Now, monitoredCIIDs))
                .ToDictionary(ci => ci.Identity);

            // prepare list of all monitoring modules
            var monitoringModuleCIIDs = allHasMonitoringModuleRelations.Select(r => r.ToCIID).Distinct();
            var monitoringModuleCIs = (await ciModel.GetMergedCIs(layerSetMonitoringDefinitionsOnly, false, trans, DateTimeOffset.Now, monitoringModuleCIIDs))
                .ToDictionary(ci => ci.Identity);

            // find and parse commands, insert into monitored CIs
            var monitoringCommandFragments = new List<BulkCIAttributeDataLayerScope.Fragment>();
            foreach (var p in allHasMonitoringModuleRelations)
            {
                var monitoringModuleCI = monitoringModuleCIs[p.ToCIID];
                if (monitoringModuleCI.Type.ID != "Monitoring Check Module")
                {
                    await errorHandler.LogError(monitoringModuleCI.Identity, "error", "Expected this CI to be of type \"Monitoring Check Module\"");
                    continue;
                }

                var monitoredCI = monitoredCIs[p.FromCIID];
                var monitoringCommands = monitoringModuleCI.GetAttributesInGroup("monitoring.commands");

                // add/collect variables
                var variables = new Dictionary<string, object>() { { "target", VariableService.CreateVariablesFromCI(monitoredCI) } };

                // template parsing and rendering
                foreach (var mca in monitoringCommands)
                {
                    string finalCommand;
                    try
                    {
                        var command = mca.Attribute.Value.Value2String();
                        Template template = Template.Parse(command);
                        finalCommand = template.Render(Hash.FromDictionary(variables));
                    }
                    catch (Exception e)
                    {
                        await errorHandler.LogError(mca.Attribute.CIID, "error", $"Error parsing or rendering command: {e.Message}");
                        continue;
                    }
                    monitoringCommandFragments.Add(
                        BulkCIAttributeDataLayerScope.Fragment.Build(BulkCIAttributeDataLayerScope.Fragment.StripPrefix(mca.Attribute.Name, "monitoring.commands"),
                        AttributeValueTextScalar.Build(finalCommand), p.FromCIID)
                    );
                }
            }
            await ciModel.BulkReplaceAttributes(BulkCIAttributeDataLayerScope.Build("monitoring.commands", layerID, monitoringCommandFragments), changeset.ID, trans);

            // assign monitored cis to naemon instances
            var monitoredByCIIDPairs = new List<(string, string)>();
            var naemonInstances = await ciModel.GetMergedCIsByType(layerSetMonitoringDefinitionsOnly, trans, DateTimeOffset.Now, "Naemon Instance");
            foreach (var naemonInstance in naemonInstances)
                foreach (var monitoredCI in monitoredCIs)
                    monitoredByCIIDPairs.Add((monitoredCI.Value.Identity, naemonInstance.Identity));
            await relationModel.BulkReplaceRelations(BulkRelationData.Build("is_monitored_by", layerID, monitoredByCIIDPairs.ToArray()), changeset.ID, trans);

            // write final naemon config
            var checkCommandsPerNaemonInstance = new Dictionary<string, List<string>>();
            var naemonInstance2MoitoredCILookup = monitoredByCIIDPairs.GroupBy(t => t.Item2).ToDictionary(t => t.Key, t => t.Select(t => t.Item1));
            var monitoringConfigs = new List<BulkCIAttributeDataLayerScope.Fragment>();
            foreach (var kv in naemonInstance2MoitoredCILookup)
            {
                var naemonInstance = kv.Key;
                var cis = kv.Value;
                var commands = monitoringCommandFragments.Where(f => cis.Contains(f.CIID)).Select(f => f.Value.Value2String());
                var finalConfig = string.Join("\n", commands);
                monitoringConfigs.Add(BulkCIAttributeDataLayerScope.Fragment.Build("naemonConfig", AttributeValueTextScalar.Build(finalConfig, true), naemonInstance));
            }
            await ciModel.BulkReplaceAttributes(BulkCIAttributeDataLayerScope.Build("monitoring", layerID, monitoringConfigs), changeset.ID, trans);

            return true;
        }
    }
}
