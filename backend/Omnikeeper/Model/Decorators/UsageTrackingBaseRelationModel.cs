using Autofac;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Model.Decorators
{
    public class UsageTrackingBaseRelationModel : IBaseRelationModel
    {
        private readonly IBaseRelationModel model;
        private readonly ScopedLifetimeAccessor scopedLifetimeAccessor;

        private void TrackLayerUsage(string layerID)
        {
            var usageTracker = scopedLifetimeAccessor.GetLifetimeScope()?.Resolve<IScopedUsageTracker>();
            if (usageTracker != null)
                usageTracker.TrackUseLayer(layerID);
        }

        public UsageTrackingBaseRelationModel(IBaseRelationModel model, ScopedLifetimeAccessor scopedLifetimeAccessor)
        {
            this.model = model;
            this.scopedLifetimeAccessor = scopedLifetimeAccessor;
        }

        public async Task<IEnumerable<(Guid fromCIID, Guid toCIID, string predicateID)>> BulkReplaceRelations<F>(IBulkRelationData<F> data, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        {
            TrackLayerUsage(data.LayerID);
            return await model.BulkReplaceRelations(data, changesetProxy, origin, trans);
        }

        public async Task<Relation?> GetRelation(Guid fromCIID, Guid toCIID, string predicateID, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            TrackLayerUsage(layerID);
            return await model.GetRelation(fromCIID, toCIID, predicateID, layerID, trans, atTime);
        }

        public async Task<IEnumerable<Relation>> GetRelations(IRelationSelection rl, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            TrackLayerUsage(layerID);
            return await model.GetRelations(rl, layerID, trans, atTime);
        }

        public async Task<(Relation relation, bool changed)> InsertRelation(Guid fromCIID, Guid toCIID, string predicateID, string layerID, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        {
            TrackLayerUsage(layerID);
            return await model.InsertRelation(fromCIID, toCIID, predicateID, layerID, changesetProxy, origin, trans);
        }

        public async Task<(Relation relation, bool changed)> RemoveRelation(Guid fromCIID, Guid toCIID, string predicateID, string layerID, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        {
            TrackLayerUsage(layerID);
            return await model.RemoveRelation(fromCIID, toCIID, predicateID, layerID, changesetProxy, origin, trans);
        }

        public async Task<IEnumerable<Relation>> GetRelationsOfChangeset(Guid changesetID, bool getRemoved, IModelContext trans)
        {
            return await model.GetRelationsOfChangeset(changesetID, getRemoved, trans);
        }

        //public async Task<bool> BulkUpdateRelations(IList<(Guid thisCIID, string predicateID, IEnumerable<Guid> otherCIIDs)> d, bool outgoing, string layerID, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        //{
        //    TrackLayerUsage(layerID);
        //    return await model.BulkUpdateRelations(d, outgoing, layerID, changesetProxy, origin, trans);
        //}

        //public async Task<bool> BulkReplaceOutgoingRelations(Guid fromCIID, string predicateID, IEnumerable<Guid> toCIIDs, string layerID, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        //{
        //    TrackLayerUsage(layerID);
        //    return await model.BulkReplaceOutgoingRelations(fromCIID, predicateID, toCIIDs, layerID, changesetProxy, origin, trans);
        //}

        //public async Task<bool> BulkReplaceIncomingRelations(Guid toCIID, string predicateID, IEnumerable<Guid> fromCIIDs, string layerID, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        //{
        //    TrackLayerUsage(layerID);
        //    return await model.BulkReplaceIncomingRelations(toCIID, predicateID, fromCIIDs, layerID, changesetProxy, origin, trans);
        //}
    }
}
