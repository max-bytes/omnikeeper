using LandscapePrototype.Entity.AttributeValues;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace LandscapePrototype.Model
{
    public class CIModel
    {
        public NpgsqlConnection CreateOpenConnection(string dbName)
        {
            NpgsqlConnection conn = new NpgsqlConnection($"Server=127.0.0.1;User Id=postgres; Password=postgres;Database={dbName};");
            conn.Open();
            conn.TypeMapper.MapEnum<AttributeState>("attributestate");
            return conn;
        }

        public IEnumerable<CIAttribute> GetMergedAttributes(string ciIdentity, bool includeRemoved, NpgsqlConnection conn)
        {
            var ret = new List<CIAttribute>();

            using (var command = new NpgsqlCommand(@"
            select distinct
            last_value(a.name) over wnd as last_name,
            last_value(a.ci_id) over wnd as last_ci_id,
            last_value(a.type) over wnd as last_type,
            last_value(a.value) over wnd as ""last_value"",
            last_value(a.activation_time) over wnd as last_activation_time,
            last_value(a.layer_id) over wnd as last_layer_id,
            last_value(a.state) over wnd as last_state
                from ""attribute"" a inner join ci c ON a.ci_id = c.id
                where a.activation_time <= now() and c.identity = @ci_identity
            WINDOW wnd AS(
                PARTITION by a.name, a.ci_id ORDER BY a.activation_time, a.layer_id-- TODO: sort by layer priority, not by layer id
                ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING)
            ", conn))
            {
                command.Parameters.AddWithValue("ci_identity", ciIdentity);
                using var dr = command.ExecuteReader();

                // Output rows
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

                    var att = CIAttribute.Build(name, CIID, av, activationTime, layerID, state);

                    if (includeRemoved || att.State != AttributeState.Removed)
                        ret.Add(att);
                }
            }
            return ret;
        }

        private IEnumerable<CIAttribute> GetAttributes(string ciIdentity, long layerID, bool includeRemoved, NpgsqlConnection conn)
        {
            var ret = new List<CIAttribute>();

            using (var command = new NpgsqlCommand(@"
            select distinct
            last_value(a.name) over wnd as last_name,
            last_value(a.ci_id) over wnd as last_ci_id,
            last_value(a.type) over wnd as last_type,
            last_value(a.value) over wnd as ""last_value"",
            last_value(a.activation_time) over wnd as last_activation_time,
            last_value(a.layer_id) over wnd as last_layer_id,
            last_value(a.state) over wnd as last_state
                from ""attribute"" a inner join ci c ON a.ci_id = c.id
                where a.activation_time <= now() and c.identity = @ci_identity and a.layer_id = @layer_id
            WINDOW wnd AS(
                PARTITION by a.name, a.ci_id ORDER BY a.activation_time
                ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING)
            ", conn))
            {
                command.Parameters.AddWithValue("ci_identity", ciIdentity);
                command.Parameters.AddWithValue("layer_id", layerID);
                using var dr = command.ExecuteReader();

                // Output rows
                while (dr.Read())
                {
                    var name = dr.GetString(0);
                    var CIID = dr.GetInt64(1);
                    var type = dr.GetString(2);
                    var value = dr.GetString(3);
                    var av = AttributeValueBuilder.Build(type, value);
                    var activationTime = dr.GetTimeStamp(4).ToDateTime();
                    var _layerID = dr.GetInt64(5);
                    var state = dr.GetFieldValue<AttributeState>(6);
                    var att = CIAttribute.Build(name, CIID, av, activationTime, layerID, state);

                    if (includeRemoved || att.State != AttributeState.Removed)
                        ret.Add(att);
                }
            }
            return ret;
        }

        public CIAttribute GetAttribute(string name, long layerID, string ciIdentity, bool includeRemoved, NpgsqlConnection conn)
        {
            var attributes = GetAttributes(ciIdentity, layerID, includeRemoved, conn);
            return attributes.FirstOrDefault(a => a.Name == name);
        }

        private long GetCIIDFromIdentity(string ciIdentity, NpgsqlConnection conn)
        {
            using (var command = new NpgsqlCommand(@"select id from ci where identity = @identity LIMIT 1", conn)) 
            {
                command.Parameters.AddWithValue("identity", ciIdentity);
                var s = command.ExecuteScalar();
                return (long)s;
            }
        }

        public bool RemoveAttribute(string name, long layerID, string ciIdentity, long changesetID, NpgsqlConnection conn)
        {
            var currentAttribute = GetAttribute(name, layerID, ciIdentity, true, conn);
            var ciID = GetCIIDFromIdentity(ciIdentity, conn);

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
                VALUES (@name, @ci_id, @type, @value, now(), @layer_id, @state, @changeset_id)", conn);
            var (strType, strValue) = AttributeValueBuilder.GetTypeAndValueString(currentAttribute.Value);

            command.Parameters.AddWithValue("name", name);
            command.Parameters.AddWithValue("ci_id", ciID);
            command.Parameters.AddWithValue("type", strType);
            command.Parameters.AddWithValue("value", strValue);
            command.Parameters.AddWithValue("layer_id", layerID);
            command.Parameters.AddWithValue("state", AttributeState.Removed);
            command.Parameters.AddWithValue("changeset_id", changesetID);

            var numInserted = command.ExecuteNonQuery();

            return numInserted == 1;
        }

        public bool InsertAttribute(string name, IAttributeValue value, long layerID, string ciIdentity, long changesetID, NpgsqlConnection conn)
        {
            var currentAttribute = GetAttribute(name, layerID, ciIdentity, true, conn);
            var ciID = GetCIIDFromIdentity(ciIdentity, conn);

            var state = AttributeState.New; // TODO
            var equalValue = false;
            if (currentAttribute != null)
            {
                if (currentAttribute.Equals(value))
                {
                    equalValue = true;
                } else if (currentAttribute.State == AttributeState.Removed)
                    state = AttributeState.Renewed;
                else
                    state = AttributeState.Changed;
            }

            // TODO: handle equality case, also think about what should happen if a different user inserts the same data?

            using var command = new NpgsqlCommand(@"INSERT INTO attribute (name, ci_id, type, value, activation_time, layer_id, state, changeset_id) 
                VALUES (@name, @ci_id, @type, @value, now(), @layer_id, @state, @changeset_id)", conn);
            var (strType, strValue) = AttributeValueBuilder.GetTypeAndValueString(value);

            command.Parameters.AddWithValue("name", name);
            command.Parameters.AddWithValue("ci_id", ciID);
            command.Parameters.AddWithValue("type", strType);
            command.Parameters.AddWithValue("value", strValue);
            command.Parameters.AddWithValue("layer_id", layerID);
            command.Parameters.AddWithValue("state", state);
            command.Parameters.AddWithValue("changeset_id", changesetID);

            var numInserted = command.ExecuteNonQuery();

            return numInserted == 1;
        }

        public long CreateLayer(string name, NpgsqlConnection conn)
        {
            using var command = new NpgsqlCommand(@"INSERT INTO layer (name) VALUES (@name) returning id", conn);
            command.Parameters.AddWithValue("name", name);
            var id = (long)command.ExecuteScalar();
            return id;
        }

        public long CreateCI(string identity, NpgsqlConnection conn)
        {
            using var command = new NpgsqlCommand(@"INSERT INTO ci (identity) 
                    VALUES (@identity) returning id", conn);
            command.Parameters.AddWithValue("identity", identity);
            var id = (long)command.ExecuteScalar();
            return id;
        }

        public long CreateChangeset(NpgsqlConnection conn)
        {
            using var command = new NpgsqlCommand(@"INSERT INTO changeset (timestamp) 
                    VALUES (now()) returning id", conn);
            var id = (long)command.ExecuteScalar();
            return id;
        }
    }
}
