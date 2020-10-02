using Keycloak.Net.Models.Key;
using Landscape.Base.Entity;
using Landscape.Base.Model;
using Npgsql;
using System;
using System.Threading.Tasks;

namespace LandscapeRegistry.Model
{
    public class LayerStatisticsModel : ILayerStatisticsModel
    {
        private readonly NpgsqlConnection conn;
        private readonly ILayerModel layerModel;

        public LayerStatisticsModel(NpgsqlConnection connection, ILayerModel layerModel)
        {
            conn = connection;
            this.layerModel = layerModel;
        }

        public async Task<long> GetActiveAttributes(Layer layer, NpgsqlTransaction trans)
        {
            // return number of all active attributes
            using var commandActiveLayers = new NpgsqlCommand($@"
                SELECT COUNT(*)
                FROM 
                (
                    SELECT DISTINCT ON (ci_id, name) state
                    FROM attribute ATTR
                    WHERE ATTR.timestamp <= @time_threshold AND ATTR.layer_id = @layer_id
                    ORDER BY ATTR.ci_id, ATTR.name, ATTR.timestamp DESC
                ) R
                WHERE R.state != 'removed'
            ", conn, trans);

            commandActiveLayers.Parameters.AddWithValue("layer_id", layer.ID);
            commandActiveLayers.Parameters.AddWithValue("time_threshold", DateTimeOffset.Now);

            return (long)await commandActiveLayers.ExecuteScalarAsync();
        }


        public async Task<long> GetAttributeChangesHistory(Layer layer, NpgsqlTransaction trans)
        {
            // return number of all historic attribute changes
            using var command = new NpgsqlCommand($@"
                SELECT COUNT(*) 
                FROM attribute ATT 
                WHERE ATT.timestamp <= @time_threshold AND ATT.layer_id = @layer_id

            ", conn, trans);

            command.Parameters.AddWithValue("layer_id", layer.ID);
            command.Parameters.AddWithValue("time_threshold", DateTimeOffset.Now);

            return (long)await command.ExecuteScalarAsync();
        }
        public async Task<long> GetActiveRelations(Layer layer, NpgsqlTransaction trans)
        {
            // return number of all active relations

            using var command = new NpgsqlCommand($@"
                SELECT COUNT(*)
                FROM(
                    SELECT DISTINCT ON (R.from_ci_id, R.to_ci_id, R.predicate_id) R.id, R.from_ci_id, R.to_ci_id, R.predicate_id, R.state, R.changeset_id  
                    FROM relation R
                    WHERE R.timestamp <= @time_threshold AND R.layer_id = @layer_id 
                    ORDER BY R.from_ci_id, R.to_ci_id, R.predicate_id, R.layer_id, R.timestamp DESC
                ) RES
                WHERE RES.STATE != 'removed'
            ", conn, trans);

            command.Parameters.AddWithValue("layer_id", layer.ID);
            command.Parameters.AddWithValue("time_threshold", DateTimeOffset.Now);

            return (long)await command.ExecuteScalarAsync();
        }

        public async Task<long> GetRelationChangesHistory(Layer layer, NpgsqlTransaction trans)
        {
            // return number of all historic relation changes

            using var command = new NpgsqlCommand($@"
                SELECT COUNT(*)
                FROM relation R
                WHERE R.timestamp <= @time_threshold AND R.layer_id = @layer_id
            ", conn, trans);

            command.Parameters.AddWithValue("layer_id", layer.ID);
            command.Parameters.AddWithValue("time_threshold", DateTimeOffset.Now);

            return (long)await command.ExecuteScalarAsync();
        }

        public async Task<long> GetLayerChangesetsHistory(Layer layer, NpgsqlTransaction trans)
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
            ", conn, trans);

            command.Parameters.AddWithValue("layer_id", layer.ID);
            command.Parameters.AddWithValue("time_threshold", DateTimeOffset.Now);

            return (long)await command.ExecuteScalarAsync();
        }
    }
}
