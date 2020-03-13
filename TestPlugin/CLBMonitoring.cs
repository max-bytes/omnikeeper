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

        public CLBMonitoring(ICIModel ciModel, ILayerModel layerModel, IRelationModel relationModel, IChangesetModel changesetModel, NpgsqlConnection conn) 
            : base(ciModel, layerModel, changesetModel, conn)
        {
            this.relationModel = relationModel;
        }

        public override async Task<bool> Run(long layerID, Changeset changeset, CLBErrorHandler errorHandler, NpgsqlTransaction trans)
        {
            var layerSetMonitoringDefinitionsOnly = await layerModel.BuildLayerSet(new[] { "Monitoring Definitions" }, trans);
            var layerSetAll = await layerModel.BuildLayerSet(new[] { "CMDB", "Inventory Scan", "Monitoring Definitions" }, trans);

            var allMonitoredByRelations = await relationModel.GetRelationsWithPredicate(layerSetMonitoringDefinitionsOnly, false, "is monitored via", trans);

            foreach (var p in allMonitoredByRelations)
            {
                var monitoringModuleCI = await ciModel.GetCI(p.ToCIID, layerSetMonitoringDefinitionsOnly, trans, DateTimeOffset.Now);
                if (!monitoringModuleCI.IsOfType("Monitoring Check Module"))
                {
                    await errorHandler.LogError(monitoringModuleCI.Identity, "error", "Expected this CI to be of type \"Monitoring Check Module\"");
                    continue;
                }

                var monitoredCI = await ciModel.GetCI(p.FromCIID, layerSetAll, trans, DateTimeOffset.Now);
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

                    await ciModel.InsertAttribute(mca.Name, AttributeValueText.Build(finalCommand), layerID, p.FromCIID, changeset.ID, trans);
                }
            }

            trans.Commit();

            return true;
        }
    }
}
