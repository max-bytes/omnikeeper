using Landscape.Base.Entity;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace LandscapeRegistry.Service
{
    public class MarkedForDeletionService
    {
        private readonly IPredicateModel predicateModel;
        private readonly ILayerModel layerModel;
        public MarkedForDeletionService(IPredicateModel predicateModel, ILayerModel layerModel)
        {
            this.predicateModel = predicateModel;
            this.layerModel = layerModel;
        }

        public async Task<bool> Run(ILogger logger)
        {
            // try to delete marked predicates
            var toDeletePredicates = await predicateModel.GetPredicates(null, TimeThreshold.BuildLatest(), AnchorStateFilter.MarkedForDeletion);
            foreach (var d in toDeletePredicates)
            {
                var wasDeleted = await predicateModel.TryToDelete(d.Key, null);
                if (wasDeleted)
                {
                    logger.LogInformation($"Deleted predicate {d.Key}");
                }
                else
                {
                    logger.LogDebug($"Could not delete predicate {d.Key}");
                }
            }

            // try to delete marked layers
            var toDeleteLayers = await layerModel.GetLayers(AnchorStateFilter.MarkedForDeletion, null);
            foreach (var d in toDeleteLayers)
            {
                var wasDeleted = await layerModel.TryToDelete(d.ID, null);
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
