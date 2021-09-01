using Npgsql;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Threading.Tasks;

namespace Omnikeeper.Model
{
    public class BaseAttributeRevisionistModel : IBaseAttributeRevisionistModel
    {
        public async Task<int> DeleteAllAttributes(string layerID, IModelContext trans)
        {
            var query = @"delete from attribute a where a.layer_id = @layer_id";

            using var command = new NpgsqlCommand(query, trans.DBConnection, trans.DBTransaction);
            command.Parameters.AddWithValue("layer_id", layerID);
            command.Prepare();

            var numDeleted = await command.ExecuteNonQueryAsync();

            return numDeleted;
        }

        public async Task<int> DeleteOutdatedAttributesOlderThan(string layerID, IModelContext trans, DateTimeOffset threshold, TimeThreshold atTime)
        {
            var query = @"DELETE FROM attribute
	                WHERE timestamp < @delete_threshold
                    AND id NOT IN (
                        select distinct on(ci_id, name) id FROM attribute 
                        where timestamp <= @now and layer_id = @layer_id
                        order by ci_id, name, timestamp DESC NULLS LAST
                    )";

            using var command = new NpgsqlCommand(query, trans.DBConnection, trans.DBTransaction);

            var now = TimeThreshold.BuildLatest();
            command.Parameters.AddWithValue("delete_threshold", threshold);
            command.Parameters.AddWithValue("now", atTime.Time);
            command.Parameters.AddWithValue("layer_id", layerID);
            command.Prepare();

            var numDeleted = await command.ExecuteNonQueryAsync();

            return numDeleted;
        }
    }
}
