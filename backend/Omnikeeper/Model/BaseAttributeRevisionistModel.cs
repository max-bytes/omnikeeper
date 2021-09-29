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
            // TODO: this fails to consider relations with state "removed" that are present in the *_latest table, even when they are old enough

            // query inspired by https://stackoverflow.com/questions/15959061/delete-records-which-do-not-have-a-match-in-another-table
            var query = @"DELETE FROM attribute a
                    USING (SELECT a2.id FROM attribute a2 WHERE NOT EXISTS(SELECT * FROM attribute_latest l WHERE l.id = a2.id)) i
	                WHERE a.timestamp < @delete_threshold
                    AND a.layer_id = @layer_id
                    AND i.id = a.id";

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
