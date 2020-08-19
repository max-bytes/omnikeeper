using Landscape.Base.Entity;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using LandscapeRegistry.Service;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LandscapeRegistry.Model.Decorators
{
    public class CachingBaseRelationModel : IBaseRelationModel
    {
        private readonly IBaseRelationModel model;
        private readonly IMemoryCache memoryCache;

        public CachingBaseRelationModel(IBaseRelationModel model, IMemoryCache memoryCache)
        {
            this.model = model;
            this.memoryCache = memoryCache;
        }

        public async Task<bool> BulkReplaceRelations<F>(IBulkRelationData<F> data, IChangesetProxy changesetProxy, NpgsqlTransaction trans)
        {
            var success = await model.BulkReplaceRelations(data, changesetProxy, trans);
            if (success)
                foreach (var f in data.Fragments)
                {
                    EvictFromCache(data.GetFromCIID(f), data.GetToCIID(f), data.GetPredicateID(f), data.LayerID);
                }
            return success;
        }

        private void EvictFromCache(Guid fromCIID, Guid toCIID, string predicateID, long layerID)
        {
            memoryCache.CancelRelationsChangeToken(new RelationSelectionAll(), layerID);
            memoryCache.CancelRelationsChangeToken(new RelationSelectionEitherFromOrTo(fromCIID), layerID);
            memoryCache.CancelRelationsChangeToken(new RelationSelectionEitherFromOrTo(toCIID), layerID);
            memoryCache.CancelRelationsChangeToken(new RelationSelectionFrom(fromCIID), layerID);
            memoryCache.CancelRelationsChangeToken(new RelationSelectionWithPredicate(predicateID), layerID);
        }

        public async Task<Relation> GetRelation(Guid fromCIID, Guid toCIID, string predicateID, long layerID, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            // TODO: caching
            return await model.GetRelation(fromCIID, toCIID, predicateID, layerID, trans, atTime);
        }

        public async Task<IEnumerable<Relation>> GetRelations(IRelationSelection rl, long layerID, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            if (atTime.IsLatest) {
                return await memoryCache.GetOrCreateAsync(CacheKeyService.Relations(rl, layerID), async (ce) =>
                {
                    var changeToken = memoryCache.GetRelationsCancellationChangeToken(rl, layerID);
                    ce.AddExpirationToken(changeToken);
                    return await model.GetRelations(rl, layerID, trans, atTime);
                });
            }
            else 
                return await model.GetRelations(rl, layerID, trans, atTime);
        }

        public async Task<Relation> InsertRelation(Guid fromCIID, Guid toCIID, string predicateID, long layerID, IChangesetProxy changesetProxy, NpgsqlTransaction trans)
        {
            EvictFromCache(fromCIID, toCIID, predicateID, layerID);
            return await model.InsertRelation(fromCIID, toCIID, predicateID, layerID, changesetProxy, trans);
        }

        public async Task<Relation> RemoveRelation(Guid fromCIID, Guid toCIID, string predicateID, long layerID, IChangesetProxy changesetProxy, NpgsqlTransaction trans)
        {
            EvictFromCache(fromCIID, toCIID, predicateID, layerID);
            return await model.RemoveRelation(fromCIID, toCIID, predicateID, layerID, changesetProxy, trans);
        }
    }
}
