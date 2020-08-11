using Landscape.Base.Entity;
using Landscape.Base.Inbound;
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
    public class OIABaseRelationModel : IBaseRelationModel
    {
        private readonly IBaseRelationModel model;
        private readonly IOnlineAccessProxy onlineAccessProxy;

        public OIABaseRelationModel(IBaseRelationModel model, IOnlineAccessProxy onlineAccessProxy)
        {
            this.model = model;
            this.onlineAccessProxy = onlineAccessProxy;
        }

        public async Task<bool> BulkReplaceRelations<F>(IBulkRelationData<F> data, IChangesetProxy changesetProxy, NpgsqlTransaction trans)
        {
            if (await onlineAccessProxy.IsOnlineInboundLayer(data.LayerID, trans)) throw new Exception("Cannot write to online inbound layer");

            return await model.BulkReplaceRelations(data, changesetProxy, trans);
        }

        public async Task<Relation> GetRelation(Guid fromCIID, Guid toCIID, string predicateID, long layerID, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            if (await onlineAccessProxy.IsOnlineInboundLayer(layerID, trans))
            {
                return await onlineAccessProxy.GetRelation(fromCIID, toCIID, predicateID, layerID, trans, atTime);
            }

            return await model.GetRelation(fromCIID, toCIID, predicateID, layerID, trans, atTime);
        }

        public async Task<IEnumerable<Relation>> GetRelations(IRelationSelection rl, bool includeRemoved, long layerID, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            if (await onlineAccessProxy.IsOnlineInboundLayer(layerID, trans))
            {
                return onlineAccessProxy.GetRelations(rl, layerID, trans, atTime).ToEnumerable();
            }

            return await model.GetRelations(rl, includeRemoved, layerID, trans, atTime);
        }

        public async Task<Relation> InsertRelation(Guid fromCIID, Guid toCIID, string predicateID, long layerID, IChangesetProxy changesetProxy, NpgsqlTransaction trans)
        {
            if (await onlineAccessProxy.IsOnlineInboundLayer(layerID, trans)) throw new Exception("Cannot write to online inbound layer");

            return await model.InsertRelation(fromCIID, toCIID, predicateID, layerID, changesetProxy, trans);
        }

        public async Task<Relation> RemoveRelation(Guid fromCIID, Guid toCIID, string predicateID, long layerID, IChangesetProxy changesetProxy, NpgsqlTransaction trans)
        {
            if (await onlineAccessProxy.IsOnlineInboundLayer(layerID, trans)) throw new Exception("Cannot write to online inbound layer");

            return await model.RemoveRelation(fromCIID, toCIID, predicateID, layerID, changesetProxy, trans);
        }
    }
}
