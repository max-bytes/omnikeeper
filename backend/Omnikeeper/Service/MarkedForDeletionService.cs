using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils.ModelContext;
using System.Threading.Tasks;

namespace Omnikeeper.Service
{
    public class MarkedForDeletionService
    {
        private readonly ILayerModel layerModel;

        public MarkedForDeletionService(ILayerModel layerModel)
        {
            this.layerModel = layerModel;
        }

        public async Task<bool> Run(IModelContextBuilder modelContextBuilder, ILogger logger)
        {
            var trans = modelContextBuilder.BuildImmediate();

            // try to delete marked layers
            var toDeleteLayers = await layerModel.GetLayers(AnchorStateFilter.MarkedForDeletion, trans);
            foreach (var d in toDeleteLayers)
            {
                var wasDeleted = await layerModel.TryToDelete(d.ID, trans);
                if (wasDeleted)
                {
                    logger.LogInformation($"Deleted layer {d.ID}");
                }
                else
                {
                    logger.LogDebug($"Could not delete layer {d.ID}");
                }
            }

            return true;
        }
    }
}
