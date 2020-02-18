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
    public class CIModel
    {
        private readonly NpgsqlConnection conn;

        public CIModel(NpgsqlConnection connection)
        {
            conn = connection;
        }

        public async Task<CI> GetCI(string ciIdentity, LayerSet layers, NpgsqlTransaction trans)
        {
            var attributes = await GetMergedAttributes(ciIdentity, false, layers, trans);
            return CI.Build(ciIdentity, attributes);
        }

        public async Task<IEnumerable<CIAttribute>> GetMergedAttributes(string ciIdentity, bool includeRemoved, LayerSet layers, NpgsqlTransaction trans)
        {
            var ret = new List<CIAttribute>();

            await LayerSet.CreateLayerSetTempTable(layers, "temp_layerset", conn);

            using (var command = new NpgsqlCommand(@"
            select distinct
            last_value(inn.last_name) over wndOut,
            last_value(inn.last_ci_id) over wndOut,
            last_value(inn.last_type) over wndOut,
            last_value(inn.last_value) over wndOut,
            last_value(inn.last_activation_time) over wndOut,
            last_value(inn.last_layer_id) over wndOut,
            last_value(inn.last_state) over wndOut,
            last_value(inn.last_changeset_id) over wndOut
            from(
                select distinct
                last_value(a.name) over wnd as last_name,
                last_value(a.ci_id) over wnd as last_ci_id,
                last_value(a.type) over wnd as last_type,
                last_value(a.value) over wnd as ""last_value"",
                last_value(a.activation_time) over wnd as last_activation_time,
                last_value(a.layer_id) over wnd as last_layer_id,
                last_value(a.state) over wnd as last_state,
                last_value(a.changeset_id) over wnd as last_changeset_id
                from ""attribute"" a
                inner join ci c ON a.ci_id = c.id
                WHERE a.activation_time <= now() and c.identity = @ci_identity
                WINDOW wnd AS(PARTITION by a.name, a.ci_id, a.layer_id ORDER BY a.activation_time ASC-- sort by activation time
                    ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING)
            ) inn
            inner join temp_layerset ls ON inn.last_layer_id = ls.id-- inner join to only keep rows that are in the selected layers
            where inn.last_state != ALL(@excluded_states) -- remove entries from layers which' last item is deleted
            WINDOW wndOut AS(PARTITION by inn.last_name, inn.last_ci_id ORDER BY ls.order DESC-- sort by layer order
                ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING)
            ", conn, trans))
            {
                command.Parameters.AddWithValue("ci_identity", ciIdentity);
                var excludedStates = (includeRemoved) ? new AttributeState[] { } : new AttributeState[] { AttributeState.Removed };
                command.Parameters.AddWithValue("excluded_states", excludedStates);
                using var dr = command.ExecuteReader();

                while (dr.Read())
                {
                    var name = dr.GetString(0);
                    var CIID = dr.GetInt64(1);
                    var type = dr.GetString(2);
                    var value = dr.GetString(3);
                    var av = AttributeValueBuilder.Build(type, value);
                    var activationTime = dr.GetTimeStamp(4).ToDateTime();
                    var layerID = dr.GetInt64(5);
                    var state = dr.GetFieldValue<AttributeState>(6);
                    var changesetID = dr.GetInt64(7);

                    var att = CIAttribute.Build(name, CIID, av, activationTime, layerID, state, changesetID);

                    ret.Add(att);
                }
            }
            return ret;
        }

        public async Task<CIAttribute> GetAttribute(string name, long layerID, long ciid, NpgsqlTransaction trans)
        {
            using (var command = new NpgsqlCommand(@"
            select distinct
            last_value(a.ci_id) over wnd as last_ci_id,
            last_value(a.type) over wnd as last_type,
            last_value(a.value) over wnd as ""last_value"",
            last_value(a.activation_time) over wnd as last_activation_time,
            last_value(a.state) over wnd as last_state,
            last_value(a.changeset_id) over wnd as last_changeset_id
                from ""attribute"" a inner join ci c ON a.ci_id = c.id
                where a.activation_time <= now() and c.id = @ci_id and a.layer_id = @layer_id and a.name = @name
            WINDOW wnd AS(
                PARTITION by a.name, a.ci_id ORDER BY a.activation_time
                ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING) 
            LIMIT 1
            ", conn, trans))
            {
                command.Parameters.AddWithValue("ci_id", ciid);
                command.Parameters.AddWithValue("layer_id", layerID);
                command.Parameters.AddWithValue("name", name);
                using var dr = await command.ExecuteReaderAsync();

                if (!await dr.ReadAsync())
                    return null;

                var CIID = dr.GetInt64(0);
                var type = dr.GetString(1);
                var value = dr.GetString(2);
                var av = AttributeValueBuilder.Build(type, value);
                var activationTime = dr.GetTimeStamp(3).ToDateTime();
                var state = dr.GetFieldValue<AttributeState>(4);
                var changesetID = dr.GetInt64(5);
                var att = CIAttribute.Build(name, CIID, av, activationTime, layerID, state, changesetID);
                return att;
            }
        }

        // TODO: having both of these suck! maybe combine id and identity, or use identity for (almost) everything
        public async Task<long> GetCIIDFromIdentity(string ciIdentity, NpgsqlTransaction trans)
        {
            using var command = new NpgsqlCommand(@"select id from ci where identity = @identity LIMIT 1", conn, trans);
            command.Parameters.AddWithValue("identity", ciIdentity);
            var s = await command.ExecuteScalarAsync();
            return (long)s;
        }
        public async Task<string> GetIdentityFromCIID(long ciid, NpgsqlTransaction trans)
        {
            using var command = new NpgsqlCommand(@"select identity from ci where id = @id LIMIT 1", conn, trans);
            command.Parameters.AddWithValue("id", ciid);
            var s = await command.ExecuteScalarAsync();
            return (string)s;
        }

        public async Task<bool> RemoveAttribute(string name, long layerID, long ciid, long changesetID, NpgsqlTransaction trans)
        {
            var currentAttribute = await GetAttribute(name, layerID, ciid, trans);

            if (currentAttribute == null)
            {
                // attribute does not exist
                return false; 
            }
            if (currentAttribute.State == AttributeState.Removed)
            {
                // the attribute is already removed, no-op(?)
                return true;
            }

            using var command = new NpgsqlCommand(@"INSERT INTO attribute (name, ci_id, type, value, activation_time, layer_id, state, changeset_id) 
                VALUES (@name, @ci_id, @type, @value, now(), @layer_id, @state, @changeset_id)", conn, trans);
            var (strType, strValue) = AttributeValueBuilder.GetTypeAndValueString(currentAttribute.Value);

            command.Parameters.AddWithValue("name", name);
            command.Parameters.AddWithValue("ci_id", ciid);
            command.Parameters.AddWithValue("type", strType);
            command.Parameters.AddWithValue("value", strValue);
            command.Parameters.AddWithValue("layer_id", layerID);
            command.Parameters.AddWithValue("state", AttributeState.Removed);
            command.Parameters.AddWithValue("changeset_id", changesetID);

            var numInserted = await command.ExecuteNonQueryAsync();

            return numInserted == 1;
        }

        public async Task<bool> InsertAttribute(string name, IAttributeValue value, long layerID, long ciid, long changesetID, NpgsqlTransaction trans)
        {
            var currentAttribute = await GetAttribute(name, layerID, ciid, trans);

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
            if (currentAttribute != null && currentAttribute.Value.Equals(value)) // TODO: check other things, like user
                return true;


            using var command = new NpgsqlCommand(@"INSERT INTO attribute (name, ci_id, type, value, activation_time, layer_id, state, changeset_id) 
                VALUES (@name, @ci_id, @type, @value, now(), @layer_id, @state, @changeset_id)", conn, trans);
            var (strType, strValue) = AttributeValueBuilder.GetTypeAndValueString(value);

            command.Parameters.AddWithValue("name", name);
            command.Parameters.AddWithValue("ci_id", ciid);
            command.Parameters.AddWithValue("type", strType);
            command.Parameters.AddWithValue("value", strValue);
            command.Parameters.AddWithValue("layer_id", layerID);
            command.Parameters.AddWithValue("state", state);
            command.Parameters.AddWithValue("changeset_id", changesetID);

            var numInserted = await command.ExecuteNonQueryAsync();

            return numInserted == 1;
        }

        public async Task<long> CreateCI(string identity, NpgsqlTransaction trans)
        {
            using var command = new NpgsqlCommand(@"INSERT INTO ci (identity) VALUES (@identity) returning id", conn, trans);
            command.Parameters.AddWithValue("identity", identity);
            var id = (long)await command.ExecuteScalarAsync();
            return id;
        }

        public async Task<long> CreateChangeset(NpgsqlTransaction trans)
        {
            using var command = new NpgsqlCommand(@"INSERT INTO changeset (timestamp) VALUES (now()) returning id", conn, trans);
            var id = (long)await command.ExecuteScalarAsync();
            return id;
        }
    }
}
