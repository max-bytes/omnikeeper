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
            var currentAttribute = await _GetAttribute(name, ciid, layerID, trans, readTS, false);

            // handle equality case
            // which user it is does not make any difference; if the data is the same, no insert is made
            // the origin also does not make a difference... TODO: think about that! Is this correct?
            if (currentAttribute != null && currentAttribute.Value.Equals(value))
                return (currentAttribute, false);

            var id = Guid.NewGuid();
            var (_, changesetID) = await BulkUpdate(
                new (Guid, string, IAttributeValue, Guid?, Guid)[] { (ciid, name, value, currentAttribute?.ID, id) },
                new (Guid, string, IAttributeValue, Guid, Guid)[0],
                layerID, origin, changesetProxy, trans);

            return (new CIAttribute(id, name, ciid, value, changesetID), true);
        }

        public async Task<(CIAttribute attribute, bool changed)> RemoveAttribute(string name, Guid ciid, string layerID, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        {
            var readTS = TimeThreshold.BuildLatest();
            var currentAttribute = await _GetAttribute(name, ciid, layerID, trans, readTS, false);

            if (currentAttribute == null)
            {
                // attribute does not exist
                throw new Exception("Trying to remove attribute that does not exist");
            }

            var id = Guid.NewGuid();
            var (_, changesetID) = await BulkUpdate(
                new (Guid, string, IAttributeValue, Guid?, Guid)[0],
                new (Guid, string, IAttributeValue, Guid, Guid)[] { (ciid, name, currentAttribute.Value, currentAttribute.ID, id) },
                layerID, origin, changesetProxy, trans);

            var ret = new CIAttribute(id, name, ciid, currentAttribute.Value, changesetID);

            return (ret, true);
        }

        // NOTE: this bulk operation DOES check if the attributes that are inserted are "unique":
        // it is not possible to insert the "same" attribute (same ciid, name and layer) multiple times when using this preparation method
        // if this operation detects a duplicate, an exception is thrown;
        // the caller is responsible for making sure there are no duplicates
        public async Task<(
            IList<(Guid ciid, string fullName, IAttributeValue value, Guid? existingAttributeID, Guid newAttributeID)> inserts,
            IList<(Guid ciid, string name, IAttributeValue value, Guid attributeID, Guid newAttributeID)> removes
            )> PrepareForBulkUpdate<F>(IBulkCIAttributeData<F> data, IModelContext trans)
        {
            var readTS = TimeThreshold.BuildLatest();

            // consider ALL relevant attributes as outdated first
            var outdatedAttributes = (data switch
            {
                BulkCIAttributeDataLayerScope d => (d.NamePrefix.IsEmpty()) ?
                        (await GetAttributes(new AllCIIDsSelection(), AllAttributeSelection.Instance, new string[] { data.LayerID }, trans, readTS)) :
                        (await GetAttributes(new AllCIIDsSelection(), new RegexAttributeSelection($"^{d.NamePrefix}"), new string[] { data.LayerID }, trans, readTS)),
                BulkCIAttributeDataCIScope d => await GetAttributes(SpecificCIIDsSelection.Build(d.CIID), AllAttributeSelection.Instance, new string[] { data.LayerID }, trans: trans, atTime: readTS),
                BulkCIAttributeDataCIAndAttributeNameScope a =>
                    await GetAttributes(SpecificCIIDsSelection.Build(a.RelevantCIs), NamedAttributesSelection.Build(a.RelevantAttributes), new string[] { data.LayerID }, trans, readTS),
                _ => throw new Exception("Unknown scope")
            }).SelectMany(t => t.Values.SelectMany(tt => tt.Values)).ToDictionary(a => a.InformationHash); // TODO: slow?

            var actualInserts = new List<(Guid ciid, string fullName, IAttributeValue value, Guid? existingAttributeID, Guid newAttributeID)>();
            var informationHashesToInsert = new HashSet<string>();
            foreach (var fragment in data.Fragments)
            {
                var fullName = data.GetFullName(fragment);
                var ciid = data.GetCIID(fragment);
                var value = data.GetValue(fragment);

                var informationHash = CIAttribute.CreateInformationHash(fullName, ciid);
                if (informationHashesToInsert.Contains(informationHash))
                {
                    throw new Exception($"Duplicate attribute fragment detected! Bulk insertion does not support duplicate attributes; attribute name: {fullName}, ciid: {ciid}, value: {value.Value2String()}");
                }
                informationHashesToInsert.Add(informationHash);

                // remove the current attribute from the list of attributes to remove
                outdatedAttributes.Remove(informationHash, out var currentAttribute);

                // handle equality case, also think about what should happen if a different user inserts the same data
                if (currentAttribute != null && currentAttribute.Value.Equals(value))
                    continue;

                actualInserts.Add((ciid, fullName, value, currentAttribute?.ID, Guid.NewGuid()));
            }

            var removes = outdatedAttributes.Values.Select(a => (a.CIID, a.Name, a.Value, a.ID, Guid.NewGuid())).ToList();

            return (actualInserts, removes);
        }

        public async Task<(bool changed, Guid changesetID)> BulkUpdate(
            IList<(Guid ciid, string fullName, IAttributeValue value, Guid? existingAttributeID, Guid newAttributeID)> inserts,
            IList<(Guid ciid, string name, IAttributeValue value, Guid attributeID, Guid newAttributeID)> removes,
            string layerID, DataOriginV1 origin, IChangesetProxy changesetProxy, IModelContext trans)
        {
            if (!inserts.IsEmpty() || !removes.IsEmpty())
            {
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
                    writerHistoric.Write(changeset.Timestamp, NpgsqlDbType.TimestampTz);
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
                    writerHistoric.Write(changeset.Timestamp, NpgsqlDbType.TimestampTz);
                    writerHistoric.Write(changeset.ID);
                    writerHistoric.Write(partitionIndex, NpgsqlDbType.TimestampTz);
                }
                writerHistoric.Complete();
                writerHistoric.Close();


                // latest
                // new inserts
                // NOTE: actual new inserts are only those that have isNew, which must be equivalent to NOT having an entry in the latest table
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

                // TODO: improve performance
                // add index, use CTEs
                foreach (var (_, _, _, attributeID, _) in removes)
                {
                    using var commandRemoveLatest = new NpgsqlCommand(@"
                        DELETE FROM attribute_latest WHERE id = @id", trans.DBConnection, trans.DBTransaction);
                    commandRemoveLatest.Parameters.AddWithValue("id", attributeID);
                    await commandRemoveLatest.ExecuteNonQueryAsync();
                }

                return (true, changeset.ID);
            }
            else
            {
                return (false, default);
            }
        }
    }
}
