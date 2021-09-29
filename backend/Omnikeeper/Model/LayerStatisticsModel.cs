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
        public async Task<long> GetActiveAttributes(string? layerID, IModelContext trans)
        {
            // return number of all active attributes
            // TODO: rework to use attribute_latest table
            using var commandActiveLayers = new NpgsqlCommand($@"
                SELECT COUNT(*)
                FROM 
                (
                    SELECT DISTINCT ON (ci_id, name, layer_id) removed
                    FROM attribute ATTR
                    WHERE ATTR.timestamp <= @time_threshold AND {((layerID != null) ? "ATTR.layer_id = @layer_id" : "1=1")}
                    ORDER BY ATTR.ci_id, ATTR.name, ATTR.layer_id, ATTR.timestamp DESC NULLS LAST
                ) R
                WHERE R.removed = false
            ", trans.DBConnection, trans.DBTransaction);

            if (layerID != null)
                commandActiveLayers.Parameters.AddWithValue("layer_id", layerID);
            commandActiveLayers.Parameters.AddWithValue("time_threshold", DateTimeOffset.Now);

            return (long)await commandActiveLayers.ExecuteScalarAsync();
        }

        public async Task<bool> IsLayerEmpty(string layerID, IModelContext trans)
        {
            var numAttributes = await GetAttributeChangesHistory(layerID, trans);
            if (numAttributes > 0) return false;
            var numRelations = await GetRelationChangesHistory(layerID, trans);
            if (numRelations > 0) return false;
            return true;
        }

        public async Task<long> GetAttributeChangesHistory(string? layerID, IModelContext trans)
        {
            // return number of all historic attribute changes
            using var command = new NpgsqlCommand($@"
                SELECT COUNT(*) 
                FROM attribute ATT 
                WHERE ATT.timestamp <= @time_threshold AND {((layerID != null) ? "ATT.layer_id = @layer_id" : "1=1")}

            ", trans.DBConnection, trans.DBTransaction);

            if (layerID != null)
                command.Parameters.AddWithValue("layer_id", layerID);
            command.Parameters.AddWithValue("time_threshold", DateTimeOffset.Now);

            return (long)await command.ExecuteScalarAsync();
        }

        public async Task<long> GetActiveRelations(string? layerID, IModelContext trans)
        {
            // return number of all active relations
            // TODO: rework to use attribute_latest table
            using var command = new NpgsqlCommand($@"
                SELECT COUNT(*)
                FROM(
                    SELECT DISTINCT ON (R.from_ci_id, R.to_ci_id, R.predicate_id, R.layer_id) R.id, R.from_ci_id, R.to_ci_id, R.predicate_id, R.removed, R.changeset_id  
                    FROM relation R
                    WHERE R.timestamp <= @time_threshold AND {((layerID != null) ? "R.layer_id = @layer_id" : "1=1")}
                    ORDER BY R.from_ci_id, R.to_ci_id, R.predicate_id, R.layer_id, R.timestamp DESC NULLS LAST
                ) RES
                WHERE RES.removed = false
            ", trans.DBConnection, trans.DBTransaction);

            if (layerID != null)
                command.Parameters.AddWithValue("layer_id", layerID);
            command.Parameters.AddWithValue("time_threshold", DateTimeOffset.Now);

            return (long)await command.ExecuteScalarAsync();
        }

        public async Task<long> GetRelationChangesHistory(string? layerID, IModelContext trans)
        {
            // return number of all historic relation changes
            using var command = new NpgsqlCommand($@"
                SELECT COUNT(*)
                FROM relation R
                WHERE R.timestamp <= @time_threshold AND {((layerID != null) ? "R.layer_id = @layer_id" : "1=1")}
            ", trans.DBConnection, trans.DBTransaction);

            if (layerID != null)
                command.Parameters.AddWithValue("layer_id", layerID);
            command.Parameters.AddWithValue("time_threshold", DateTimeOffset.Now);

            return (long)await command.ExecuteScalarAsync();
        }

        public async Task<long> GetLayerChangesetsHistory(string layerID, IModelContext trans)
        {
            // TODO: changeset has layerID stored themselves now too, switch over to this, is more performant

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

            command.Parameters.AddWithValue("layer_id", layerID);
            command.Parameters.AddWithValue("time_threshold", DateTimeOffset.Now);

            return (long)await command.ExecuteScalarAsync();
        }
    }
}
