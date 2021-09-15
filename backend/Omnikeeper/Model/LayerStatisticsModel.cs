using Npgsql;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Threading.Tasks;

namespace Omnikeeper.Model
{
    public class LayerStatisticsModel : ILayerStatisticsModel
    {
        public async Task<long> GetActiveAttributes(Layer layer, IModelContext trans)
        {
            // return number of all active attributes
            // TODO: rework to use attribute_latest table
            using var commandActiveLayers = new NpgsqlCommand($@"
                SELECT COUNT(*)
                FROM 
                (
                    SELECT DISTINCT ON (ci_id, name) state
                    FROM attribute ATTR
                    WHERE ATTR.timestamp <= @time_threshold AND ATTR.layer_id = @layer_id
                    ORDER BY ATTR.ci_id, ATTR.name, ATTR.timestamp DESC NULLS LAST
                ) R
                WHERE R.state != 'removed'
            ", trans.DBConnection, trans.DBTransaction);

            commandActiveLayers.Parameters.AddWithValue("layer_id", layer.ID);
            commandActiveLayers.Parameters.AddWithValue("time_threshold", DateTimeOffset.Now);

            return (long)await commandActiveLayers.ExecuteScalarAsync();
        }

        public async Task<bool> IsLayerEmpty(Layer layer, IModelContext trans)
        {
            var numAttributes = await GetAttributeChangesHistory(layer, trans);
            if (numAttributes > 0) return false;
            var numRelations = await GetRelationChangesHistory(layer, trans);
            if (numRelations > 0) return false;
            return true;
        }

        public async Task<long> GetAttributeChangesHistory(Layer layer, IModelContext trans)
        {
            // return number of all historic attribute changes
            using var command = new NpgsqlCommand($@"
                SELECT COUNT(*) 
                FROM attribute ATT 
                WHERE ATT.timestamp <= @time_threshold AND ATT.layer_id = @layer_id

            ", trans.DBConnection, trans.DBTransaction);

            command.Parameters.AddWithValue("layer_id", layer.ID);
            command.Parameters.AddWithValue("time_threshold", DateTimeOffset.Now);

            return (long)await command.ExecuteScalarAsync();
        }

        public async Task<long> GetActiveRelations(Layer layer, IModelContext trans)
        {
            // return number of all active relations
            // TODO: rework to use attribute_latest table
            using var command = new NpgsqlCommand($@"
                SELECT COUNT(*)
                FROM(
                    SELECT DISTINCT ON (R.from_ci_id, R.to_ci_id, R.predicate_id) R.id, R.from_ci_id, R.to_ci_id, R.predicate_id, R.state, R.changeset_id  
                    FROM relation R
                    WHERE R.timestamp <= @time_threshold AND R.layer_id = @layer_id 
                    ORDER BY R.from_ci_id, R.to_ci_id, R.predicate_id, R.layer_id, R.timestamp DESC NULLS LAST
                ) RES
                WHERE RES.STATE != 'removed'
            ", trans.DBConnection, trans.DBTransaction);

            command.Parameters.AddWithValue("layer_id", layer.ID);
            command.Parameters.AddWithValue("time_threshold", DateTimeOffset.Now);

            return (long)await command.ExecuteScalarAsync();
        }

        public async Task<long> GetRelationChangesHistory(Layer layer, IModelContext trans)
        {
            // return number of all historic relation changes
            using var command = new NpgsqlCommand($@"
                SELECT COUNT(*)
                FROM relation R
                WHERE R.timestamp <= @time_threshold AND R.layer_id = @layer_id
            ", trans.DBConnection, trans.DBTransaction);

            command.Parameters.AddWithValue("layer_id", layer.ID);
            command.Parameters.AddWithValue("time_threshold", DateTimeOffset.Now);

            return (long)await command.ExecuteScalarAsync();
        }

        public async Task<long> GetLayerChangesetsHistory(Layer layer, IModelContext trans)
        {
            // return number of all historic changesets that affect this layer
            using var command = new NpgsqlCommand($@"
                SELECT COUNT(*)
                FROM
                (
                    SELECT DISTINCT ON (changeset_id) changeset_id
                    FROM
                    (   
                	    SELECT R.changeset_id
                	    FROM relation R 
                	    WHERE R.layer_id = @layer_id AND R.timestamp <= @time_threshold
                	    UNION ALL
                	    SELECT ATT.changeset_id
                	    FROM attribute ATT 
                	    WHERE ATT.layer_id = @layer_id AND ATT.timestamp <= @time_threshold
                    ) AA
                ) TT
            ", trans.DBConnection, trans.DBTransaction);

            command.Parameters.AddWithValue("layer_id", layer.ID);
            command.Parameters.AddWithValue("time_threshold", DateTimeOffset.Now);

            return (long)await command.ExecuteScalarAsync();
        }
    }
}
