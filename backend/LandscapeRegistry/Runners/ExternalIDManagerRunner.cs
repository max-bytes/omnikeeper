using Landscape.Base.Inbound;
using Landscape.Base.Model;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapeRegistry.Runners
{
    public class ExternalIDManagerRunner
    {
        private readonly IDictionary<string, IOnlineInboundLayerPlugin> plugins;
        private readonly ILogger<ExternalIDManagerRunner> logger;
        private readonly ICIModel ciModel;
        private readonly ILayerModel layerModel;
        private readonly NpgsqlConnection conn;

        public ExternalIDManagerRunner(IEnumerable<IOnlineInboundLayerPlugin> plugins, ICIModel ciModel, ILayerModel layerModel, NpgsqlConnection conn, ILogger<ExternalIDManagerRunner> logger)
        {
            this.plugins = plugins.ToDictionary(p => p.Name);
            this.logger = logger;
            this.ciModel = ciModel;
            this.layerModel = layerModel;
            this.conn = conn;
        }

        public void Run()
        {
            RunAsync().GetAwaiter().GetResult();
        }

        public async Task RunAsync()
        {
            logger.LogInformation("Start");

            var activeLayers = await layerModel.GetLayers(Landscape.Base.Entity.AnchorStateFilter.ActiveAndDeprecated, null);
            var layersWithOILPs = activeLayers.Where(l => l.OnlineInboundLayerPlugin.PluginName != ""); // TODO: better check for set oilp than name != ""

            foreach (var l in layersWithOILPs)
            {
                // find oilp for layer
                if (!plugins.TryGetValue(l.OnlineInboundLayerPlugin.PluginName, out var plugin))
                {
                    logger.LogError($"Could not find online inbound layer plugin with name {l.OnlineInboundLayerPlugin.PluginName}");
                }
                else
                {
                    logger.LogInformation($"Running external ID manager for OILP {l.OnlineInboundLayerPlugin.PluginName} on layer {l.Name}");

                    var manager = plugin.GetExternalIDManager(ciModel);

                    Stopwatch stopWatch = new Stopwatch();
                    stopWatch.Start();
                    await manager.Update(conn, logger);
                    stopWatch.Stop();
                    TimeSpan ts = stopWatch.Elapsed;
                    string elapsedTime = string.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
                    logger.LogInformation($"Done in {elapsedTime}");
                }
            }

            logger.LogInformation("Finished");
        }
    }
}
