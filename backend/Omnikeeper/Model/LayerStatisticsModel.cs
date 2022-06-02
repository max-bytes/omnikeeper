using Npgsql;
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
            using var commandActiveLayers = new NpgsqlCommand($@"
                SELECT COUNT(*)
                FROM attribute_latest
                WHERE {((layerID != null) ? "layer_id = @layer_id" : "1=1")}
            ", trans.DBConnection, trans.DBTransaction);

            if (layerID != null)
                commandActiveLayers.Parameters.AddWithValue("layer_id", layerID);
            commandActiveLayers.Parameters.AddWithValue("time_threshold", DateTimeOffset.Now);

            return ((long?)await commandActiveLayers.ExecuteScalarAsync())!.Value;
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

            return ((long?)await command.ExecuteScalarAsync())!.Value;
        }

        public async Task<long> GetActiveRelations(string? layerID, IModelContext trans)
        {
            // return number of all active relations
            using var command = new NpgsqlCommand($@"
                SELECT count(*)
                FROM relation_latest
                WHERE {((layerID != null) ? "layer_id = @layer_id" : "1=1")}
            ", trans.DBConnection, trans.DBTransaction);

            if (layerID != null)
                command.Parameters.AddWithValue("layer_id", layerID);
            command.Parameters.AddWithValue("time_threshold", DateTimeOffset.Now);

            return ((long?)await command.ExecuteScalarAsync())!.Value;
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

            return ((long?)await command.ExecuteScalarAsync())!.Value;
        }

        public async Task<long> GetLayerChangesetsHistory(string layerID, IModelContext trans)
        {
            // return number of all historic changesets that affect this layer
            using var command = new NpgsqlCommand($@"
                SELECT COUNT(*)
                FROM changeset 
                WHERE layer_id = @layer_id
            ", trans.DBConnection, trans.DBTransaction);

            command.Parameters.AddWithValue("layer_id", layerID);
            command.Parameters.AddWithValue("time_threshold", DateTimeOffset.Now);

            return ((long?)await command.ExecuteScalarAsync())!.Value;
        }

        public async Task<long> GetCIIDs(IModelContext trans)
        {
            // return number of ciids
            using var command = new NpgsqlCommand($@"select count(id) from ci", trans.DBConnection, trans.DBTransaction);

            return ((long?)await command.ExecuteScalarAsync())!.Value;
        }
    }
}
