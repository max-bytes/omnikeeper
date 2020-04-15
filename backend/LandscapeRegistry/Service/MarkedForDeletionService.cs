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
        public MarkedForDeletionService(IPredicateModel predicateModel)
        {
            this.predicateModel = predicateModel;
        }

        public void Run()
        {
            RunAsync().GetAwaiter().GetResult();
        }

        public async Task RunAsync()
        {
            // try to delete marked predicates
            var toDeletePredicates = await predicateModel.GetPredicates(null, null, IPredicateModel.PredicateStateFilter.MarkedForDeletion);
            foreach(var d in toDeletePredicates)
            {
                var wasDeleted = await predicateModel.TryToDelete(d.Key, null);
                Console.WriteLine(wasDeleted);
            }
        }
    }
}
