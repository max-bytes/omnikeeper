using Landscape.Base.Entity;
using Landscape.Base.Model;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public void Run()
        {
            RunAsync().GetAwaiter().GetResult();
        }

        public async Task RunAsync()
        {
            // try to delete marked predicates
            var toDeletePredicates = await predicateModel.GetPredicates(null, null, AnchorStateFilter.MarkedForDeletion);
            foreach(var d in toDeletePredicates)
            {
                var wasDeleted = await predicateModel.TryToDelete(d.Key, null);
                Console.WriteLine(wasDeleted);
            }


            // try to delete marked layers
            var toDeleteLayers = await layerModel.GetLayers(AnchorStateFilter.MarkedForDeletion, null);
            foreach (var d in toDeleteLayers)
            {
                var wasDeleted = await layerModel.TryToDelete(d.ID, null);
                Console.WriteLine(wasDeleted);
            }
        }
    }
}
