using Landscape.Base;
using Landscape.Base.Model;
using LandscapePrototype.Entity;
using LandscapePrototype.Entity.AttributeValues;
using Microsoft.Extensions.Configuration;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapePrototype.Model
{
    public class CIModel : ICIModel
    {
        private readonly NpgsqlConnection conn;

        public CIModel(NpgsqlConnection connection)
        {
            conn = connection;
        }

        public async Task<CI> GetCI(string ciIdentity, LayerSet layers, NpgsqlTransaction trans, DateTimeOffset atTime)
        {
            var attributes = await GetMergedAttributes(ciIdentity, false, layers, trans, atTime);
            return CI.Build(ciIdentity, layers, atTime, attributes);
        }

        public async Task<IEnumerable<CI>> GetCIsWithType(LayerSet layers, NpgsqlTransaction trans, DateTimeOffset atTime, string type)
        {
            var cis = await GetCIs(layers, false, trans, atTime);
            return cis.Where(ci =>
            {
                var typeAttribute = ci.Attributes.FirstOrDefault(attribute => attribute.Name == "__type");
                return typeAttribute != null && typeAttribute.Value.Value2String() == type; // TODO: is this a good comparison?
            });
        }

        public async Task<IEnumerable<CI>> GetCIs(LayerSet layers, bool includeEmptyCIs, NpgsqlTransaction trans, DateTimeOffset atTime, IEnumerable<string> CIIDs = null)
        {
            using var command = (CIIDs == null) ? new NpgsqlCommand(@"select id from ci", conn, trans) : new NpgsqlCommand(@"select id from ci where id = ANY(@ci_ids)", conn, trans);
            if (CIIDs != null)
                command.Parameters.AddWithValue("ci_ids", CIIDs.ToArray());
            var tmp = new List<string>();
            using (var s = await command.ExecuteReaderAsync())
            {
                while (await s.ReadAsync())
                {
                    var identity = s.GetString(0);
                    tmp.Add(identity);
                }
            }
            var ret = new List<CI>();
            foreach (var identity in tmp)
            {
                // TODO: performance improvements
                var attributes = await GetMergedAttributes(identity, false, layers, trans, atTime);
                if (includeEmptyCIs || attributes.Count() > 0)
                    ret.Add(CI.Build(identity, layers, atTime, attributes));
            }
            return ret;
        }

        public async Task<IEnumerable<CIAttribute>> GetMergedAttributes(string ciIdentity, bool includeRemoved, LayerSet layers, NpgsqlTransaction trans, DateTimeOffset atTime)
        {
            var ret = new List<CIAttribute>();

            await LayerSet.CreateLayerSetTempTable(layers, "temp_layerset", conn, trans);

            using (var command = new NpgsqlCommand(@"
            select distinct
            last_value(inn.last_id) over wndOut,
            last_value(inn.last_name) over wndOut,
            last_value(inn.last_ci_id) over wndOut,
            last_value(inn.last_type) over wndOut,
            last_value(inn.last_value) over wndOut,
            last_value(inn.last_state) over wndOut,
            last_value(inn.last_changeset_id) over wndOut,
            array_agg(inn.last_layer_id) over wndOut
            from(
                select distinct
                last_value(a.id) over wnd as last_id,
                last_value(a.name) over wnd as last_name,
                last_value(a.ci_id) over wnd as last_ci_id,
                last_value(a.type) over wnd as last_type,
                last_value(a.value) over wnd as ""last_value"",
                last_value(a.layer_id) over wnd as last_layer_id,
                last_value(a.state) over wnd as last_state,
                last_value(a.changeset_id) over wnd as last_changeset_id
                from ""attribute"" a
                inner join changeset c on c.id = a.changeset_id
                WHERE c.timestamp <= @time_threshold and a.ci_id = @ci_identity
                WINDOW wnd AS(PARTITION by a.name, a.ci_id, a.layer_id ORDER BY c.timestamp ASC -- sort by timestamp
                    ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING)
            ) inn
            inner join temp_layerset ls ON inn.last_layer_id = ls.id-- inner join to only keep rows that are in the selected layers
            where inn.last_state != ALL(@excluded_states) -- remove entries from layers which' last item is deleted
            WINDOW wndOut AS(PARTITION by inn.last_name, inn.last_ci_id ORDER BY ls.order DESC -- sort by layer order
                ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING)
            ", conn, trans))
            {
                command.Parameters.AddWithValue("ci_identity", ciIdentity);
                var excludedStates = (includeRemoved) ? new AttributeState[] { } : new AttributeState[] { AttributeState.Removed };
                command.Parameters.AddWithValue("excluded_states", excludedStates);
                command.Parameters.AddWithValue("time_threshold", atTime);
                using var dr = command.ExecuteReader();

                while (dr.Read())
                {
                    var id = dr.GetInt64(0);
                    var name = dr.GetString(1);
                    var CIID = dr.GetString(2);
                    var type = dr.GetString(3);
                    var value = dr.GetString(4);
                    var av = AttributeValueBuilder.Build(type, value);
                    var state = dr.GetFieldValue<AttributeState>(5);
                    var changesetID = dr.GetInt64(6);
                    var layerStack = (long[])dr[7];

                    var att = CIAttribute.Build(id, name, CIID, av, layerStack, state, changesetID);

                    ret.Add(att);
                }
            }
            return ret;
        }

        public async Task<IEnumerable<CIAttribute>> FindAttributesByName(string like, long layerID, NpgsqlTransaction trans, DateTimeOffset atTime)
        {
            var ret = new List<CIAttribute>();

            using var command = new NpgsqlCommand(@"
            select distinct
            last_value(a.id) over wnd as last_id,
            last_value(a.name) over wnd as last_name,
            last_value(a.ci_id) over wnd as last_ci_id,
            last_value(a.type) over wnd as last_type,
            last_value(a.value) over wnd as ""last_value"",
            last_value(a.state) over wnd as last_state,
            last_value(a.changeset_id) over wnd as last_changeset_id
                from ""attribute"" a
                inner join changeset c on c.id = a.changeset_id
                where c.timestamp <= @time_threshold and a.layer_id = @layer_id and a.name LIKE @like_name
            WINDOW wnd AS(
                PARTITION by a.name, a.ci_id ORDER BY c.timestamp
                ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING) 
            ", conn, trans);
            command.Parameters.AddWithValue("layer_id", layerID);
            command.Parameters.AddWithValue("like_name", like);
            command.Parameters.AddWithValue("time_threshold", atTime);

            using var dr = await command.ExecuteReaderAsync();
            while (dr.Read())
            {
                var id = dr.GetInt64(0);
                var name = dr.GetString(1);
                var CIID = dr.GetString(2);
                var type = dr.GetString(3);
                var value = dr.GetString(4);
                var av = AttributeValueBuilder.Build(type, value);
                var state = dr.GetFieldValue<AttributeState>(5);
                var changesetID = dr.GetInt64(6);

                var att = CIAttribute.Build(id, name, CIID, av, new long[] { layerID }, state, changesetID);

                ret.Add(att);
            }
            return ret;
        }

        public async Task<CIAttribute> GetMergedAttribute(string name, long layerID, string ciid, NpgsqlTransaction trans, DateTimeOffset atTime)
        {
            using var command = new NpgsqlCommand(@"
            select distinct
            last_value(a.id) over wnd as last_id,
            last_value(a.ci_id) over wnd as last_ci_id,
            last_value(a.type) over wnd as last_type,
            last_value(a.value) over wnd as ""last_value"",
            last_value(a.state) over wnd as last_state,
            last_value(a.changeset_id) over wnd as last_changeset_id
                from ""attribute"" a
                inner join changeset c on c.id = a.changeset_id
                where c.timestamp <= @time_threshold and a.ci_id = @ci_id and a.layer_id = @layer_id and a.name = @name
            WINDOW wnd AS(
                PARTITION by a.name, a.ci_id ORDER BY c.timestamp
                ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING) 
            LIMIT 1
            ", conn, trans);
            command.Parameters.AddWithValue("ci_id", ciid);
            command.Parameters.AddWithValue("layer_id", layerID);
            command.Parameters.AddWithValue("name", name);
            command.Parameters.AddWithValue("time_threshold", atTime);

            using var dr = await command.ExecuteReaderAsync();

            if (!await dr.ReadAsync())
                return null;

            var id = dr.GetInt64(0);
            var CIID = dr.GetString(1);
            var type = dr.GetString(2);
            var value = dr.GetString(3);
            var av = AttributeValueBuilder.Build(type, value);
            var state = dr.GetFieldValue<AttributeState>(4);
            var changesetID = dr.GetInt64(5);
            var att = CIAttribute.Build(id, name, CIID, av, new long[] { layerID }, state, changesetID);
            return att;
        }

        public async Task<CIAttribute> RemoveAttribute(string name, long layerID, string ciid, long changesetID, NpgsqlTransaction trans)
        {
            var currentAttribute = await GetMergedAttribute(name, layerID, ciid, trans, DateTimeOffset.Now);

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

            using var command = new NpgsqlCommand(@"INSERT INTO attribute (name, ci_id, type, value, layer_id, state, changeset_id) 
                VALUES (@name, @ci_id, @type, @value, @layer_id, @state, @changeset_id) returning id", conn, trans);
            var (strType, strValue) = AttributeValueBuilder.GetTypeAndValueString(currentAttribute.Value);

            command.Parameters.AddWithValue("name", name);
            command.Parameters.AddWithValue("ci_id", ciid);
            command.Parameters.AddWithValue("type", strType);
            command.Parameters.AddWithValue("value", strValue);
            command.Parameters.AddWithValue("layer_id", layerID);
            command.Parameters.AddWithValue("state", AttributeState.Removed);
            command.Parameters.AddWithValue("changeset_id", changesetID);

            var layerStack = new long[] { layerID }; // TODO: calculate proper layerstack(?)

            using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            var id = reader.GetInt64(0);
            return CIAttribute.Build(id, name, ciid, currentAttribute.Value, layerStack, AttributeState.Removed, changesetID);
        }

        public async Task<CIAttribute> InsertAttribute(string name, IAttributeValue value, long layerID, string ciid, long changesetID, NpgsqlTransaction trans)
        {
            var currentAttribute = await GetMergedAttribute(name, layerID, ciid, trans, DateTimeOffset.Now);

            var state = AttributeState.New; // TODO
            if (currentAttribute != null)
            {
                if (currentAttribute.State == AttributeState.Removed)
                    state = AttributeState.Renewed;
                else
                    state = AttributeState.Changed;
            }

            // handle equality case, also think about what should happen if a different user inserts the same data
            //var equalValue = false;
            if (currentAttribute != null && currentAttribute.State != AttributeState.Removed && currentAttribute.Value.Equals(value)) // TODO: check other things, like user
                return currentAttribute;

            using var command = new NpgsqlCommand(@"INSERT INTO attribute (name, ci_id, type, value, layer_id, state, changeset_id) 
                VALUES (@name, @ci_id, @type, @value, @layer_id, @state, @changeset_id) returning id", conn, trans);
            var (strType, strValue) = AttributeValueBuilder.GetTypeAndValueString(value);

            command.Parameters.AddWithValue("name", name);
            command.Parameters.AddWithValue("ci_id", ciid);
            command.Parameters.AddWithValue("type", strType);
            command.Parameters.AddWithValue("value", strValue);
            command.Parameters.AddWithValue("layer_id", layerID);
            command.Parameters.AddWithValue("state", state);
            command.Parameters.AddWithValue("changeset_id", changesetID);

            var layerStack = new long[] { layerID }; // TODO: calculate proper layerstack(?)

            using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            var id = reader.GetInt64(0);
            return CIAttribute.Build(id, name, ciid, value, layerStack, state, changesetID);
        }

        public async Task<string> CreateCI(string identity, NpgsqlTransaction trans)
        {
            using var command = new NpgsqlCommand(@"INSERT INTO ci (id) VALUES (@id)", conn, trans);
            command.Parameters.AddWithValue("id", identity);
            await command.ExecuteNonQueryAsync();
            return identity;
        }
    }
}
