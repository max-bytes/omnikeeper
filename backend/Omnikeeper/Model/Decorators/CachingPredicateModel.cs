//using Omnikeeper.Base.Entity;
//using Omnikeeper.Base.Model;
//using Omnikeeper.Base.Service;
//using Omnikeeper.Base.Utils;
//using Omnikeeper.Base.Utils.ModelContext;
//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;

//namespace Omnikeeper.Model.Decorators
//{
//    public class CachingPredicateModel : IPredicateModel
//    {
//        private IPredicateModel Model { get; }

//        public CachingPredicateModel(IPredicateModel model)
//        {
//            Model = model;
//        }

//        public async Task<IDictionary<string, Predicate>> GetPredicates(IModelContext trans, TimeThreshold atTime, AnchorStateFilter stateFilter)
//        {
//            if (atTime.IsLatest)
//            {
//                var (item, hit) = await trans.GetOrCreateCachedValueAsync(CacheKeyService.Predicates(stateFilter), async () =>
//                {
//                    return await Model.GetPredicates(trans, atTime, stateFilter);
//                });
//                return item;
//            }
//            else
//                return await Model.GetPredicates(trans, atTime, stateFilter);
//        }

//        public async Task<Predicate> GetPredicate(string id, TimeThreshold atTime, AnchorStateFilter stateFilter, IModelContext trans)
//        {
//            if (atTime.IsLatest)
//            {
//                var (item, hit) = await trans.GetOrCreateCachedValueAsync(CacheKeyService.Predicate(id), async () =>
//                {
//                    return await Model.GetPredicate(id, atTime, stateFilter, trans);
//                });
//                return item;
//            }

//            return await Model.GetPredicate(id, atTime, stateFilter, trans);
//        }

//        public async Task<(Predicate predicate, bool changed)> InsertOrUpdate(string id, string wordingFrom, string wordingTo, AnchorState state, PredicateConstraints constraints, IModelContext trans, DateTimeOffset? timestamp = null)
//        {
//            var (predicate, changed) = await Model.InsertOrUpdate(id, wordingFrom, wordingTo, state, constraints, trans, timestamp);

//            if (changed)
//            {
//                EvictFromCache(id, trans);
//            }

//            return (predicate, changed);
//        }

//        public async Task<bool> TryToDelete(string id, IModelContext trans)
//        {
//            var success = await Model.TryToDelete(id, trans);

//            if (success)
//            {
//                EvictFromCache(id, trans);
//            }

//            return success;
//        }

//        private void EvictFromCache(string id, IModelContext trans)
//        {
//            trans.EvictFromCache(CacheKeyService.Predicate(id));
//            System.Collections.IList list = Enum.GetValues(typeof(AnchorStateFilter));
//            for (int i = 0; i < list.Count; i++)
//            {
//                AnchorStateFilter sf = (AnchorStateFilter)list[i]!;
//                trans.EvictFromCache(CacheKeyService.Predicates(sf));
//            }
//        }
//    }
//}