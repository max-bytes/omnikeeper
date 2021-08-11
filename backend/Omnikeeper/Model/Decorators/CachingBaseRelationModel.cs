using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
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

        public async Task<IEnumerable<(Guid fromCIID, Guid toCIID, string predicateID, RelationState state)>> BulkReplaceRelations<F>(IBulkRelationData<F> data, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        {
            var inserted = await model.BulkReplaceRelations(data, changesetProxy, origin, trans);
            foreach (var (fromCIID, toCIID, predicateID, _) in inserted)
            {
                EvictFromCache(fromCIID, toCIID, predicateID, data.LayerID, trans);
            }
            return inserted;
        }

        private void EvictFromCache(Guid fromCIID, Guid toCIID, string predicateID, string layerID, IModelContext trans)
        {
            trans.EvictFromCache(CacheKeyService.Relations(new RelationSelectionAll(), layerID));
            trans.EvictFromCache(CacheKeyService.Relations(new RelationSelectionEitherFromOrTo(fromCIID), layerID));
            trans.EvictFromCache(CacheKeyService.Relations(new RelationSelectionEitherFromOrTo(toCIID), layerID));
            trans.EvictFromCache(CacheKeyService.Relations(new RelationSelectionFrom(fromCIID), layerID));
            trans.EvictFromCache(CacheKeyService.Relations(new RelationSelectionWithPredicate(predicateID), layerID));
        }

        public async Task<Relation?> GetRelation(Guid fromCIID, Guid toCIID, string predicateID, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            // TODO: caching
            return await model.GetRelation(fromCIID, toCIID, predicateID, layerID, trans, atTime);
        }

        public async Task<IEnumerable<Relation>> GetRelations(IRelationSelection rl, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            if (atTime.IsLatest)
            {
                var (item, hit) = await trans.GetOrCreateCachedValueAsync(CacheKeyService.Relations(rl, layerID), async () =>
                {
                    return await model.GetRelations(rl, layerID, trans, atTime);
                });
                return item;
            }
            else
                return await model.GetRelations(rl, layerID, trans, atTime);
        }

        public async Task<(Relation relation, bool changed)> InsertRelation(Guid fromCIID, Guid toCIID, string predicateID, string layerID, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        {
            var t = await model.InsertRelation(fromCIID, toCIID, predicateID, layerID, changesetProxy, origin, trans);
            if (t.changed)
                EvictFromCache(fromCIID, toCIID, predicateID, layerID, trans);
            return t;
        }

        public async Task<(Relation relation, bool changed)> RemoveRelation(Guid fromCIID, Guid toCIID, string predicateID, string layerID, IChangesetProxy changesetProxy, IModelContext trans)
        {
            var t = await model.RemoveRelation(fromCIID, toCIID, predicateID, layerID, changesetProxy, trans);
            if (t.changed)
                EvictFromCache(fromCIID, toCIID, predicateID, layerID, trans);
            return t;
        }
    }
}
