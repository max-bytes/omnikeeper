using Omnikeeper.Base.Entity;
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

        public async Task<CIAttribute?> GetFullBinaryAttribute(string name, Guid ciid, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            if (await onlineAccessProxy.IsOnlineInboundLayer(layerID, trans))
            {
                return await onlineAccessProxy.GetFullBinaryAttribute(name, layerID, ciid, trans, atTime);
            }

            return await model.GetFullBinaryAttribute(name, ciid, layerID, trans, atTime);
        }

        public async Task<IDictionary<Guid, IDictionary<string, CIAttribute>>[]> GetAttributes(ICIIDSelection selection, IAttributeSelection attributeSelection, string[] layerIDs, IModelContext trans, TimeThreshold atTime, IGeneratedDataHandling generatedDataHandling)
        {
            switch (generatedDataHandling)
            {
                case GeneratedDataHandlingExclude:
                    return await model.GetAttributes(selection, attributeSelection, layerIDs, trans, atTime, generatedDataHandling);
                case GeneratedDataHandlingInclude:
                    return await MixOnlineAndRegular(layerIDs, trans,
                    async (regularLayerIDs) => await model.GetAttributes(selection, attributeSelection, regularLayerIDs, trans, atTime, generatedDataHandling),
                    async (onlineLayerIDs) =>
                    {
                        var onlineResults = await onlineAccessProxy.GetAttributes(selection, onlineLayerIDs, trans, atTime, attributeSelection);

                        var ret = new IDictionary<Guid, IDictionary<string, CIAttribute>>[onlineLayerIDs.Length];
                        for (int i = 0; i < onlineResults.Length; i++)
                        {
                            var layerID = onlineLayerIDs[i];
                            var tmp2 = (IDictionary<Guid, IDictionary<string, CIAttribute>>)onlineResults[i].GroupBy(a => a.CIID).ToDictionary(t => t.Key, t => t.ToDictionary(t => t.Name));
                            ret[i] = tmp2;
                        }
                        return ret;
                    });
                default:
                    throw new Exception("Unknown generated-data-handling detected");
            }
        }

        private async Task<T[]> MixOnlineAndRegular<T>(string[] layerIDs, IModelContext trans, Func<string[], Task<T[]>> baseFetchF, Func<string[], Task<T[]>> proxyFetchF)
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
                return await baseFetchF(layerIDs);
            }
            else
            {
                var ret = new T[layerMap.Count];

                // split online- and regular layers, add into return array
                var regularLayerIDs = layerMap.Where(l => !l.Value.isOnlineLayer).Select(t => t.Key).ToArray();
                var regularResults = await baseFetchF(regularLayerIDs);
                for (int i = 0; i < regularResults.Length; i++)
                {
                    var regularLayerID = regularLayerIDs[i];
                    var indexInFullArray = layerMap[regularLayerID].index;
                    ret[indexInFullArray] = regularResults[i];
                }

                var onlineLayerIDs = layerMap.Where(l => l.Value.isOnlineLayer).Select(t => t.Key).ToArray();
                var onlineResults = await proxyFetchF(onlineLayerIDs);
                for (int i = 0; i < onlineResults.Length; i++)
                {
                    var onlineLayerID = onlineLayerIDs[i];
                    var indexInFullArray = layerMap[onlineLayerID].index;
                    ret[indexInFullArray] = regularResults[i];
                }
                return ret;
            }
        }

        public async Task<IReadOnlySet<Guid>> GetCIIDsWithAttributes(ICIIDSelection selection, string[] layerIDs, IModelContext trans, TimeThreshold atTime)
        {
            // TODO: implement
            return await model.GetCIIDsWithAttributes(selection, layerIDs, trans, atTime);
        }

        public async Task<(bool changed, Guid changesetID)> BulkUpdate(IList<(Guid ciid, string fullName, IAttributeValue value, Guid? existingAttributeID, Guid newAttributeID)> inserts, IList<(Guid ciid, string name, IAttributeValue value, Guid attributeID, Guid newAttributeID)> removes, string layerID, IChangesetProxy changesetProxy, IModelContext trans)
        {
            if (await onlineAccessProxy.IsOnlineInboundLayer(layerID, trans)) throw new Exception("Cannot write to online inbound layer");

            return await model.BulkUpdate(inserts, removes, layerID, changesetProxy, trans);
        }

        public async Task<IReadOnlyList<CIAttribute>> GetAttributesOfChangeset(Guid changesetID, bool getRemoved, IModelContext trans)
        {
            // NOTE: OIAs do not support changesets, so an OIA can never return any
            return await model.GetAttributesOfChangeset(changesetID, getRemoved, trans);
        }
    }
}
