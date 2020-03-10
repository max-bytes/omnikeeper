using DotLiquid;
using Landscape.Base;
using Landscape.Base.Model;
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
            //var monitoringCheckModules = await ciModel.GetCIsWithType(layerSet, trans, DateTimeOffset.Now, "Monitoring Check Module");
            //foreach(var mcm in monitoringCheckModules)
            //{
            //    await ciModel.InsertAttribute("monitored CIs", AttributeValueText.Build("foo yeah"), layerID, mcm.Identity, changeset.ID, trans);
            //}

            var allMonitoredByRelations = await relationModel.GetRelationsWithPredicate(layerSetMonitoringDefinitionsOnly, false, "is monitored via", trans);

            foreach(var p in allMonitoredByRelations)
            {
                var monitoringModuleCI = await ciModel.GetCI(p.ToCIID, layerSetMonitoringDefinitionsOnly, trans, DateTimeOffset.Now);
                if (!monitoringModuleCI.IsOfType("Monitoring Check Module"))
                {
                    // TODO: error handling
                    await errorHandler.LogError(monitoringModuleCI.Identity, "error", "Expected this CI to be of type \"Monitoring Check Module\"");
                    continue;
                }

                var monitoredCI = await ciModel.GetCI(p.FromCIID, layerSetAll, trans, DateTimeOffset.Now);

                var monitoringCommands = monitoringModuleCI.GetAttributesInGroup("monitoring.commands");

                // TODO: more variables
                var variables = Hash.FromDictionary(new Dictionary<string, object>()
                {
                    { "ciid", monitoredCI.Identity }
                });

                foreach (var mca in monitoringCommands)
                {
                    // template parsing
                    Template template;
                    try
                    {
                        var command = mca.Value.Value2String();
                        template = Template.Parse(command);
                    }
                    catch (DotLiquid.Exceptions.LiquidException e)
                    {
                        await errorHandler.LogError(mca.CIID, "error", $"Error parsing command: {e.Message}");
                        continue;
                    }

                    // templat rendering
                    string finalCommand;
                    try
                    {
                        finalCommand = template.Render(variables);
                    } catch (DotLiquid.Exceptions.LiquidException e)
                    {
                        await errorHandler.LogError(mca.CIID, "error", $"Error rendering command: {e.Message}");
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
