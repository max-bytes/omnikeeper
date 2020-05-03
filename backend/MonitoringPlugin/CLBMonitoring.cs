using DotLiquid;
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

namespace MonitoringPlugin
{
    public class CLBMonitoring : CLBBase
    {
        private readonly IRelationModel relationModel;
        private readonly ICIModel ciModel;
        private readonly ITraitModel traitModel;

        public CLBMonitoring(ICIModel ciModel, IAttributeModel atributeModel, ILayerModel layerModel, ITraitModel traitModel, IRelationModel relationModel, IChangesetModel changesetModel, IUserInDatabaseModel userModel, NpgsqlConnection conn)
            : base(atributeModel, layerModel, changesetModel, userModel, conn)
        {
            this.ciModel = ciModel;
            this.relationModel = relationModel;
            this.traitModel = traitModel;
        }

        public override async Task<bool> Run(Layer targetLayer, Changeset changeset, CLBErrorHandler errorHandler, NpgsqlTransaction trans, ILogger logger)
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
            var renderedCommands = new List<(Guid ciid, string command)>();
            foreach (var p in allHasMonitoringModuleRelations)
            {
                logger.LogDebug("Process mm relation...");

                var monitoringModuleCI = monitoringModuleCIs[p.ToCIID];
                var monitoringModuleET = await traitModel.CalculateEffectiveTraitSetForCI(monitoringModuleCI, trans, timeThreshold); // TODO: move outside of loop, prefetch
                if (!monitoringModuleET.EffectiveTraits.ContainsKey("monitoring_check_module"))
                {
                    logger.LogError($"Expected CI {monitoringModuleCI.ID} to have trait \"monitoring_check_module\"");
                    await errorHandler.LogError(monitoringModuleCI.ID, "error", "Expected this CI to have trait \"monitoring_check_module\"");
                    continue;
                }
                logger.LogDebug("  Fetched effective traits");

                var monitoredCI = monitoredCIs[p.FromCIID];
                var monitoringCommands = monitoringModuleET.EffectiveTraits["monitoring_check_module"].TraitAttributes["commands"].Attribute.Value as AttributeValueTextArray;

                // add/collect variables
                var variables = new Dictionary<string, object>() { { "target", VariableService.CreateVariablesFromCI(monitoredCI) } };

                logger.LogDebug("  Parse/Render commands");
                // template parsing and rendering
                foreach (var commandStr in monitoringCommands.Values)
                {
                    string finalCommand;
                    try
                    {
                        var template = DotLiquid.Template.Parse(commandStr);
                        finalCommand = template.Render(Hash.FromDictionary(variables));
                    }
                    catch (Exception e)
                    {
                        logger.LogError($"Error parsing or rendering command from monitoring module \"{monitoringModuleCI.ID}\": {e.Message}");
                        await errorHandler.LogError(monitoringModuleCI.ID, "error", $"Error parsing or rendering command: {e.Message}");
                        continue;
                    }
                    renderedCommands.Add((p.FromCIID, finalCommand));
                }
                logger.LogDebug("Processed mm relation");
            }

            var monitoringCommandFragments = renderedCommands.GroupBy(t => t.ciid)
                .Select(tt => BulkCIAttributeDataLayerScope.Fragment.Build("", AttributeValueTextArray.Build(tt.Select(ttt => ttt.command).ToArray()), tt.Key));
            await attributeModel.BulkReplaceAttributes(BulkCIAttributeDataLayerScope.Build("monitoring.executing_commands", targetLayer.ID, monitoringCommandFragments), changeset.ID, trans);

            logger.LogDebug("Updated executed commands per monitored CI");

            // assign monitored cis to naemon instances
            var monitoredByCIIDFragments = new List<BulkRelationDataPredicateScope.Fragment>();
            var naemonInstancesTS = await traitModel.CalculateEffectiveTraitSetsForTraitName("naemon_instance", layerSetAll, trans, timeThreshold);
            foreach (var naemonInstance in naemonInstancesTS)
                foreach (var monitoredCI in monitoredCIs)
                    monitoredByCIIDFragments.Add(BulkRelationDataPredicateScope.Fragment.Build(monitoredCI.Value.ID, naemonInstance.UnderlyingCI.ID));
            await relationModel.BulkReplaceRelations(BulkRelationDataPredicateScope.Build("is_monitored_by", targetLayer.ID, monitoredByCIIDFragments.ToArray()), changeset.ID, trans);

            logger.LogDebug("Assigned CIs to naemon instances");

            logger.LogDebug("Writing final naemon config");

            // write final naemon config
            var checkCommandsPerNaemonInstance = new Dictionary<string, List<string>>();
            var naemonInstance2MonitoredCILookup = monitoredByCIIDFragments.GroupBy(t => t.To).ToDictionary(t => t.Key, t => t.Select(t => t.From));
            var monitoringConfigs = new List<BulkCIAttributeDataLayerScope.Fragment>();
            foreach (var kv in naemonInstance2MonitoredCILookup)
            {
                var naemonInstance = kv.Key;
                var cis = kv.Value;
                var commands = monitoringCommandFragments.Where(f => cis.Contains(f.CIID)).Select(f => string.Join("\n", (f.Value as AttributeValueTextArray).Values));
                var finalConfig = string.Join("\n", commands);
                monitoringConfigs.Add(BulkCIAttributeDataLayerScope.Fragment.Build("", AttributeValueTextScalar.Build(finalConfig, true), naemonInstance));
            }
            await attributeModel.BulkReplaceAttributes(BulkCIAttributeDataLayerScope.Build("monitoring.naemonConfig", targetLayer.ID, monitoringConfigs), changeset.ID, trans);

            logger.LogDebug("End clbMonitoring");
            return true;
        }
    }
}
