using Microsoft.Extensions.Caching.Memory;
using Npgsql;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Model.Decorators
{
    public class CachingBaseRelationModel : IBaseRelationModel
    {
        private readonly IBaseRelationModel model;

        public CachingBaseRelationModel(IBaseRelationModel model)
        {
            this.model = model;
        }

        public async Task<IEnumerable<(Guid fromCIID, Guid toCIID, string predicateID, RelationState state)>> BulkReplaceRelations<F>(IBulkRelationData<F> data, IChangesetProxy changesetProxy, IModelContext trans)
        {
            var inserted = await model.BulkReplaceRelations(data, changesetProxy, trans);
            foreach (var (fromCIID, toCIID, predicateID, _) in inserted)
            {
                EvictFromCache(fromCIID, toCIID, predicateID, data.LayerID, trans);
            }
            return inserted;
        }

        private void EvictFromCache(Guid fromCIID, Guid toCIID, string predicateID, long layerID, IModelContext trans)
        {
            trans.CancelToken(CacheKeyService.RelationsChangeToken(new RelationSelectionAll(), layerID));
            trans.CancelToken(CacheKeyService.RelationsChangeToken(new RelationSelectionEitherFromOrTo(fromCIID), layerID));
            trans.CancelToken(CacheKeyService.RelationsChangeToken(new RelationSelectionEitherFromOrTo(toCIID), layerID));
            trans.CancelToken(CacheKeyService.RelationsChangeToken(new RelationSelectionFrom(fromCIID), layerID));
            trans.CancelToken(CacheKeyService.RelationsChangeToken(new RelationSelectionWithPredicate(predicateID), layerID));
        }

        public async Task<Relation?> GetRelation(Guid fromCIID, Guid toCIID, string predicateID, long layerID, IModelContext trans, TimeThreshold atTime)
        {
            // TODO: caching
            return await model.GetRelation(fromCIID, toCIID, predicateID, layerID, trans, atTime);
        }

        public async Task<IEnumerable<Relation>> GetRelations(IRelationSelection rl, long layerID, IModelContext trans, TimeThreshold atTime)
        {
            if (atTime.IsLatest)
            {
                return await trans.GetOrCreateCachedValueAsync(CacheKeyService.Relations(rl, layerID), async () =>
                {
                    return await model.GetRelations(rl, layerID, trans, atTime);
                }, CacheKeyService.RelationsChangeToken(rl, layerID));
            }
            else
                return await model.GetRelations(rl, layerID, trans, atTime);
        }

        public async Task<(Relation relation, bool changed)> InsertRelation(Guid fromCIID, Guid toCIID, string predicateID, long layerID, IChangesetProxy changesetProxy, IModelContext trans)
        {
            var t = await model.InsertRelation(fromCIID, toCIID, predicateID, layerID, changesetProxy, trans);
            if (t.changed)
                EvictFromCache(fromCIID, toCIID, predicateID, layerID, trans);
            return t;
        }

        public async Task<(Relation relation, bool changed)> RemoveRelation(Guid fromCIID, Guid toCIID, string predicateID, long layerID, IChangesetProxy changesetProxy, IModelContext trans)
        {
            var t = await model.RemoveRelation(fromCIID, toCIID, predicateID, layerID, changesetProxy, trans);
            if (t.changed)
                EvictFromCache(fromCIID, toCIID, predicateID, layerID, trans);
            return t;
        }
    }
}
