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
            if (layerID != null)
            {
                using var command = new NpgsqlCommand($@"
                    SELECT COUNT(*)
                    FROM attribute_latest
                    WHERE layer_id = @layer_id", trans.DBConnection, trans.DBTransaction);
                command.Parameters.AddWithValue("layer_id", layerID);
                return ((long?)await command.ExecuteScalarAsync())!.Value;
            } 
            else
            {
                using var command = new NpgsqlCommand(CreateCountQuery("attribute_latest"), trans.DBConnection, trans.DBTransaction);
                return ((long?)await command.ExecuteScalarAsync())!.Value;
            }
            
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
            if (layerID != null)
            {
                using var command = new NpgsqlCommand($@"
                    SELECT COUNT(*) 
                    FROM attribute ATT 
                    WHERE ATT.layer_id = @layer_id", trans.DBConnection, trans.DBTransaction);
                command.Parameters.AddWithValue("layer_id", layerID);
                return ((long?)await command.ExecuteScalarAsync())!.Value;
            } 
            else
            {
                using var command = new NpgsqlCommand(CreateCountQueryForPartitionedTable("attribute"), trans.DBConnection, trans.DBTransaction);
                return ((long?)await command.ExecuteScalarAsync())!.Value;
            }
        }

        public async Task<long> GetActiveRelations(string? layerID, IModelContext trans)
        {
            // return number of all active relations
            if (layerID != null)
            {
                using var command = new NpgsqlCommand($@"
                SELECT count(*)
                FROM relation_latest
                WHERE layer_id = @layer_id", trans.DBConnection, trans.DBTransaction);
                command.Parameters.AddWithValue("layer_id", layerID);
                command.Parameters.AddWithValue("time_threshold", DateTimeOffset.Now);
                return ((long?)await command.ExecuteScalarAsync())!.Value;
            } 
            else
            {
                using var command = new NpgsqlCommand(CreateCountQuery("relation_latest"), trans.DBConnection, trans.DBTransaction);
                return ((long?)await command.ExecuteScalarAsync())!.Value;
            }
        }

        public async Task<long> GetRelationChangesHistory(string? layerID, IModelContext trans)
        {
            // return number of all historic relation changes
            if (layerID != null)
            {
                using var command = new NpgsqlCommand($@"
                    SELECT COUNT(*)
                    FROM relation R
                    WHERE R.layer_id = @layer_id", trans.DBConnection, trans.DBTransaction);
                command.Parameters.AddWithValue("layer_id", layerID);
                command.Parameters.AddWithValue("time_threshold", DateTimeOffset.Now);
                return ((long?)await command.ExecuteScalarAsync())!.Value;
            } 
            else
            {
                using var command = new NpgsqlCommand(CreateCountQueryForPartitionedTable("relation"), trans.DBConnection, trans.DBTransaction);
                return ((long?)await command.ExecuteScalarAsync())!.Value;
            }
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
            //var query = $@"select count(id) from ci";
            var query = CreateCountQuery("ci");
            using var command = new NpgsqlCommand(query, trans.DBConnection, trans.DBTransaction);

            var d = await command.ExecuteScalarAsync();
            return ((long?)d)!.Value;
        }

        private string CreateCountQuery(string tableName)
        {
            // much faster, but inaccurate
            // taken from https://stackoverflow.com/a/7945274
            return $@"SELECT(CASE WHEN c.reltuples < 0 THEN NULL
                         WHEN c.relpages = 0 THEN float8 '0'
                         ELSE c.reltuples / c.relpages END
                 * (pg_catalog.pg_relation_size(c.oid)
                  / pg_catalog.current_setting('block_size')::int)
                   )::bigint
            FROM   pg_catalog.pg_class c
            WHERE c.oid = '{tableName}'::regclass";
        }

        private string CreateCountQueryForPartitionedTable(string tableName)
        {
            // taken from https://stackoverflow.com/a/7945274 and adapted for partitioned table
            return $@"SELECT SUM(i.tableSize)::bigint FROM(
                SELECT(CASE WHEN c.reltuples < 0 THEN NULL
                         WHEN c.relpages = 0 THEN float8 '0'
                         ELSE c.reltuples / c.relpages END
                 * (pg_catalog.pg_relation_size(c.oid)
                  / pg_catalog.current_setting('block_size')::int)
                   )::bigint tableSize
                FROM   pg_catalog.pg_class c

                WHERE c.relname LIKE '{tableName}%' AND c.relispartition = true AND c.relkind = 'r'
            ) i";
        }
    }
}
