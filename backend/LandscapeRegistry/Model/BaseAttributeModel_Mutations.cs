using Landscape.Base.Entity;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using LandscapeRegistry.Entity.AttributeValues;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapeRegistry.Model
{
    public partial class BaseAttributeModel
    {
        public async Task<CIAttribute> RemoveAttribute(string name, long layerID, Guid ciid, IChangesetProxy changesetProxy, NpgsqlTransaction trans)
        {
            var readTS = TimeThreshold.BuildLatest();
            var currentAttribute = await GetAttribute(name, layerID, ciid, trans, readTS);

            if (currentAttribute == null)
            {
                // attribute does not exist
                throw new Exception("Trying to remove attribute that does not exist");
            }
            if (currentAttribute.State == AttributeState.Removed)
            {
                // the attribute is already removed, no-op(?)
                return currentAttribute;
            }

            var changeset = await changesetProxy.GetChangeset(trans);

            using var command = new NpgsqlCommand(@"INSERT INTO attribute (id, name, ci_id, type, value, layer_id, state, ""timestamp"", changeset_id) 
                VALUES (@id, @name, @ci_id, @type, @value, @layer_id, @state, @timestamp, @changeset_id)", conn, trans);

            var id = Guid.NewGuid();
            command.Parameters.AddWithValue("id", id);
            command.Parameters.AddWithValue("name", name);
            command.Parameters.AddWithValue("ci_id", ciid);
            command.Parameters.AddWithValue("type", currentAttribute.Value.Type);
            command.Parameters.AddWithValue("value", currentAttribute.Value.ToDTO().Value2DatabaseString());
            command.Parameters.AddWithValue("layer_id", layerID);
            command.Parameters.AddWithValue("state", AttributeState.Removed);
            command.Parameters.AddWithValue("timestamp", changeset.Timestamp);
            command.Parameters.AddWithValue("changeset_id", changeset.ID);

            await command.ExecuteNonQueryAsync();
            var ret = CIAttribute.Build(id, name, ciid, currentAttribute.Value, AttributeState.Removed, changeset.ID);

            return ret;
        }

        public async Task<CIAttribute> InsertCINameAttribute(string nameValue, long layerID, Guid ciid, IChangesetProxy changesetProxy, NpgsqlTransaction trans)
            => await InsertAttribute(ICIModel.NameAttribute, AttributeScalarValueText.Build(nameValue), layerID, ciid, changesetProxy, trans);

        public async Task<CIAttribute> InsertAttribute(string name, IAttributeValue value, long layerID, Guid ciid, IChangesetProxy changesetProxy, NpgsqlTransaction trans)
        {
            var readTS = TimeThreshold.BuildLatest();
            var currentAttribute = await GetAttribute(name, layerID, ciid, trans, readTS);

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
            if (currentAttribute != null && currentAttribute.State != AttributeState.Removed && currentAttribute.Value.Equals(value))
                return currentAttribute;

            var changeset = await changesetProxy.GetChangeset(trans);

            using var command = new NpgsqlCommand(@"INSERT INTO attribute (id, name, ci_id, type, value, layer_id, state, ""timestamp"", changeset_id) 
                VALUES (@id, @name, @ci_id, @type, @value, @layer_id, @state, @timestamp, @changeset_id)", conn, trans);

            var id = Guid.NewGuid();
            command.Parameters.AddWithValue("id", id);
            command.Parameters.AddWithValue("name", name);
            command.Parameters.AddWithValue("ci_id", ciid);
            command.Parameters.AddWithValue("type", value.Type);
            command.Parameters.AddWithValue("value", value.ToDTO().Value2DatabaseString());
            command.Parameters.AddWithValue("layer_id", layerID);
            command.Parameters.AddWithValue("state", state);
            command.Parameters.AddWithValue("timestamp", changeset.Timestamp);
            command.Parameters.AddWithValue("changeset_id", changeset.ID);

            await command.ExecuteNonQueryAsync();
            return CIAttribute.Build(id, name, ciid, value, state, changeset.ID);
        }

        public async Task<bool> BulkReplaceAttributes<F>(IBulkCIAttributeData<F> data, IChangesetProxy changesetProxy, NpgsqlTransaction trans)
        {
            var readTS = TimeThreshold.BuildLatest();

            var outdatedAttributes = (data switch
            {
                BulkCIAttributeDataLayerScope d => (await FindAttributesByName($"^{data.NamePrefix}", new AllCIIDsSelection(), data.LayerID, trans, readTS)),
                BulkCIAttributeDataCIScope d => (await FindAttributesByName($"^{data.NamePrefix}", new SingleCIIDSelection(d.CIID), data.LayerID, trans, readTS)),
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
                using var writer = conn.BeginBinaryImport(@"COPY attribute (id, name, ci_id, type, value, layer_id, state, ""timestamp"", changeset_id) FROM STDIN (FORMAT BINARY)");
                foreach (var (ciid, fullName, value, state) in actualInserts)
                {
                    writer.StartRow();
                    writer.Write(Guid.NewGuid());
                    writer.Write(fullName);
                    writer.Write(ciid);
                    writer.Write(value.Type, "attributevaluetype");
                    writer.Write(value.ToDTO().Value2DatabaseString());
                    writer.Write(data.LayerID);
                    writer.Write(state, "attributestate");
                    writer.Write(changeset.Timestamp, NpgsqlDbType.TimestampTz);
                    writer.Write(changeset.ID);
                }

                // remove outdated 
                foreach (var outdatedAttribute in outdatedAttributes.Values)
                {
                    writer.StartRow();
                    writer.Write(Guid.NewGuid());
                    writer.Write(outdatedAttribute.Name);
                    writer.Write(outdatedAttribute.CIID);
                    writer.Write(outdatedAttribute.Value.Type, "attributevaluetype");
                    writer.Write(outdatedAttribute.Value.ToDTO().Value2DatabaseString());
                    writer.Write(data.LayerID);
                    writer.Write(AttributeState.Removed, "attributestate");
                    writer.Write(changeset.Timestamp, NpgsqlDbType.TimestampTz);
                    writer.Write(changeset.ID);
                }
                writer.Complete();
            }

            return true;
        }
    }
}
