﻿using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Model.Decorators
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

        public async Task<IEnumerable<Relation>[]> GetRelations(IRelationSelection rl, string[] layerIDs, IModelContext trans, TimeThreshold atTime)
        {
            if (await onlineAccessProxy.ContainsOnlineInboundLayer(new LayerSet(layerIDs), trans))
            {
                throw new Exception("Not supported");
                //return onlineAccessProxy.GetRelations(rl, layerIDs, trans, atTime).ToEnumerable();
            }

            return await model.GetRelations(rl, layerIDs, trans, atTime);
        }

        public async Task<IEnumerable<Relation>> GetRelationsOfChangeset(Guid changesetID, bool getRemoved, IModelContext trans)
        {
            // NOTE: OIAs do not support changesets, so an OIA can never return any
            return await model.GetRelationsOfChangeset(changesetID, getRemoved, trans);
        }

        public async Task<(bool changed, Guid changesetID)> BulkUpdate(IList<(Guid fromCIID, Guid toCIID, string predicateID, Guid? existingRelationID, Guid newRelationID, bool mask)> inserts, IList<(Guid fromCIID, Guid toCIID, string predicateID, Guid existingRelationID, Guid newRelationID, bool mask)> removes, string layerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans)
        {
            if (await onlineAccessProxy.IsOnlineInboundLayer(layerID, trans)) throw new Exception("Cannot write to online inbound layer");

            return await model.BulkUpdate(inserts, removes, layerID, dataOrigin, changesetProxy, trans);
        }
    }
}
