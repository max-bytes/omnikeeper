using Npgsql;
using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Service
{
    public class LatestLayerChangeService
    {
        private readonly IOnlineAccessProxy onlineAccessProxy;

        private readonly IDictionary<string, DateTimeOffset?> cache = new Dictionary<string, DateTimeOffset?>();

        public LatestLayerChangeService(IOnlineAccessProxy onlineAccessProxy)
        {
            this.onlineAccessProxy = onlineAccessProxy;
        }

        public async Task<DateTimeOffset?> GetLatestChangeInLayer(string layerID, IModelContext trans)
        {
            if (cache.TryGetValue(layerID, out var v))
            {
                return v;
            }
            else
            {
                // check if this layer is an OIA layer, then we can't know the latest change
                if (await onlineAccessProxy.IsOnlineInboundLayer(layerID, trans))
                {
                    cache.Add(layerID, null);

                    return null;
                }
                else
                {

                    var queryAttributes = "select max(timestamp) from attribute_latest where layer_id = @layer_id";
                    using var commandAttributes = new NpgsqlCommand(queryAttributes, trans.DBConnection, trans.DBTransaction);
                    commandAttributes.Prepare();
                    var latestChangeInAttributes = (DateTimeOffset?)await commandAttributes.ExecuteScalarAsync();

                    var queryRelations = "select max(timestamp) from relation_latest where layer_id = @layer_id";
                    using var commandRelations = new NpgsqlCommand(queryRelations, trans.DBConnection, trans.DBTransaction);
                    commandRelations.Prepare();
                    var latestChangeInRelations = (DateTimeOffset?)await commandRelations.ExecuteScalarAsync();

                    var finalLatestChange = DateTimeOffset.MinValue;
                    if (latestChangeInAttributes.HasValue)
                        finalLatestChange = latestChangeInAttributes.Value;
                    if (latestChangeInRelations.HasValue && latestChangeInRelations.Value > finalLatestChange)
                        finalLatestChange = latestChangeInRelations.Value;

                    cache.Add(layerID, finalLatestChange);

                    return finalLatestChange;
                }
            }
        }
    }
}
