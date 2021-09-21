﻿using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Model.Decorators
{
    public class OIABaseAttributeModel : IBaseAttributeModel
    {
        private readonly IBaseAttributeModel model;
        private readonly IOnlineAccessProxy onlineAccessProxy;

        public OIABaseAttributeModel(IBaseAttributeModel model, IOnlineAccessProxy onlineAccessProxy)
        {
            this.model = model;
            this.onlineAccessProxy = onlineAccessProxy;
        }

        public async Task<IDictionary<Guid, CIAttribute>> FindAttributesByFullName(string name, ICIIDSelection selection, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            if (await onlineAccessProxy.IsOnlineInboundLayer(layerID, trans))
            {
                // TODO
                var tmp = onlineAccessProxy.FindAttributesByFullName(name, selection, layerID, trans, atTime).ToEnumerable();
                return tmp.ToDictionary(t => t.CIID);
            }

            return await model.FindAttributesByFullName(name, selection, layerID, trans, atTime);
        }

        public async Task<CIAttribute?> GetFullBinaryAttribute(string name, Guid ciid, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            if (await onlineAccessProxy.IsOnlineInboundLayer(layerID, trans))
            {
                return await onlineAccessProxy.GetFullBinaryAttribute(name, layerID, ciid, trans, atTime);
            }

            return await model.GetFullBinaryAttribute(name, ciid, layerID, trans, atTime);
        }

        public async Task<IDictionary<Guid, IDictionary<string, CIAttribute>>[]> GetAttributes(ICIIDSelection selection, string[] layerIDs, bool returnRemoved, IModelContext trans, TimeThreshold atTime, string? nameRegexFilter = null)
        {
            var layerMap = new Dictionary<string, (int index, bool isOnlineLayer)>();
            var ii = 0;
            foreach (var layerID in layerIDs)
            {
                var isOnlineLayer = await onlineAccessProxy.IsOnlineInboundLayer(layerID, trans);
                layerMap.Add(layerID, (ii, isOnlineLayer));
                ii++;
            }

            if (!layerMap.Values.Any(l => l.isOnlineLayer))
            {
                return await model.GetAttributes(selection, layerIDs, returnRemoved, trans, atTime, nameRegexFilter);
            } else
            {
                var ret = new IDictionary<Guid, IDictionary<string, CIAttribute>>[layerMap.Count];

                // split online- and regular layers, add into return array
                var regularLayerIDs = layerMap.Where(l => !l.Value.isOnlineLayer).Select(t => t.Key).ToArray();
                var regularResults = await model.GetAttributes(selection, regularLayerIDs, returnRemoved, trans, atTime, nameRegexFilter);
                for(int i = 0;i < regularResults.Length;i++)
                {
                    var regularLayerID = regularLayerIDs[i];
                    var indexInFullArray = layerMap[regularLayerID].index;
                    ret[indexInFullArray] = regularResults[i];
                }

                var onlineLayerIDs = layerMap.Where(l => l.Value.isOnlineLayer).Select(t => t.Key).ToArray();
                var onlineResults = onlineAccessProxy.GetAttributes(selection, onlineLayerIDs, trans, atTime, nameRegexFilter).ToEnumerable();
                var groupedOnlineResults = onlineResults.GroupBy(t => t.layerID, t => t.attribute);
                foreach (var layerGroup in groupedOnlineResults)
                {
                    var layerID = layerGroup.Key;
                    var tmp2 = (IDictionary<Guid, IDictionary<string, CIAttribute>>)layerGroup.GroupBy(a => a.CIID).ToDictionary(t => t.Key, t => t.ToDictionary(t => t.Name));
                    var indexInFullArray = layerMap[layerID].index;
                    ret[indexInFullArray] = tmp2;
                }
                return ret;
            }
        }

        public async Task<(CIAttribute attribute, bool changed)> InsertAttribute(string name, IAttributeValue value, Guid ciid, string layerID, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        {
            if (await onlineAccessProxy.IsOnlineInboundLayer(layerID, trans)) throw new Exception("Cannot write to online inbound layer");

            return await model.InsertAttribute(name, value, ciid, layerID, changesetProxy, origin, trans);
        }

        public async Task<(CIAttribute attribute, bool changed)> RemoveAttribute(string name, Guid ciid, string layerID, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        {
            if (await onlineAccessProxy.IsOnlineInboundLayer(layerID, trans)) throw new Exception("Cannot write to online inbound layer");

            return await model.RemoveAttribute(name, ciid, layerID, changesetProxy, origin, trans);
        }

        public async Task<IEnumerable<(Guid ciid, string fullName)>> BulkReplaceAttributes<F>(IBulkCIAttributeData<F> data, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        {
            if (await onlineAccessProxy.IsOnlineInboundLayer(data.LayerID, trans)) throw new Exception("Cannot write to online inbound layer");

            return await model.BulkReplaceAttributes(data, changesetProxy, origin, trans);
        }

        public async Task<IEnumerable<CIAttribute>> GetAttributesOfChangeset(Guid changesetID, IModelContext trans)
        {
            // NOTE: OIAs do not support changesets, so an OIA can never return any
            return await model.GetAttributesOfChangeset(changesetID, trans);
        }
    }
}
