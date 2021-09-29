using Npgsql;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Threading.Tasks;

namespace Omnikeeper.Model
{
    public class BaseRelationRevisionistModel : IBaseRelationRevisionistModel
    {
        public async Task<int> DeleteAllRelations(string layerID, IModelContext trans)
        {
            using var commandLatest = new NpgsqlCommand(@"delete from relation_latest r where r.layer_id = @layer_id", trans.DBConnection, trans.DBTransaction);
            commandLatest.Parameters.AddWithValue("layer_id", layerID);
            commandLatest.Prepare();
            await commandLatest.ExecuteNonQueryAsync();

            using var commandHistoric = new NpgsqlCommand(@"delete from relation r where r.layer_id = @layer_id", trans.DBConnection, trans.DBTransaction);
            commandHistoric.Parameters.AddWithValue("layer_id", layerID);
            commandHistoric.Prepare();
            var numDeleted = await commandHistoric.ExecuteNonQueryAsync();

            return numDeleted;
        }

        public async Task<int> DeleteOutdatedRelationsOlderThan(string layerID, IModelContext trans, DateTimeOffset threshold, TimeThreshold atTime)
        {
            // TODO: this fails to consider relations with state "removed" that are present in the *_latest table, even when they are old enough

            // query inspired by https://stackoverflow.com/questions/15959061/delete-records-which-do-not-have-a-match-in-another-table
            var query = @"DELETE FROM relation r
                    USING (SELECT r2.id FROM relation r2 WHERE NOT EXISTS(SELECT * FROM relation_latest l WHERE l.id = r2.id)) i
	                WHERE r.timestamp < @delete_threshold
                    AND r.layer_id = @layer_id
                    AND i.id = r.id";

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
