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
            // TODO: this fails to consider relations with state "removed"!
            // and it also does not affect the relation_latest table, which it SHOULD affect in case of removed relations
            // TODO: use latest table
            var query = @"DELETE FROM relation
	                WHERE timestamp < @delete_threshold
                    AND layer_id = @layer_id
                    AND id NOT IN (
                        select distinct on(from_ci_id, to_ci_id, predicate_id) id FROM relation 
                        where timestamp <= @now and layer_id = @layer_id
                        order by from_ci_id, to_ci_id, predicate_id, layer_id, timestamp DESC NULLS LAST
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
