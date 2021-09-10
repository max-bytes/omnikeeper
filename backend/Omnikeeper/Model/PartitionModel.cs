using Npgsql;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Threading.Tasks;

namespace Omnikeeper.Model
{
    public class PartitionModel : IPartitionModel
    {
        public async Task<DateTimeOffset> GetLatestPartitionIndex(TimeThreshold timeThreshold, IModelContext trans)
        {
            using var command = new NpgsqlCommand(@"SELECT max(partition_index) FROM partition WHERE partition_index <= @timestamp", trans.DBConnection, trans.DBTransaction);
            command.Parameters.AddWithValue("timestamp", timeThreshold.Time);
            command.Prepare();
            var pi = (DateTime)await command.ExecuteScalarAsync();
            return pi;
        }

        public async Task StartNewPartition(TimeThreshold timeThreshold, IModelContext trans)
        {
            // TODO: think more about transaction / isolation / locking

            // get old partition_index
            var oldPartitionIndex = await GetLatestPartitionIndex(timeThreshold, trans);

            var partitionIndexValue = timeThreshold.Time.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ssZ");

            // create partition entry
            using var commandPartitionEntry = new NpgsqlCommand(@$"
            INSERT INTO public.partition(partition_index) VALUES('{partitionIndexValue}'::timestamptz) RETURNING partition_index", trans.DBConnection, trans.DBTransaction);
            //commandPartitionEntry.Parameters.AddWithValue("timestamp", timeThreshold.Time);
            var newPartitionIndex = (DateTime)await commandPartitionEntry.ExecuteScalarAsync();
            if (newPartitionIndex == null)
                throw new Exception($"Could not insert new partition entry with partition_index {timeThreshold.Time}");

            var partitionTableSuffix = timeThreshold.Time.ToUniversalTime().ToString("yyyy_MM_dd_HH_mm_ss");

            // create partition table for attributes
            var partitionTableNameAttributes = $"public.attribute_{partitionTableSuffix}";
            var tmp = @$"CREATE TABLE {partitionTableNameAttributes} PARTITION OF public.attribute FOR VALUES IN ('{partitionIndexValue}'::timestamptz)";
            using var commandPartitionTableAttributes = new NpgsqlCommand(tmp,
                trans.DBConnection, trans.DBTransaction);
            // HACK: we would love to use a bind parameter when specifying the partition criteria, but postgres/npgsql does not like this
            //commandPartitionTableAttributes.Parameters.AddWithValue("timestamp", new DateTimeOffset[] { timeThreshold.Time });
            await commandPartitionTableAttributes.ExecuteNonQueryAsync();

            // move attributes over
            using var commandMoveAttributes = new NpgsqlCommand($@"
            UPDATE attribute SET partition_index = @new_partition_index
            FROM (
                select distinct on(ci_id, name, layer_id) id FROM attribute 
                where timestamp <= @time_threshold and partition_index = @old_partition_index
                order by ci_id, name, layer_id, timestamp DESC NULLS LAST) AS sub
            WHERE attribute.id = sub.id", trans.DBConnection, trans.DBTransaction);
            commandMoveAttributes.Parameters.AddWithValue("time_threshold", timeThreshold.Time);
            commandMoveAttributes.Parameters.AddWithValue("old_partition_index", oldPartitionIndex);
            commandMoveAttributes.Parameters.AddWithValue("new_partition_index", newPartitionIndex.ToUniversalTime());
            await commandMoveAttributes.ExecuteNonQueryAsync();

            // create partition table for relations
            var partitionTableNameRelations = $"public.relation_{partitionTableSuffix}";
            using var commandPartitionTableRelations = new NpgsqlCommand(@$"
                CREATE TABLE {partitionTableNameRelations} PARTITION OF public.relation FOR VALUES IN('{partitionIndexValue}'::timestamptz)",
                trans.DBConnection, trans.DBTransaction);
            // HACK: we would love to use a bind parameter when specifying the partition criteria, but postgres/npgsql does not like this
            //commandPartitionTableRelations.Parameters.AddWithValue("timestamp", timeThreshold.Time);
            await commandPartitionTableRelations.ExecuteNonQueryAsync();

            // move relations over
            using var commandMoveRelations = new NpgsqlCommand($@"
            UPDATE relation SET partition_index = @new_partition_index
            FROM (
                select distinct on (from_ci_id, to_ci_id, predicate_id, layer_id) id from relation 
                where timestamp <= @time_threshold and partition_index = @old_partition_index
                order by from_ci_id, to_ci_id, predicate_id, layer_id, timestamp DESC NULLS LAST) AS sub
            WHERE relation.id = sub.id", trans.DBConnection, trans.DBTransaction);
            commandMoveRelations.Parameters.AddWithValue("time_threshold", timeThreshold.Time);
            commandMoveRelations.Parameters.AddWithValue("old_partition_index", oldPartitionIndex);
            commandMoveRelations.Parameters.AddWithValue("new_partition_index", newPartitionIndex.ToUniversalTime());
            await commandMoveRelations.ExecuteNonQueryAsync();
        }
    }
}
