using GraphQL;
using GraphQL.Types;
using Landscape.Base;
using Landscape.Base.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapeRegistry.Runners
{
    public class CLBRunner
    {
        public CLBRunner(IEnumerable<IComputeLayerBrain> existingComputeLayerBrains, ILayerModel layerModel, ILogger<CLBRunner> logger)
        {
            this.existingComputeLayerBrains = existingComputeLayerBrains.ToDictionary(l => l.Name);
            this.layerModel = layerModel;
            this.logger = logger;
        }

        public void Run()
        {
            RunAsync().GetAwaiter().GetResult();
        }

        public async Task RunAsync()
        {
            logger.LogInformation("Start");

            var activeLayers = await layerModel.GetLayers(Landscape.Base.Entity.AnchorStateFilter.ActiveOnly, null);
            var layersWithCLBs = activeLayers.Where(l => l.ComputeLayerBrain.Name != ""); // TODO: better check for set clb than name != ""

            foreach(var l in layersWithCLBs)
            {
                // find clb for layer
                if (!existingComputeLayerBrains.TryGetValue(l.ComputeLayerBrain.Name, out var clb))
                {
                    logger.LogError($"Could not find compute layer brain with name {l.ComputeLayerBrain.Name}");
                } else
                {
                    await clb.Run(new CLBSettings(l.Name), logger);
                }
            }

            logger.LogInformation("Finished");
        }

        private readonly IDictionary<string, IComputeLayerBrain> existingComputeLayerBrains;
        private readonly ILayerModel layerModel;
        private readonly ILogger<CLBRunner> logger;
    }
}
