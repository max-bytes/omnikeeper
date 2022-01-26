using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Model.Decorators.CachingLatestLayerChange
{
    public class CachingLatestLayerChangeRelationModel : IBaseRelationModel
    {
        private readonly IBaseRelationModel model;
        private readonly LatestLayerChangeCache cache;

        public CachingLatestLayerChangeRelationModel(IBaseRelationModel model, LatestLayerChangeCache cache)
        {
            this.model = model;
            this.cache = cache;
        }

        public async Task<IEnumerable<Relation>[]> GetRelations(IRelationSelection rl, string[] layerIDs, IModelContext trans, TimeThreshold atTime)
        {
            return await model.GetRelations(rl, layerIDs, trans, atTime);
        }

        public async Task<IEnumerable<Relation>> GetRelationsOfChangeset(Guid changesetID, bool getRemoved, IModelContext trans)
        {
            return await model.GetRelationsOfChangeset(changesetID, getRemoved, trans);
        }

        public async Task<(Relation relation, bool changed)> InsertRelation(Guid fromCIID, Guid toCIID, string predicateID, string layerID, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        {
            var t = await model.InsertRelation(fromCIID, toCIID, predicateID, layerID, changesetProxy, origin, trans);
            if (t.changed)
                cache.UpdateCache(layerID, changesetProxy.TimeThreshold.Time);
            return t;
        }

        public async Task<(Relation relation, bool changed)> RemoveRelation(Guid fromCIID, Guid toCIID, string predicateID, string layerID, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        {
            var t = await model.RemoveRelation(fromCIID, toCIID, predicateID, layerID, changesetProxy, origin, trans);
            if (t.changed)
                cache.UpdateCache(layerID, changesetProxy.TimeThreshold.Time);
            return t;
        }

        public async Task<IEnumerable<(Guid fromCIID, Guid toCIID, string predicateID)>> BulkReplaceRelations<F>(IBulkRelationData<F> data, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans, IMaskHandlingForRemoval maskHandling)
        {
            var t = await model.BulkReplaceRelations(data, changesetProxy, origin, trans, maskHandling);
            if (!t.IsEmpty())
                cache.UpdateCache(data.LayerID, changesetProxy.TimeThreshold.Time);
            return t;
        }
    }
}
