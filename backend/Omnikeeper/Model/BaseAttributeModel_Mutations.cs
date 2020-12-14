using Npgsql;
using NpgsqlTypes;
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
        public async Task<(CIAttribute attribute, bool changed)> RemoveAttribute(string name, Guid ciid, long layerID, IChangesetProxy changesetProxy, IModelContext trans)
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

            var changeset = await changesetProxy.GetChangeset(trans);

            using var command = new NpgsqlCommand(@"INSERT INTO attribute (id, name, ci_id, type, value_text, value_binary, value_control, layer_id, state, ""timestamp"", changeset_id, origin_type) 
                VALUES (@id, @name, @ci_id, @type, @value_text, @value_binary, @value_control, @layer_id, @state, @timestamp, @changeset_id, @origin_type)", trans.DBConnection, trans.DBTransaction);

            var (valueText, valueBinary, valueControl) = Marshal(currentAttribute.Value);

            var id = Guid.NewGuid();
            command.Parameters.AddWithValue("id", id);
            command.Parameters.AddWithValue("name", name);
            command.Parameters.AddWithValue("ci_id", ciid);
            command.Parameters.AddWithValue("type", currentAttribute.Value.Type);
            command.Parameters.AddWithValue("value_text", valueText);
            command.Parameters.AddWithValue("value_binary", valueBinary);
            command.Parameters.AddWithValue("value_control", valueControl);
            command.Parameters.AddWithValue("layer_id", layerID);
            command.Parameters.AddWithValue("state", AttributeState.Removed);
            command.Parameters.AddWithValue("timestamp", changeset.Timestamp);
            command.Parameters.AddWithValue("changeset_id", changeset.ID);
            command.Parameters.AddWithValue("origin_type", currentAttribute.Origin.Type);

            await command.ExecuteNonQueryAsync();
            var ret = new CIAttribute(id, name, ciid, currentAttribute.Value, AttributeState.Removed, changeset.ID, currentAttribute.Origin);

            return (ret, true);
        }

        public async Task<(CIAttribute attribute, bool changed)> InsertCINameAttribute(string nameValue, Guid ciid, long layerID, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
            => await InsertAttribute(ICIModel.NameAttribute, new AttributeScalarValueText(nameValue), ciid, layerID, changesetProxy, origin, trans);

        public async Task<(CIAttribute attribute, bool changed)> InsertAttribute(string name, IAttributeValue value, Guid ciid, long layerID, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
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

            var changeset = await changesetProxy.GetChangeset(trans);

            using var command = new NpgsqlCommand(@"INSERT INTO attribute (id, name, ci_id, type, value_text, value_binary, value_control, layer_id, state, ""timestamp"", changeset_id, origin_type) 
                VALUES (@id, @name, @ci_id, @type, @value_text, @value_binary, @value_control, @layer_id, @state, @timestamp, @changeset_id, @origin_type)", trans.DBConnection, trans.DBTransaction);

            var (valueText, valueBinary, valueControl) = Marshal(value);

            var id = Guid.NewGuid();
            command.Parameters.AddWithValue("id", id);
            command.Parameters.AddWithValue("name", name);
            command.Parameters.AddWithValue("ci_id", ciid);
            command.Parameters.AddWithValue("type", value.Type);
            command.Parameters.AddWithValue("value_text", valueText);
            command.Parameters.AddWithValue("value_binary", valueBinary);
            command.Parameters.AddWithValue("value_control", valueControl);
            command.Parameters.AddWithValue("layer_id", layerID);
            command.Parameters.AddWithValue("state", state);
            command.Parameters.AddWithValue("timestamp", changeset.Timestamp);
            command.Parameters.AddWithValue("changeset_id", changeset.ID);
            command.Parameters.AddWithValue("origin_type", origin.Type);

            await command.ExecuteNonQueryAsync();
            return (new CIAttribute(id, name, ciid, value, state, changeset.ID, origin), true);
        }

        public async Task<IEnumerable<(Guid ciid, string fullName, IAttributeValue value, AttributeState state)>> BulkReplaceAttributes<F>(IBulkCIAttributeData<F> data, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        {
            var readTS = TimeThreshold.BuildLatest();

            var outdatedAttributes = (data switch
            {
                BulkCIAttributeDataLayerScope d => (await FindAttributesByName($"^{data.NamePrefix}", new AllCIIDsSelection(), data.LayerID, trans, readTS)),
                BulkCIAttributeDataCIScope d => (await FindAttributesByName($"^{data.NamePrefix}", SpecificCIIDsSelection.Build(d.CIID), data.LayerID, trans, readTS)),
                _ => null
            }).ToDictionary(a => a.InformationHash);

            var actualInserts = new List<(Guid ciid, string fullName, IAttributeValue value, AttributeState state)>();
            foreach (var fragment in data.Fragments)
            {
                var fullName = data.GetFullName(fragment);
                var ciid = data.GetCIID(fragment);
                var value = data.GetValue(fragment);

                var informationHash = CIAttribute.CreateInformationHash(fullName, ciid);
                // remove the current attribute from the list of attribute to remove
                outdatedAttributes.Remove(informationHash, out var currentAttribute);

                var state = AttributeState.New;
                if (currentAttribute != null)
                {
                    if (currentAttribute.State == AttributeState.Removed)
                        state = AttributeState.Renewed;
                    else
                        state = AttributeState.Changed;
                }

                // handle equality case, also think about what should happen if a different user inserts the same data
                if (currentAttribute != null && currentAttribute.State != AttributeState.Removed && currentAttribute.Value.Equals(value))
                    continue;

                actualInserts.Add((ciid, fullName, value, state));
            }

            // changeset is only created and copy mode is only entered when there is actually anything inserted
            if (!actualInserts.IsEmpty() || !outdatedAttributes.IsEmpty())
            {
                Changeset changeset = await changesetProxy.GetChangeset(trans);

                // use postgres COPY feature instead of manual inserts https://www.npgsql.org/doc/copy.html
                using var writer = trans.DBConnection.BeginBinaryImport(@"COPY attribute (id, name, ci_id, type, value_text, value_binary, value_control, layer_id, state, ""timestamp"", changeset_id, origin_type) FROM STDIN (FORMAT BINARY)");
                foreach (var (ciid, fullName, value, state) in actualInserts)
                {
                    var (valueText, valueBinary, valueControl) = Marshal(value);

                    writer.StartRow();
                    writer.Write(Guid.NewGuid());
                    writer.Write(fullName);
                    writer.Write(ciid);
                    writer.Write(value.Type, "attributevaluetype");
                    writer.Write(valueText);
                    writer.Write(valueBinary);
                    writer.Write(valueControl);
                    writer.Write(data.LayerID);
                    writer.Write(state, "attributestate");
                    writer.Write(changeset.Timestamp, NpgsqlDbType.TimestampTz);
                    writer.Write(changeset.ID);
                    writer.Write(origin.Type, "dataorigintype");
                }

                // remove outdated 
                foreach (var outdatedAttribute in outdatedAttributes.Values)
                {
                    var (valueText, valueBinary, valueControl) = Marshal(outdatedAttribute.Value);

                    writer.StartRow();
                    writer.Write(Guid.NewGuid());
                    writer.Write(outdatedAttribute.Name);
                    writer.Write(outdatedAttribute.CIID);
                    writer.Write(outdatedAttribute.Value.Type, "attributevaluetype");
                    writer.Write(valueText);
                    writer.Write(valueBinary);
                    writer.Write(valueControl);
                    writer.Write(data.LayerID);
                    writer.Write(AttributeState.Removed, "attributestate");
                    writer.Write(changeset.Timestamp, NpgsqlDbType.TimestampTz);
                    writer.Write(changeset.ID);
                    writer.Write(outdatedAttribute.Origin.Type, "dataorigintype");
                }
                writer.Complete();
            }

            return actualInserts;

        }
    }
}
