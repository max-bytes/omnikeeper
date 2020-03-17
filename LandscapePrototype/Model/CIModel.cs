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

        public async Task<string> CreateCI(string identity, NpgsqlTransaction trans)
        {
            using var command = new NpgsqlCommand(@"INSERT INTO ci (id) VALUES (@id)", conn, trans);
            command.Parameters.AddWithValue("id", identity);
            await command.ExecuteNonQueryAsync();
            return identity;
        }

        public async Task<string> CreateCIType(string typeID, NpgsqlTransaction trans)
        {
            using var command = new NpgsqlCommand(@"INSERT INTO citype (id) VALUES (@id)", conn, trans);
            command.Parameters.AddWithValue("id", typeID);
            var r = await command.ExecuteNonQueryAsync();
            return typeID;
        }

        public async Task<string> CreateCIWithType(string identity, string typeID, NpgsqlTransaction trans)
        {
            var ciType = await GetCIType(typeID, trans);
            if (ciType == null) throw new Exception($"Could not find CI-Type {typeID}");
            using var command = new NpgsqlCommand(@"INSERT INTO ci (id) VALUES (@id)", conn, trans);
            command.Parameters.AddWithValue("id", identity);
            await command.ExecuteNonQueryAsync();

            await SetCIType(identity, typeID, trans);

            return identity;
        }

        public async Task<CI> GetFullCI(string ciid, LayerSet layers, NpgsqlTransaction trans, DateTimeOffset atTime)
        {
            var type = await GetTypeOfCI(ciid, trans, atTime);
            var attributes = await GetMergedAttributes(ciid, false, layers, trans, atTime);
            return CI.Build(ciid, type, layers, atTime, attributes);
        }

        public async Task<CIType> GetTypeOfCI(string ciid, NpgsqlTransaction trans, DateTimeOffset? atTime)
        {
            var r = await GetTypeOfCIs(new string[] { ciid }, trans, atTime);
            return r.Values.FirstOrDefault();
        }

        public async Task<IDictionary<string, CIType>> GetTypeOfCIs(IEnumerable<string> ciids, NpgsqlTransaction trans, DateTimeOffset? atTime)
        {
            using var command = new NpgsqlCommand(@"SELECT distinct
                last_value(cta.ci_id) over wnd,
                last_value(ct.id) over wnd
                FROM citype_assignment cta
                INNER JOIN citype ct ON ct.id = cta.citype_id AND cta.timestamp <= @atTime AND cta.ci_id = ANY(@ci_ids)
                WINDOW wnd AS(
                    PARTITION by cta.ci_id ORDER BY cta.timestamp
                    ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING)
            ", conn, trans);
            var finalTimeThreshold = atTime ?? DateTimeOffset.Now;
            command.Parameters.AddWithValue("atTime", finalTimeThreshold);
            command.Parameters.AddWithValue("ci_ids", ciids.ToArray());

            var ret = new Dictionary<string, CIType>();
            using (var s = await command.ExecuteReaderAsync())
            {
                while (await s.ReadAsync())
                {
                    var ciid = s.GetString(0);
                    var typeID = s.GetString(1);
                    ret.Add(ciid, CIType.Build(typeID));
                }
            }
            return ret;
        }

        public async Task<CIType> GetCIType(string typeID, NpgsqlTransaction trans)
        {
            using var command = new NpgsqlCommand(@"SELECT id FROM citype WHERE id = @citype_id LIMIT 1", conn, trans);
            command.Parameters.AddWithValue("citype_id", typeID);

            using var dr = await command.ExecuteReaderAsync();

            if (!await dr.ReadAsync())
                return null;

            var typeIDOut = dr.GetString(0);
            return CIType.Build(typeIDOut);
        }

        public async Task<IEnumerable<CIType>> GetCITypes(NpgsqlTransaction trans)
        {
            var ret = new List<CIType>();
            using var command = new NpgsqlCommand(@"SELECT id FROM citype", conn, trans);
            using (var s = await command.ExecuteReaderAsync())
            {
                while (await s.ReadAsync())
                {
                    var id = s.GetString(0);
                    ret.Add(CIType.Build(id));
                }
            }

            return ret;
        }

        public async Task<IEnumerable<CI>> GetFullCIsWithType(LayerSet layers, NpgsqlTransaction trans, DateTimeOffset atTime, string typeID)
        {
            // TODO: performance improvements
            var cis = await GetFullCIs(layers, true, trans, atTime);
            return cis.Where(ci => ci.Type.ID == typeID);
        }

        public async Task<IEnumerable<string>> GetCIIDs(NpgsqlTransaction trans)
        {
            using var command = new NpgsqlCommand(@"select id from ci", conn, trans);
            var tmp = new List<string>();
            using var s = await command.ExecuteReaderAsync();
            while (await s.ReadAsync())
                tmp.Add(s.GetString(0));
            return tmp;
        }

        public async Task<IEnumerable<CI>> GetFullCIs(LayerSet layers, bool includeEmptyCIs, NpgsqlTransaction trans, DateTimeOffset atTime, IEnumerable<string> CIIDs = null)
        {
            if (CIIDs == null) CIIDs = await GetCIIDs(trans);
            var attributes = await GetMergedAttributes(CIIDs, false, layers, trans, atTime);

            var groupedAttributes = attributes.GroupBy(a => a.Attribute.CIID).ToDictionary(a => a.Key, a => a.ToList());

            if (includeEmptyCIs)
            {
                var emptyCIs = CIIDs.Except(groupedAttributes.Select(a => a.Key)).ToDictionary(a => a, a => new List<MergedCIAttribute>());
                groupedAttributes = groupedAttributes.Concat(emptyCIs).ToDictionary(a => a.Key, a => a.Value);
            }

            var ret = new List<CI>();
            var ciTypes = await GetTypeOfCIs(groupedAttributes.Keys, trans, atTime);
            foreach (var ga in groupedAttributes)
            {
                ret.Add(CI.Build(ga.Key, ciTypes[ga.Key], layers, atTime, ga.Value));
            }
            return ret;
        }

        public async Task<IEnumerable<MergedCIAttribute>> GetMergedAttributes(string ciIdentity, bool includeRemoved, LayerSet layers, NpgsqlTransaction trans, DateTimeOffset atTime)
        {
            return await GetMergedAttributes(new string[] { ciIdentity }, includeRemoved, layers, trans, atTime);
        }

        public async Task<IEnumerable<MergedCIAttribute>> GetMergedAttributes(IEnumerable<string> ciIdentities, bool includeRemoved, LayerSet layers, NpgsqlTransaction trans, DateTimeOffset atTime)
        {
            var ret = new List<MergedCIAttribute>();

            var tempLayersetTableName = await LayerSet.CreateLayerSetTempTable(layers, "temp_layerset", conn, trans);

            using (var command = new NpgsqlCommand(@$"
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
                WHERE c.timestamp <= @time_threshold and a.ci_id = ANY(@ci_identities)
                WINDOW wnd AS(PARTITION by a.name, a.ci_id, a.layer_id ORDER BY c.timestamp ASC -- sort by timestamp
                    ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING)
            ) inn
            inner join {tempLayersetTableName} ls ON inn.last_layer_id = ls.id-- inner join to only keep rows that are in the selected layers
            where inn.last_state != ALL(@excluded_states) -- remove entries from layers which' last item is deleted
            WINDOW wndOut AS(PARTITION by inn.last_name, inn.last_ci_id ORDER BY ls.order DESC -- sort by layer order
                ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING)
            ", conn, trans))
            {
                command.Parameters.AddWithValue("ci_identities", ciIdentities.ToArray());
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

                    var att = MergedCIAttribute.Build(CIAttribute.Build(id, name, CIID, av, state, changesetID), layerStack);

                    ret.Add(att);
                }
            }
            return ret;
        }

        public async Task<IEnumerable<CIAttribute>> FindAttributesByName(string like, bool includeRemoved, long layerID, NpgsqlTransaction trans, DateTimeOffset atTime)
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

                if (state != AttributeState.Removed || includeRemoved)
                {
                    var att = CIAttribute.Build(id, name, CIID, av, state, changesetID);
                    ret.Add(att);
                }
            }
            return ret;
        }

        private async Task<CIAttribute> GetAttribute(string name, long layerID, string ciid, NpgsqlTransaction trans, DateTimeOffset atTime)
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
            var att = CIAttribute.Build(id, name, CIID, av, state, changesetID);
            return att;
        }

        public async Task<CIAttribute> RemoveAttribute(string name, long layerID, string ciid, long changesetID, NpgsqlTransaction trans)
        {
            var currentAttribute = await GetAttribute(name, layerID, ciid, trans, DateTimeOffset.Now);

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

            using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            var id = reader.GetInt64(0);
            return CIAttribute.Build(id, name, ciid, currentAttribute.Value, AttributeState.Removed, changesetID);
        }

        public async Task<bool> SetCIType(string ciid, string typeID, NpgsqlTransaction trans)
        {
            using var command = new NpgsqlCommand(@"INSERT INTO citype_assignment (ci_id, citype_id, timestamp) VALUES
                (@ci_id, @citype_id, NOW())", conn, trans);
            command.Parameters.AddWithValue("ci_id", ciid);
            command.Parameters.AddWithValue("citype_id", typeID);
            await command.ExecuteNonQueryAsync();
            return true;
        }

        public async Task<CIAttribute> InsertAttribute(string name, IAttributeValue value, long layerID, string ciid, long changesetID, NpgsqlTransaction trans)
        {
            var currentAttribute = await GetAttribute(name, layerID, ciid, trans, DateTimeOffset.Now);

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

            using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            var id = reader.GetInt64(0);
            return CIAttribute.Build(id, name, ciid, value, state, changesetID);
        }

        public async Task<bool> BulkReplaceAttributes(BulkCIAttributeData data, long changesetID, NpgsqlTransaction trans)
        {
            var outdatedAttributes = (await FindAttributesByName($"{data.NamePrefix}%", false, data.LayerID, trans, DateTimeOffset.Now))
                .ToDictionary(a => a.InformationHash);

            // TODO: use postgres COPY feature instead of manual inserts https://www.npgsql.org/doc/copy.html
            foreach (var fragment in data.Fragments)
            {
                var fullName = fragment.FullName(data.NamePrefix);
                var informationHash = CIAttribute.CreateInformationHash(fullName, fragment.CIID);
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
                if (currentAttribute != null && currentAttribute.State != AttributeState.Removed && currentAttribute.Value.Equals(fragment.Value))
                    continue;

                using var command = new NpgsqlCommand(@"INSERT INTO attribute (name, ci_id, type, value, layer_id, state, changeset_id) 
                VALUES (@name, @ci_id, @type, @value, @layer_id, @state, @changeset_id) returning id", conn, trans);
                var (strType, strValue) = AttributeValueBuilder.GetTypeAndValueString(fragment.Value);

                command.Parameters.AddWithValue("name", fullName);
                command.Parameters.AddWithValue("ci_id", fragment.CIID);
                command.Parameters.AddWithValue("type", strType);
                command.Parameters.AddWithValue("value", strValue);
                command.Parameters.AddWithValue("layer_id", data.LayerID);
                command.Parameters.AddWithValue("state", state);
                command.Parameters.AddWithValue("changeset_id", changesetID);

                var id = (long)await command.ExecuteScalarAsync();
            }

            // remove outdated 
            foreach (var outdatedAttribute in outdatedAttributes.Values)
                await RemoveAttribute(outdatedAttribute.Name, data.LayerID, outdatedAttribute.CIID, changesetID, trans);

            return true;
        }
    }
}
