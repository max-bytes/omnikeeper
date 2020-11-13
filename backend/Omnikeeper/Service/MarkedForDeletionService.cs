using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System.Threading.Tasks;

namespace Omnikeeper.Service
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

        public async Task<bool> Run(IModelContextBuilder modelContextBuilder, ILogger logger)
        {
            // try to delete marked predicates
            var trans = modelContextBuilder.BuildImmediate();
            var toDeletePredicates = await predicateModel.GetPredicates(trans, TimeThreshold.BuildLatest(), AnchorStateFilter.MarkedForDeletion);
            foreach (var d in toDeletePredicates)
            {
                var wasDeleted = await predicateModel.TryToDelete(d.Key, trans);
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
