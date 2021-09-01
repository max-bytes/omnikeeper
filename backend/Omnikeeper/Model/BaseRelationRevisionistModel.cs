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
            var query = @"delete from relation r where r.layer_id = @layer_id";

            using var command = new NpgsqlCommand(query, trans.DBConnection, trans.DBTransaction);
            command.Parameters.AddWithValue("layer_id", layerID);
            command.Prepare();

            var numDeleted = await command.ExecuteNonQueryAsync();

            return numDeleted;
        }

        public async Task<int> DeleteOutdatedRelationsOlderThan(string layerID, IModelContext trans, DateTimeOffset threshold, TimeThreshold atTime)
        {
            var query = @"DELETE FROM relation
	                WHERE timestamp < @delete_threshold
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
