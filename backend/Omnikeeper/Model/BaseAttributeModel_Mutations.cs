using Npgsql;
using NpgsqlTypes;
using Omnikeeper.Base.AttributeValues;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Model
{
    public partial class BaseAttributeModel
    {
        // TODO: with the introduction of the latest table, consider using/enforcing a different transaction isolation level as the default "read committed" for modifications
        // ("repeatable read" may be needed?)

        public async Task<(bool changed, Guid changesetID)> BulkUpdate(
            IList<(Guid ciid, string fullName, IAttributeValue value, Guid? existingAttributeID, Guid newAttributeID)> inserts,
            IList<(Guid ciid, string name, IAttributeValue value, Guid attributeID, Guid newAttributeID)> removes,
            string layerID, DataOriginV1 origin, IChangesetProxy changesetProxy, IModelContext trans)
        {
            if (inserts.IsEmpty() && removes.IsEmpty())
                return (false, default);

            Changeset changeset = await changesetProxy.GetChangeset(layerID, origin, trans);

            var partitionIndex = await partitionModel.GetLatestPartitionIndex(changesetProxy.TimeThreshold, trans);

            // historic
            // inserts
            // use postgres COPY feature instead of manual inserts https://www.npgsql.org/doc/copy.html
            using var writerHistoric = trans.DBConnection.BeginBinaryImport(@"COPY attribute (id, name, ci_id, type, value_text, value_binary, value_control, layer_id, removed, ""timestamp"", changeset_id, partition_index) FROM STDIN (FORMAT BINARY)");
            foreach (var (ciid, fullName, value, _, newAttributeID) in inserts)
            {
                var (valueText, valueBinary, valueControl) = AttributeValueHelper.Marshal(value);

                writerHistoric.StartRow();
                writerHistoric.Write(newAttributeID);
                writerHistoric.Write(fullName);
                writerHistoric.Write(ciid);
                writerHistoric.Write(value.Type, "attributevaluetype");
                writerHistoric.Write(valueText);
                writerHistoric.Write(valueBinary);
                writerHistoric.Write(valueControl);
                writerHistoric.Write(layerID);
                writerHistoric.Write(false);
                writerHistoric.Write(changeset.Timestamp.ToUniversalTime(), NpgsqlDbType.TimestampTz);
                writerHistoric.Write(changeset.ID);
                writerHistoric.Write(partitionIndex, NpgsqlDbType.TimestampTz);
            }

            // removes 
            foreach (var (ciid, name, value, _, newAttributeID) in removes)
            {
                var (valueText, valueBinary, valueControl) = AttributeValueHelper.Marshal(value);

                writerHistoric.StartRow();
                writerHistoric.Write(newAttributeID);
                writerHistoric.Write(name);
                writerHistoric.Write(ciid);
                writerHistoric.Write(value.Type, "attributevaluetype");
                writerHistoric.Write(valueText);
                writerHistoric.Write(valueBinary);
                writerHistoric.Write(valueControl);
                writerHistoric.Write(layerID);
                writerHistoric.Write(true);
                writerHistoric.Write(changeset.Timestamp.ToUniversalTime(), NpgsqlDbType.TimestampTz);
                writerHistoric.Write(changeset.ID);
                writerHistoric.Write(partitionIndex, NpgsqlDbType.TimestampTz);
            }
            writerHistoric.Complete();
            writerHistoric.Close();


            // latest
            // new inserts
            // NOTE: actual new inserts are only those that have no existing attribute ID, which must be equivalent to NOT having an entry in the latest table
            // that allows us to do COPY insertion, because we guarantee that there are no unique constraint violations
            // should this ever throw a unique constraint violation, means there is a bug and _latest and _historic are out of sync
            var actualNewInserts = inserts.Where(t => t.existingAttributeID == null);
            if (!actualNewInserts.IsEmpty())
            {
                using var writerLatest = trans.DBConnection.BeginBinaryImport(@"COPY attribute_latest (id, name, ci_id, type, value_text, value_binary, value_control, layer_id, changeset_id) FROM STDIN (FORMAT BINARY)");
                foreach (var (ciid, fullName, value, _, newAttributeID) in actualNewInserts)
                {
                    var (valueText, valueBinary, valueControl) = AttributeValueHelper.Marshal(value);
                    writerLatest.StartRow();
                    writerLatest.Write(newAttributeID);
                    writerLatest.Write(fullName);
                    writerLatest.Write(ciid);
                    writerLatest.Write(value.Type, "attributevaluetype");
                    writerLatest.Write(valueText);
                    writerLatest.Write(valueBinary);
                    writerLatest.Write(valueControl);
                    writerLatest.Write(layerID);
                    writerLatest.Write(changeset.ID);
                }
                writerLatest.Complete();
                writerLatest.Close();
            }

            // updates (actual updates and removals)
            // TODO: improve performance
            // add index, use CTEs
            var actualModified = inserts.Where(t => t.existingAttributeID != null);
            foreach (var (ciid, fullName, value, existingAttributeID, newAttributeID) in actualModified)
            {
                using var commandUpdateLatest = new NpgsqlCommand(@"
                    UPDATE attribute_latest SET id = @id, type = @type, value_text = @value_text, value_binary = @value_binary, 
                    value_control = @value_control, changeset_id = @changeset_id
                    WHERE id = @old_id", trans.DBConnection, trans.DBTransaction);
                var (valueText, valueBinary, valueControl) = AttributeValueHelper.Marshal(value);
                commandUpdateLatest.Parameters.AddWithValue("id", newAttributeID);
                commandUpdateLatest.Parameters.AddWithValue("old_id", existingAttributeID!);
                commandUpdateLatest.Parameters.AddWithValue("type", value.Type);
                commandUpdateLatest.Parameters.AddWithValue("value_text", valueText);
                commandUpdateLatest.Parameters.AddWithValue("value_binary", valueBinary);
                commandUpdateLatest.Parameters.AddWithValue("value_control", valueControl);
                commandUpdateLatest.Parameters.AddWithValue("changeset_id", changeset.ID);
                await commandUpdateLatest.ExecuteNonQueryAsync();
            }

            if (!removes.IsEmpty())
            {
                using var commandRemoveLatest = new NpgsqlCommand(@$"
                        WITH to_remove(attribute_id) AS (VALUES {string.Join(",", removes.Select(t => $"('{t.attributeID}'::uuid)"))})
                        DELETE FROM attribute_latest WHERE id IN (select attribute_id from to_remove)", trans.DBConnection, trans.DBTransaction);
                await commandRemoveLatest.ExecuteNonQueryAsync();
            }

            return (true, changeset.ID);
        }
    }
}
