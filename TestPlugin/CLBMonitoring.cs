﻿using DotLiquid;
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

        public CLBMonitoring(ICIModel ciModel, ILayerModel layerModel, IRelationModel relationModel, IChangesetModel changesetModel, IUserModel userModel, NpgsqlConnection conn) 
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
            var monitoredCIs = (await ciModel.GetFullCIs(layerSetAll, true, trans, DateTimeOffset.Now, monitoredCIIDs)).ToDictionary(ci => ci.Identity);

            // find and parse commands, insert into monitored CIs
            var monitoringCommandFragments = new List<BulkCIAttributeDataFragment>();
            foreach (var p in allHasMonitoringModuleRelations)
            {
                var monitoringModuleCI = await ciModel.GetFullCI(p.ToCIID, layerSetMonitoringDefinitionsOnly, trans, DateTimeOffset.Now);
                if (monitoringModuleCI.Type.ID != "Monitoring Check Module")
                {
                    await errorHandler.LogError(monitoringModuleCI.Identity, "error", "Expected this CI to be of type \"Monitoring Check Module\"");
                    continue;
                }

                var monitoredCI = monitoredCIs[p.FromCIID];
                var monitoringCommands = monitoringModuleCI.GetAttributesInGroup("monitoring.commands");

                // add/collect variables
                var variables = new Dictionary<string, object>() { { "target", VariableService.CreateVariablesFromCI(monitoredCI) } };

                foreach (var mca in monitoringCommands)
                {
                    // template parsing and rendering
                    string finalCommand;
                    try
                    {
                        var command = mca.Value.Value2String();
                        Template template = Template.Parse(command);
                        finalCommand = template.Render(Hash.FromDictionary(variables));
                    }
                    catch (Exception e)
                    {
                        await errorHandler.LogError(mca.CIID, "error", $"Error parsing or rendering command: {e.Message}");
                        continue;
                    }
                    monitoringCommandFragments.Add(
                        BulkCIAttributeDataFragment.Build(BulkCIAttributeDataFragment.StripPrefix(mca.Name, "monitoring.commands"),
                        AttributeValueText.Build(finalCommand), p.FromCIID)
                    );
                }
            }
            await ciModel.BulkReplaceAttributes(BulkCIAttributeData.Build("monitoring.commands", layerID, monitoringCommandFragments.ToArray()), changeset.ID, trans);

            // assign monitored cis to naemon instances
            var monitoredByCIIDPairs = new List<(string, string)>();
            var naemonInstances = await ciModel.GetFullCIsWithType(layerSetMonitoringDefinitionsOnly, trans, DateTimeOffset.Now, "Naemon Instance");
            foreach (var naemonInstance in naemonInstances)
                foreach (var monitoredCI in monitoredCIs)
                    monitoredByCIIDPairs.Add((monitoredCI.Value.Identity, naemonInstance.Identity));
            await relationModel.BulkReplaceRelations(BulkRelationData.Build("is_monitored_by", layerID, monitoredByCIIDPairs.ToArray()), changeset.ID, trans);

            trans.Commit();

            return true;
        }
    }
}
