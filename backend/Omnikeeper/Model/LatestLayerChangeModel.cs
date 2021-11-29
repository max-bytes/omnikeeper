using Npgsql;
using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Threading.Tasks;

namespace Omnikeeper.Model
{
    public class LatestLayerChangeModel : ILatestLayerChangeModel
    {
        private readonly IOnlineAccessProxy onlineAccessProxy;

        public LatestLayerChangeModel(IOnlineAccessProxy onlineAccessProxy)
        {
            this.onlineAccessProxy = onlineAccessProxy;
        }

        public async Task<DateTimeOffset?> GetLatestChangeInLayer(string layerID, IModelContext trans)
        {
            // check if this layer is an OIA layer, then we can't know the latest change
            if (await onlineAccessProxy.IsOnlineInboundLayer(layerID, trans))
            {
                return null;
            }
            else
            {
                var queryAttributes = "select max(timestamp) from attribute_latest where layer_id = @layer_id";
                using var commandAttributes = new NpgsqlCommand(queryAttributes, trans.DBConnection, trans.DBTransaction);
                commandAttributes.Parameters.AddWithValue("layer_id", layerID);
                commandAttributes.Prepare();
                var rawAttributes = await commandAttributes.ExecuteScalarAsync();
                var latestChangeInAttributes = (rawAttributes == null || rawAttributes == DBNull.Value) ? null : (DateTime?)rawAttributes;

                var queryRelations = "select max(timestamp) from relation_latest where layer_id = @layer_id";
                using var commandRelations = new NpgsqlCommand(queryRelations, trans.DBConnection, trans.DBTransaction);
                commandRelations.Parameters.AddWithValue("layer_id", layerID);
                commandRelations.Prepare();
                var rawRelations = await commandRelations.ExecuteScalarAsync();
                var latestChangeInRelations = (rawRelations == null || rawRelations == DBNull.Value) ? null : (DateTime?)rawRelations;

                DateTimeOffset? finalLatestChange = null;
                if (latestChangeInAttributes.HasValue)
                    finalLatestChange = latestChangeInAttributes.Value;
                if (latestChangeInRelations.HasValue && latestChangeInRelations.Value > finalLatestChange)
                    finalLatestChange = latestChangeInRelations.Value;

                return finalLatestChange;
            }
        }
    }
}
