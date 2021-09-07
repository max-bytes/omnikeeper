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
            using var commandLatest = new NpgsqlCommand(@"delete from attribute_latest a where a.layer_id = @layer_id", trans.DBConnection, trans.DBTransaction);
            commandLatest.Parameters.AddWithValue("layer_id", layerID);
            commandLatest.Prepare();
            await commandLatest.ExecuteNonQueryAsync();

            using var commandHistoric = new NpgsqlCommand(@"delete from attribute a where a.layer_id = @layer_id", trans.DBConnection, trans.DBTransaction);
            commandHistoric.Parameters.AddWithValue("layer_id", layerID);
            commandHistoric.Prepare();
            var numDeleted = await commandHistoric.ExecuteNonQueryAsync();

            return numDeleted;
        }

        public async Task<int> DeleteOutdatedAttributesOlderThan(string layerID, IModelContext trans, DateTimeOffset threshold, TimeThreshold atTime)
        {
            // TODO: this fails to consider attributes with state "removed"!
            // and it also does not affect the attribute_latest table, which it SHOULD affect in case of removed attributes
            // TODO: use latest table
            var query = @"DELETE FROM attribute
	                WHERE timestamp < @delete_threshold
                    AND layer_id = @layer_id
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
