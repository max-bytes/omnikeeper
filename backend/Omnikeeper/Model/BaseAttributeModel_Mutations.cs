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

        public async Task<(CIAttribute attribute, bool changed)> InsertAttribute(string name, IAttributeValue value, Guid ciid, string layerID, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        {
            var readTS = TimeThreshold.BuildLatest();
            var currentAttribute = await GetAttribute(name, ciid, layerID, trans, readTS);

            var state = AttributeState.New;
            if (currentAttribute != null)
            {
                if (currentAttribute.State == AttributeState.Removed)
                    state = AttributeState.Renewed;
                else
                    state = AttributeState.Changed;
            }

            // handle equality case
            // which user it is does not make any difference; if the data is the same, no insert is made
            // the origin also does not make a difference... TODO: think about that! Is this correct?
            if (currentAttribute != null && currentAttribute.State != AttributeState.Removed && currentAttribute.Value.Equals(value))
                return (currentAttribute, false);

            var changeset = await changesetProxy.GetChangeset(layerID, origin, trans);
            var partitionIndex = await partitionModel.GetLatestPartitionIndex(changesetProxy.TimeThreshold, trans);

            var (valueText, valueBinary, valueControl) = AttributeValueBuilder.Marshal(value);
            var id = Guid.NewGuid();

            using var commandHistoric = new NpgsqlCommand(@"INSERT INTO attribute (id, name, ci_id, type, value_text, value_binary, value_control, layer_id, state, ""timestamp"", changeset_id, partition_index) 
                VALUES (@id, @name, @ci_id, @type, @value_text, @value_binary, @value_control, @layer_id, @state, @timestamp, @changeset_id, @partition_index)", trans.DBConnection, trans.DBTransaction);
            commandHistoric.Parameters.AddWithValue("id", id);
            commandHistoric.Parameters.AddWithValue("name", name);
            commandHistoric.Parameters.AddWithValue("ci_id", ciid);
            commandHistoric.Parameters.AddWithValue("type", value.Type);
            commandHistoric.Parameters.AddWithValue("value_text", valueText);
            commandHistoric.Parameters.AddWithValue("value_binary", valueBinary);
            commandHistoric.Parameters.AddWithValue("value_control", valueControl);
            commandHistoric.Parameters.AddWithValue("layer_id", layerID);
            commandHistoric.Parameters.AddWithValue("state", state);
            commandHistoric.Parameters.AddWithValue("timestamp", changeset.Timestamp);
            commandHistoric.Parameters.AddWithValue("changeset_id", changeset.ID);
            commandHistoric.Parameters.AddWithValue("partition_index", partitionIndex);
            await commandHistoric.ExecuteNonQueryAsync();

            using var commandLatest = new NpgsqlCommand(@"
                INSERT INTO attribute_latest (id, name, ci_id, type, value_text, value_binary, value_control, layer_id, state, ""timestamp"", changeset_id) 
                VALUES (@id, @name, @ci_id, @type, @value_text, @value_binary, @value_control, @layer_id, @state, @timestamp, @changeset_id)
                ON CONFLICT ON CONSTRAINT name_ci_id_layer_id DO UPDATE SET id = EXCLUDED.id, type = EXCLUDED.type, value_text = EXCLUDED.value_text, value_binary = EXCLUDED.value_binary, 
                value_control = EXCLUDED.value_control, state = EXCLUDED.state, ""timestamp"" = EXCLUDED.""timestamp"", changeset_id = EXCLUDED.changeset_id", trans.DBConnection, trans.DBTransaction);
            commandLatest.Parameters.AddWithValue("id", id);
            commandLatest.Parameters.AddWithValue("name", name);
            commandLatest.Parameters.AddWithValue("ci_id", ciid);
            commandLatest.Parameters.AddWithValue("type", value.Type);
            commandLatest.Parameters.AddWithValue("value_text", valueText);
            commandLatest.Parameters.AddWithValue("value_binary", valueBinary);
            commandLatest.Parameters.AddWithValue("value_control", valueControl);
            commandLatest.Parameters.AddWithValue("layer_id", layerID);
            commandLatest.Parameters.AddWithValue("state", state);
            commandLatest.Parameters.AddWithValue("timestamp", changeset.Timestamp);
            commandLatest.Parameters.AddWithValue("changeset_id", changeset.ID);
            await commandLatest.ExecuteNonQueryAsync();

            return (new CIAttribute(id, name, ciid, value, state, changeset.ID), true);
        }

        public async Task<(CIAttribute attribute, bool changed)> RemoveAttribute(string name, Guid ciid, string layerID, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        {
            var readTS = TimeThreshold.BuildLatest();
            var currentAttribute = await GetAttribute(name, ciid, layerID, trans, readTS);

            if (currentAttribute == null)
            {
                // attribute does not exist
                throw new Exception("Trying to remove attribute that does not exist");
            }
            if (currentAttribute.State == AttributeState.Removed)
            {
                // the attribute is already removed, no-op(?)
                return (currentAttribute, false);
            }

            var changeset = await changesetProxy.GetChangeset(layerID, origin, trans);
            var partitionIndex = await partitionModel.GetLatestPartitionIndex(changesetProxy.TimeThreshold, trans);
            var (valueText, valueBinary, valueControl) = AttributeValueBuilder.Marshal(currentAttribute.Value);
            var id = Guid.NewGuid();

            using var commandHistoric = new NpgsqlCommand(@"INSERT INTO attribute (id, name, ci_id, type, value_text, value_binary, value_control, layer_id, state, ""timestamp"", changeset_id, partition_index) 
                VALUES (@id, @name, @ci_id, @type, @value_text, @value_binary, @value_control, @layer_id, @state, @timestamp, @changeset_id, @partition_index)", trans.DBConnection, trans.DBTransaction);
            commandHistoric.Parameters.AddWithValue("id", id);
            commandHistoric.Parameters.AddWithValue("name", name);
            commandHistoric.Parameters.AddWithValue("ci_id", ciid);
            commandHistoric.Parameters.AddWithValue("type", currentAttribute.Value.Type);
            commandHistoric.Parameters.AddWithValue("value_text", valueText);
            commandHistoric.Parameters.AddWithValue("value_binary", valueBinary);
            commandHistoric.Parameters.AddWithValue("value_control", valueControl);
            commandHistoric.Parameters.AddWithValue("layer_id", layerID);
            commandHistoric.Parameters.AddWithValue("state", AttributeState.Removed);
            commandHistoric.Parameters.AddWithValue("timestamp", changeset.Timestamp);
            commandHistoric.Parameters.AddWithValue("changeset_id", changeset.ID);
            commandHistoric.Parameters.AddWithValue("partition_index", partitionIndex);
            await commandHistoric.ExecuteNonQueryAsync();

            using var commandLatest = new NpgsqlCommand(@"
                UPDATE attribute_latest SET id = @id, state = @state, ""timestamp"" = @timestamp, changeset_id = @changeset_id WHERE id = @old_id", trans.DBConnection, trans.DBTransaction);
            commandLatest.Parameters.AddWithValue("id", id);
            commandLatest.Parameters.AddWithValue("old_id", currentAttribute.ID);
            commandLatest.Parameters.AddWithValue("state", AttributeState.Removed);
            commandLatest.Parameters.AddWithValue("timestamp", changeset.Timestamp);
            commandLatest.Parameters.AddWithValue("changeset_id", changeset.ID);
            await commandLatest.ExecuteNonQueryAsync();

            var ret = new CIAttribute(id, name, ciid, currentAttribute.Value, AttributeState.Removed, changeset.ID);

            return (ret, true);
        }

        // NOTE: this bulk operation DOES check if the attributes that are inserted are "unique":
        // it is not possible to insert the "same" attribute (same ciid, name and layer) multiple times
        // if this operation detects a duplicate, an exception is thrown;
        // the caller is responsible for making sure there are no duplicates
        public async Task<IEnumerable<(Guid ciid, string fullName)>> BulkReplaceAttributes<F>(IBulkCIAttributeData<F> data, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        {
            var readTS = TimeThreshold.BuildLatest();

            // consider ALL relevant attributes as outdated first
            var outdatedAttributes = (data switch
            { // TODO: performance improvements when data.NamePrefix is empty?
                BulkCIAttributeDataLayerScope d => (await FindAttributesByName($"^{data.NamePrefix}", new AllCIIDsSelection(), data.LayerID, returnRemoved: true, trans, readTS)),
                BulkCIAttributeDataCIScope d => (await FindAttributesByName($"^{data.NamePrefix}", SpecificCIIDsSelection.Build(d.CIID), data.LayerID, returnRemoved: true, trans, readTS)),
                _ => null
            }).ToDictionary(a => a.InformationHash, attribute => (attribute, Guid.NewGuid()));

            var actualInserts = new List<(Guid ciid, string fullName, IAttributeValue value, AttributeState state, Guid attributeID, Guid? existingAttributeID)>();
            var informationHashesToInsert = new HashSet<string>();
            foreach (var fragment in data.Fragments)
            {
                var fullName = data.GetFullName(fragment);
                var ciid = data.GetCIID(fragment);
                var value = data.GetValue(fragment);

                var informationHash = CIAttribute.CreateInformationHash(fullName, ciid);
                if (informationHashesToInsert.Contains(informationHash))
                {
                    throw new Exception($"Duplicate attribute fragment detected! Bulk insertion does not support duplicate attributes; attribute name: {fullName}, ciid: {ciid}");
                }
                informationHashesToInsert.Add(informationHash);

                // remove the current attribute from the list of attributes to remove
                outdatedAttributes.Remove(informationHash, out var currentAttribute);

                var state = AttributeState.New;
                if (currentAttribute.attribute != null)
                {
                    if (currentAttribute.attribute.State == AttributeState.Removed)
                        state = AttributeState.Renewed;
                    else
                        state = AttributeState.Changed;
                }

                // handle equality case, also think about what should happen if a different user inserts the same data
                if (currentAttribute.attribute != null && currentAttribute.attribute.State != AttributeState.Removed && currentAttribute.attribute.Value.Equals(value))
                    continue;

                var attributeID = Guid.NewGuid();
                actualInserts.Add((ciid, fullName, value, state, attributeID, currentAttribute.attribute?.ID));
            }

            // the list of outdatedAttributes now contains only attributes that need to be removed
            // BUT: the list of outdatedAttributes also can contain attributes whose state == "removed"
            // those cases we can ignore because they do not need to be removed anymore, so we remove them from the list too
            outdatedAttributes = outdatedAttributes.Where(t => t.Value.attribute.State != AttributeState.Removed).ToDictionary(t => t.Key, t => t.Value);

            // changeset is only created and copy mode is only entered when there is actually anything inserted
            if (!actualInserts.IsEmpty() || !outdatedAttributes.IsEmpty())
            {
                Changeset changeset = await changesetProxy.GetChangeset(data.LayerID, origin, trans);

                var partitionIndex = await partitionModel.GetLatestPartitionIndex(changesetProxy.TimeThreshold, trans);

                // historic
                // use postgres COPY feature instead of manual inserts https://www.npgsql.org/doc/copy.html
                using var writerHistoric = trans.DBConnection.BeginBinaryImport(@"COPY attribute (id, name, ci_id, type, value_text, value_binary, value_control, layer_id, state, ""timestamp"", changeset_id, partition_index) FROM STDIN (FORMAT BINARY)");
                foreach (var (ciid, fullName, value, state, newAttributeID, _) in actualInserts)
                {
                    var (valueText, valueBinary, valueControl) = AttributeValueBuilder.Marshal(value);

                    writerHistoric.StartRow();
                    writerHistoric.Write(newAttributeID);
                    writerHistoric.Write(fullName);
                    writerHistoric.Write(ciid);
                    writerHistoric.Write(value.Type, "attributevaluetype");
                    writerHistoric.Write(valueText);
                    writerHistoric.Write(valueBinary);
                    writerHistoric.Write(valueControl);
                    writerHistoric.Write(data.LayerID);
                    writerHistoric.Write(state, "attributestate");
                    writerHistoric.Write(changeset.Timestamp, NpgsqlDbType.TimestampTz);
                    writerHistoric.Write(changeset.ID);
                    writerHistoric.Write(partitionIndex, NpgsqlDbType.TimestampTz);
                }

                // remove outdated 
                foreach (var (outdatedAttribute, newAttributeID) in outdatedAttributes.Values)
                {
                    var (valueText, valueBinary, valueControl) = AttributeValueBuilder.Marshal(outdatedAttribute.Value);

                    writerHistoric.StartRow();
                    writerHistoric.Write(newAttributeID);
                    writerHistoric.Write(outdatedAttribute.Name);
                    writerHistoric.Write(outdatedAttribute.CIID);
                    writerHistoric.Write(outdatedAttribute.Value.Type, "attributevaluetype");
                    writerHistoric.Write(valueText);
                    writerHistoric.Write(valueBinary);
                    writerHistoric.Write(valueControl);
                    writerHistoric.Write(data.LayerID);
                    writerHistoric.Write(AttributeState.Removed, "attributestate");
                    writerHistoric.Write(changeset.Timestamp, NpgsqlDbType.TimestampTz);
                    writerHistoric.Write(changeset.ID);
                    writerHistoric.Write(partitionIndex, NpgsqlDbType.TimestampTz);
                }
                writerHistoric.Complete();
                writerHistoric.Close();


                // latest
                // new inserts
                // NOTE: actual new inserts are only those that have a state == new, which must be equivalent to NOT having an entry in the latest table
                // that allows us to do COPY insertion, because we guarantee that there are no unique constraint violations
                // should this ever throw a unique constraint violation, means there is a bug and _latest and _historic are out of sync
                var actualNewInserts = actualInserts.Where(t => t.state == AttributeState.New);
                if (!actualNewInserts.IsEmpty())
                {
                    using var writerLatest = trans.DBConnection.BeginBinaryImport(@"COPY attribute_latest (id, name, ci_id, type, value_text, value_binary, value_control, layer_id, state, ""timestamp"", changeset_id) FROM STDIN (FORMAT BINARY)");
                    foreach (var (ciid, fullName, value, state, newAttributeID, _) in actualNewInserts)
                    {
                        var (valueText, valueBinary, valueControl) = AttributeValueBuilder.Marshal(value);
                        writerLatest.StartRow();
                        writerLatest.Write(newAttributeID);
                        writerLatest.Write(fullName);
                        writerLatest.Write(ciid);
                        writerLatest.Write(value.Type, "attributevaluetype");
                        writerLatest.Write(valueText);
                        writerLatest.Write(valueBinary);
                        writerLatest.Write(valueControl);
                        writerLatest.Write(data.LayerID);
                        writerLatest.Write(state, "attributestate");
                        writerLatest.Write(changeset.Timestamp, NpgsqlDbType.TimestampTz);
                        writerLatest.Write(changeset.ID);
                    }
                    writerLatest.Complete();
                    writerLatest.Close();
                }

                // updates (actual updates and removals)
                var actualModified = actualInserts.Where(t => t.state != AttributeState.New);
                foreach (var (ciid, fullName, value, state, newAttributeID, existingAttributeID) in actualModified)
                {
                    using var commandUpdateLatest = new NpgsqlCommand(@"
                        UPDATE attribute_latest SET id = @id, type = @type, value_text = @value_text, value_binary = @value_binary, 
                        value_control = @value_control, state = @state, ""timestamp"" = @timestamp, changeset_id = @changeset_id
                        WHERE id = @old_id", trans.DBConnection, trans.DBTransaction);
                    var (valueText, valueBinary, valueControl) = AttributeValueBuilder.Marshal(value);
                    commandUpdateLatest.Parameters.AddWithValue("id", newAttributeID);
                    commandUpdateLatest.Parameters.AddWithValue("old_id", existingAttributeID!);
                    commandUpdateLatest.Parameters.AddWithValue("type", value.Type);
                    commandUpdateLatest.Parameters.AddWithValue("value_text", valueText);
                    commandUpdateLatest.Parameters.AddWithValue("value_binary", valueBinary);
                    commandUpdateLatest.Parameters.AddWithValue("value_control", valueControl);
                    commandUpdateLatest.Parameters.AddWithValue("state", state);
                    commandUpdateLatest.Parameters.AddWithValue("timestamp", changeset.Timestamp);
                    commandUpdateLatest.Parameters.AddWithValue("changeset_id", changeset.ID);
                    await commandUpdateLatest.ExecuteNonQueryAsync();
                }
                foreach (var (outdatedAttribute, newAttributeID) in outdatedAttributes.Values)
                {
                    using var commandRemoveLatest = new NpgsqlCommand(@"
                        UPDATE attribute_latest SET id = @id, state = @state, ""timestamp"" = @timestamp, changeset_id = @changeset_id WHERE id = @old_id", trans.DBConnection, trans.DBTransaction);
                    commandRemoveLatest.Parameters.AddWithValue("id", newAttributeID);
                    commandRemoveLatest.Parameters.AddWithValue("old_id", outdatedAttribute.ID);
                    commandRemoveLatest.Parameters.AddWithValue("state", AttributeState.Removed);
                    commandRemoveLatest.Parameters.AddWithValue("timestamp", changeset.Timestamp);
                    commandRemoveLatest.Parameters.AddWithValue("changeset_id", changeset.ID);
                    await commandRemoveLatest.ExecuteNonQueryAsync();
                }
            }

            // return all attributes that have changed (their ciids and the attribute full names)
            return actualInserts.Select(i => (i.ciid, i.fullName)).Concat(outdatedAttributes.Values.Select(i => (i.attribute.CIID, i.attribute.Name)));
        }
    }
}
