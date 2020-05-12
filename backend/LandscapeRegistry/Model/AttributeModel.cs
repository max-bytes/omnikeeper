﻿using Landscape.Base.Entity;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using LandscapeRegistry.Entity.AttributeValues;
using LandscapeRegistry.Utils;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Landscape.Base.Model.IAttributeModel;

namespace LandscapeRegistry.Model
{
    public class AttributeModel : IAttributeModel
    {
        private readonly NpgsqlConnection conn;

        public AttributeModel(NpgsqlConnection connection)
        {
            conn = connection;
        }

        public async Task<IDictionary<string, MergedCIAttribute>> GetMergedAttributes(Guid ciid, bool includeRemoved, LayerSet layers, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            var d = await GetMergedAttributes(new Guid[] { ciid }, includeRemoved, layers, trans, atTime);
            return d.GetValueOrDefault(ciid, () => new Dictionary<string, MergedCIAttribute>());
        }

        public async Task<IDictionary<Guid, IDictionary<string, MergedCIAttribute>>> GetMergedAttributes(IEnumerable<Guid> ciids, bool includeRemoved, LayerSet layers, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            var ret = new Dictionary<Guid, IDictionary<string, MergedCIAttribute>>();

            var tempLayersetTableName = await LayerSet.CreateLayerSetTempTable(layers, "temp_layerset", conn, trans);

            // inner query can use distinct on, outer needs to do windowing, because of array_agg
            using (var command = new NpgsqlCommand(@$"
            select distinct
            last_value(inn.id) over wndOut,
            last_value(inn.name) over wndOut,
            last_value(inn.ci_id) over wndOut,
            last_value(inn.type) over wndOut,
            last_value(inn.value) over wndOut,
            last_value(inn.state) over wndOut,
            last_value(inn.changeset_id) over wndOut,
            array_agg(inn.layer_id) over wndOut
            from(
                select distinct on (ci_id, name, layer_id) * from
                    attribute where timestamp <= @time_threshold and ci_id = ANY(@ci_identities) and layer_id = ANY(@layer_ids) order by ci_id, name, layer_id, timestamp DESC
            ) inn
            inner join {tempLayersetTableName} ls ON inn.layer_id = ls.id -- inner join to only keep rows that are in the selected layers
            where inn.state != ALL(@excluded_states) -- remove entries from layers which' last item is deleted
            WINDOW wndOut AS(PARTITION by inn.name, inn.ci_id ORDER BY ls.order DESC -- sort by layer order
                ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING)
            ", conn, trans))
            {
                command.Parameters.AddWithValue("ci_identities", ciids.ToArray());
                var excludedStates = (includeRemoved) ? new AttributeState[] { } : new AttributeState[] { AttributeState.Removed };
                command.Parameters.AddWithValue("excluded_states", excludedStates);
                command.Parameters.AddWithValue("time_threshold", atTime.Time);
                command.Parameters.AddWithValue("layer_ids", layers.ToArray());
                using var dr = command.ExecuteReader();

                while (dr.Read())
                {
                    var id = dr.GetInt64(0);
                    var name = dr.GetString(1);
                    var CIID = dr.GetGuid(2);
                    var type = dr.GetFieldValue<AttributeValueType>(3);
                    var value = dr.GetString(4);
                    var av = AttributeValueBuilder.BuildFromDatabase(value, type);

                    var state = dr.GetFieldValue<AttributeState>(5);
                    var changesetID = dr.GetInt64(6);
                    var layerStack = (long[])dr[7];

                    var att = MergedCIAttribute.Build(CIAttribute.Build(id, name, CIID, av, state, changesetID), layerStack);

                    if (!ret.ContainsKey(CIID))
                        ret.Add(CIID, new Dictionary<string, MergedCIAttribute>());
                    ret[CIID].Add(name, att);
                }
            }
            return ret;
        }

        public async Task<IEnumerable<CIAttribute>> GetAttributes(IAttributeSelection selection, bool includeRemoved, long layerID, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            var ret = new List<CIAttribute>();

            using var command = new NpgsqlCommand($@"
            select distinct on(ci_id, name) id, name, ci_id, type, value, state, changeset_id FROM attribute 
            where timestamp <= @time_threshold and ({selection.WhereClause}) and layer_id = @layer_id
            order by ci_id, name, timestamp DESC
            ", conn, trans);
            selection.AddParameters(command.Parameters);
            command.Parameters.AddWithValue("layer_id", layerID);
            command.Parameters.AddWithValue("time_threshold", atTime.Time);

            using var dr = await command.ExecuteReaderAsync();

            while (dr.Read())
            {
                var id = dr.GetInt64(0);
                var name = dr.GetString(1);
                var CIID = dr.GetGuid(2);
                var type = dr.GetFieldValue<AttributeValueType>(3);
                var value = dr.GetString(4);
                var av = AttributeValueBuilder.BuildFromDatabase(value, type);
                var state = dr.GetFieldValue<AttributeState>(5);
                var changesetID = dr.GetInt64(6);

                var att = CIAttribute.Build(id, name, CIID, av, state, changesetID);
                if (state != AttributeState.Removed || includeRemoved)
                {
                    ret.Add(att);
                }
            }
            return ret;
        }

        public async Task<IEnumerable<CIAttribute>> FindAttributesByName(string like, bool includeRemoved, long layerID, NpgsqlTransaction trans, TimeThreshold atTime, Guid? ciid = null)
        {
            var ret = new List<CIAttribute>();

            var innerWhereClause = "1=1";
            if (ciid != null)
            {
                innerWhereClause = "ci_id = @ci_id";
            }

            using var command = new NpgsqlCommand($@"
            select distinct on(ci_id, name, layer_id) id, name, ci_id, type, value, state, changeset_id from
                attribute where timestamp <= @time_threshold and layer_id = @layer_id and name like @like_name and ({innerWhereClause}) order by ci_id, name, layer_id, timestamp DESC
            ", conn, trans);

            command.Parameters.AddWithValue("layer_id", layerID);
            command.Parameters.AddWithValue("like_name", like);
            command.Parameters.AddWithValue("time_threshold", atTime.Time);
            if (ciid != null)
                command.Parameters.AddWithValue("ci_id", ciid.Value);

            using var dr = await command.ExecuteReaderAsync();
            while (dr.Read())
            {
                var id = dr.GetInt64(0);
                var name = dr.GetString(1);
                var CIID = dr.GetGuid(2);
                var type = dr.GetFieldValue<AttributeValueType>(3);
                var value = dr.GetString(4);
                var av = AttributeValueBuilder.BuildFromDatabase(value, type);
                var state = dr.GetFieldValue<AttributeState>(5);
                var changesetID = dr.GetInt64(6);

                if (state != AttributeState.Removed || includeRemoved)
                {
                    var att = CIAttribute.Build(id, name, CIID, av, state, changesetID);
                    ret.Add(att);
                }
            }
            return ret;
        }

        public async Task<IDictionary<Guid, MergedCIAttribute>> FindMergedAttributesByFullName(string name, IAttributeSelection selection, bool includeRemoved, LayerSet layers, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            var ret = new Dictionary<Guid, MergedCIAttribute>();

            var tempLayersetTableName = await LayerSet.CreateLayerSetTempTable(layers, "temp_layerset", conn, trans);


            // inner query can use distinct on, outer needs to do windowing, because of array_agg
            using (var command = new NpgsqlCommand(@$"
            select distinct
            last_value(inn.id) over wndOut,
            last_value(inn.ci_id) over wndOut,
            last_value(inn.type) over wndOut,
            last_value(inn.value) over wndOut,
            last_value(inn.state) over wndOut,
            last_value(inn.changeset_id) over wndOut,
            array_agg(inn.layer_id) over wndOut
            from(
                select distinct on (ci_id, name, layer_id) * from
                    attribute where timestamp <= @time_threshold and ({selection.WhereClause}) and name = @name and layer_id = ANY(@layer_ids) order by ci_id, name, layer_id, timestamp DESC
            ) inn
            inner join {tempLayersetTableName} ls ON inn.layer_id = ls.id -- inner join to only keep rows that are in the selected layers
            where inn.state != ALL(@excluded_states) -- remove entries from layers which' last item is deleted
            WINDOW wndOut AS(PARTITION by inn.name, inn.ci_id ORDER BY ls.order DESC -- sort by layer order
                ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING)
            ", conn, trans))
            {
                var excludedStates = (includeRemoved) ? new AttributeState[] { } : new AttributeState[] { AttributeState.Removed };
                command.Parameters.AddWithValue("excluded_states", excludedStates);
                command.Parameters.AddWithValue("time_threshold", atTime.Time);
                command.Parameters.AddWithValue("name", name);
                command.Parameters.AddWithValue("layer_ids", layers.ToArray());
                selection.AddParameters(command.Parameters);
                using var dr = command.ExecuteReader();

                while (dr.Read())
                {
                    var id = dr.GetInt64(0);
                    var CIID = dr.GetGuid(1);
                    var type = dr.GetFieldValue<AttributeValueType>(2);
                    var value = dr.GetString(3);
                    var av = AttributeValueBuilder.BuildFromDatabase(value, type);

                    var state = dr.GetFieldValue<AttributeState>(4);
                    var changesetID = dr.GetInt64(5);
                    var layerStack = (long[])dr[6];

                    var att = MergedCIAttribute.Build(CIAttribute.Build(id, name, CIID, av, state, changesetID), layerStack);
                    ret[CIID] = att;
                }
            }
            return ret;
        }

        public async Task<CIAttribute> GetAttribute(string name, long layerID, Guid ciid, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            using var command = new NpgsqlCommand(@"
            select id, ci_id, type, value, state, changeset_id FROM attribute 
            where timestamp <= @time_threshold and ci_id = @ci_id and layer_id = @layer_id and name = @name
            order by timestamp DESC LIMIT 1
            ", conn, trans);
            command.Parameters.AddWithValue("ci_id", ciid);
            command.Parameters.AddWithValue("layer_id", layerID);
            command.Parameters.AddWithValue("name", name);
            command.Parameters.AddWithValue("time_threshold", atTime.Time);

            using var dr = await command.ExecuteReaderAsync();

            if (!await dr.ReadAsync())
                return null;

            var id = dr.GetInt64(0);
            var CIID = dr.GetGuid(1);
            var type = dr.GetFieldValue<AttributeValueType>(2);
            var value = dr.GetString(3);
            var av = AttributeValueBuilder.BuildFromDatabase(value, type);
            var state = dr.GetFieldValue<AttributeState>(4);
            var changesetID = dr.GetInt64(5);
            var att = CIAttribute.Build(id, name, CIID, av, state, changesetID);
            return att;
        }

        public async Task<CIAttribute> RemoveAttribute(string name, long layerID, Guid ciid, long changesetID, NpgsqlTransaction trans)
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

            using var command = new NpgsqlCommand(@"INSERT INTO attribute (name, ci_id, type, value, layer_id, state, ""timestamp"", changeset_id) 
                VALUES (@name, @ci_id, @type, @value, @layer_id, @state, @timestamp, @changeset_id) returning id", conn, trans);

            command.Parameters.AddWithValue("name", name);
            command.Parameters.AddWithValue("ci_id", ciid);
            command.Parameters.AddWithValue("type", currentAttribute.Value.Type);
            command.Parameters.AddWithValue("value", currentAttribute.Value.ToDTO().Value2DatabaseString());
            command.Parameters.AddWithValue("layer_id", layerID);
            command.Parameters.AddWithValue("state", AttributeState.Removed);
            command.Parameters.AddWithValue("timestamp", DateTimeOffset.Now);
            command.Parameters.AddWithValue("changeset_id", changesetID);

            using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            var id = reader.GetInt64(0);
            var ret = CIAttribute.Build(id, name, ciid, currentAttribute.Value, AttributeState.Removed, changesetID);

            return ret;
        }

        public async Task<CIAttribute> InsertCINameAttribute(string nameValue, long layerID, Guid ciid, long changesetID, NpgsqlTransaction trans)
            => await InsertAttribute(CIModel.NameAttribute, AttributeValueTextScalar.Build(nameValue), layerID, ciid, changesetID, trans);

        public async Task<CIAttribute> InsertAttribute(string name, IAttributeValue value, long layerID, Guid ciid, long changesetID, NpgsqlTransaction trans)
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

            using var command = new NpgsqlCommand(@"INSERT INTO attribute (name, ci_id, type, value, layer_id, state, ""timestamp"", changeset_id) 
                VALUES (@name, @ci_id, @type, @value, @layer_id, @state, @timestamp, @changeset_id) returning id", conn, trans);

            command.Parameters.AddWithValue("name", name);
            command.Parameters.AddWithValue("ci_id", ciid);
            command.Parameters.AddWithValue("type", value.Type);
            command.Parameters.AddWithValue("value", value.ToDTO().Value2DatabaseString());
            command.Parameters.AddWithValue("layer_id", layerID);
            command.Parameters.AddWithValue("state", state);
            command.Parameters.AddWithValue("timestamp", DateTimeOffset.Now);
            command.Parameters.AddWithValue("changeset_id", changesetID);

            using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            var id = reader.GetInt64(0);
            return CIAttribute.Build(id, name, ciid, value, state, changesetID);
        }

        public async Task<bool> BulkReplaceAttributes<F>(IBulkCIAttributeData<F> data, long changesetID, NpgsqlTransaction trans)
        {
            var readTS = TimeThreshold.BuildLatest();

            var outdatedAttributes = (data switch
            {
                BulkCIAttributeDataLayerScope d => (await FindAttributesByName($"{data.NamePrefix}%", false, data.LayerID, trans, readTS)),
                BulkCIAttributeDataCIScope d => (await FindAttributesByName($"{data.NamePrefix}%", false, data.LayerID, trans, readTS, d.CIID)),
                _ => null
            }).ToDictionary(a => a.InformationHash);


            // get current timestamp in database
            var writeTS = TimeThreshold.BuildLatest();

            // use postgres COPY feature instead of manual inserts https://www.npgsql.org/doc/copy.html
            using (var writer = conn.BeginBinaryImport(@"COPY attribute (name, ci_id, type, value, layer_id, state, ""timestamp"", changeset_id) FROM STDIN (FORMAT BINARY)"))
            {
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

                    writer.StartRow();
                    writer.Write(fullName);
                    writer.Write(ciid);
                    writer.Write(value.Type, "attributevaluetype");
                    writer.Write(value.ToDTO().Value2DatabaseString());
                    writer.Write(data.LayerID);
                    writer.Write(state, "attributestate");
                    writer.Write(writeTS.Time, NpgsqlDbType.TimestampTz);
                    writer.Write(changesetID);
                }

                // remove outdated 
                foreach (var outdatedAttribute in outdatedAttributes.Values)
                {
                    writer.StartRow();
                    writer.Write(outdatedAttribute.Name);
                    writer.Write(outdatedAttribute.CIID);
                    writer.Write(outdatedAttribute.Value.Type, "attributevaluetype");
                    writer.Write(outdatedAttribute.Value.ToDTO().Value2DatabaseString());
                    writer.Write(data.LayerID);
                    writer.Write(AttributeState.Removed, "attributestate");
                    writer.Write(writeTS.Time, NpgsqlDbType.TimestampTz);
                    writer.Write(changesetID);
                }

                writer.Complete();
            }

            return true;
        }
    }
}
