using Landscape.Base;
using Landscape.Base.Model;
using LandscapeRegistry.Entity;
using LandscapeRegistry.Entity.AttributeValues;
using Microsoft.Extensions.Configuration;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapeRegistry.Model
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

        public async Task<MergedCI> GetMergedCI(string ciid, LayerSet layers, NpgsqlTransaction trans, DateTimeOffset atTime)
        {
            var type = await GetTypeOfCI(ciid, trans, atTime);
            var attributes = await GetMergedAttributes(ciid, false, layers, trans, atTime);
            return MergedCI.Build(ciid, type, layers, atTime, attributes);
        }

        public async Task<CI> GetCI(string ciid, long layerID, NpgsqlTransaction trans, DateTimeOffset atTime)
        {
            var type = await GetTypeOfCI(ciid, trans, atTime);
            var attributes = await GetAttributes(new SingleCIIDAttributeSelection(ciid), false, layerID, trans, atTime);
            return CI.Build(ciid, type, layerID, atTime, attributes);
        }

        public async Task<IEnumerable<CI>> GetCIs(long layerID, bool includeEmptyCIs, NpgsqlTransaction trans, DateTimeOffset atTime)
        {
            var attributes = await GetAttributes(new AllCIIDsAttributeSelection(), false, layerID, trans, atTime);
            var groupedAttributes = attributes.GroupBy(a => a.CIID).ToDictionary(a => a.Key, a => a.ToList());
            if (includeEmptyCIs)
            {
                var allCIIds = await GetCIIDs(trans);
                var emptyCIs = allCIIds.Except(groupedAttributes.Select(a => a.Key)).ToDictionary(a => a, a => new List<CIAttribute>());
                groupedAttributes = groupedAttributes.Concat(emptyCIs).ToDictionary(a => a.Key, a => a.Value);
            }
            var ciTypes = await GetTypeOfCIs(groupedAttributes.Keys, trans, atTime);
            var t = groupedAttributes.Select(ga => CI.Build(ga.Key, ciTypes[ga.Key], layerID, atTime, ga.Value));
            return t;
        }

        public async Task<CIType> GetTypeOfCI(string ciid, NpgsqlTransaction trans, DateTimeOffset? atTime)
        {
            var r = await GetTypeOfCIs(new string[] { ciid }, trans, atTime);
            return r.Values.FirstOrDefault();
        }

        public async Task<IDictionary<string, CIType>> GetTypeOfCIs(IEnumerable<string> ciids, NpgsqlTransaction trans, DateTimeOffset? atTime)
        {
            using var command = new NpgsqlCommand(@"SELECT distinct on (cta.ci_id)
                cta.ci_id, ct.id
                FROM citype_assignment cta
                INNER JOIN citype ct ON ct.id = cta.citype_id AND cta.timestamp <= @atTime AND cta.ci_id = ANY(@ci_ids)
                ORDER BY cta.ci_id, cta.timestamp DESC
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

        public async Task<IEnumerable<MergedCI>> GetMergedCIsByType(LayerSet layers, NpgsqlTransaction trans, DateTimeOffset atTime, string typeID)
        {
            // TODO: performance improvements
            var cis = await GetMergedCIs(layers, true, trans, atTime);
            return cis.Where(ci => ci.Type.ID == typeID);
        }
        public async Task<IEnumerable<MergedCI>> GetMergedCIsByType(LayerSet layers, NpgsqlTransaction trans, DateTimeOffset atTime, IEnumerable<string> typeIDs)
        {
            // TODO: performance improvements
            var cis = await GetMergedCIs(layers, true, trans, atTime);
            return cis.Where(ci => typeIDs.Contains(ci.Type.ID));
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

        public async Task<IEnumerable<MergedCI>> GetMergedCIs(LayerSet layers, bool includeEmptyCIs, NpgsqlTransaction trans, DateTimeOffset atTime, IEnumerable<string> CIIDs = null)
        {
            if (CIIDs == null) CIIDs = await GetCIIDs(trans);
            var attributes = await GetMergedAttributes(CIIDs, false, layers, trans, atTime);

            var groupedAttributes = attributes.GroupBy(a => a.Attribute.CIID).ToDictionary(a => a.Key, a => a.ToList());

            if (includeEmptyCIs)
            {
                var emptyCIs = CIIDs.Except(groupedAttributes.Select(a => a.Key)).ToDictionary(a => a, a => new List<MergedCIAttribute>());
                groupedAttributes = groupedAttributes.Concat(emptyCIs).ToDictionary(a => a.Key, a => a.Value);
            }

            var ret = new List<MergedCI>();
            var ciTypes = await GetTypeOfCIs(groupedAttributes.Keys, trans, atTime);
            foreach (var ga in groupedAttributes)
            {
                ret.Add(MergedCI.Build(ga.Key, ciTypes[ga.Key], layers, atTime, ga.Value));
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
                command.Parameters.AddWithValue("ci_identities", ciIdentities.ToArray());
                var excludedStates = (includeRemoved) ? new AttributeState[] { } : new AttributeState[] { AttributeState.Removed };
                command.Parameters.AddWithValue("excluded_states", excludedStates);
                command.Parameters.AddWithValue("time_threshold", atTime);
                command.Parameters.AddWithValue("layer_ids", layers.ToArray());
                using var dr = command.ExecuteReader();

                while (dr.Read())
                {
                    var id = dr.GetInt64(0);
                    var name = dr.GetString(1);
                    var CIID = dr.GetString(2);
                    var type = dr.GetFieldValue<AttributeValueType>(3);
                    var value = dr.GetString(4);
                    var av = AttributeValueBuilder.BuildFromDatabase(value, type);

                    var state = dr.GetFieldValue<AttributeState>(5);
                    var changesetID = dr.GetInt64(6);
                    var layerStack = (long[])dr[7];

                    var att = MergedCIAttribute.Build(CIAttribute.Build(id, name, CIID, av, state, changesetID), layerStack);

                    ret.Add(att);
                }
            }
            return ret;
        }

        interface IAttributeSelection
        {
            string WhereClause { get; }
            void AddParameters(NpgsqlParameterCollection p);
        }
        class SingleCIIDAttributeSelection : IAttributeSelection
        {
            public string CIID { get; }
            public SingleCIIDAttributeSelection(string ciid)
            {
                CIID = ciid;
            }
            public string WhereClause => "ci_id = @ci_id";
            public void AddParameters(NpgsqlParameterCollection p) => p.AddWithValue("ci_id", CIID);
        }
        class MultiCIIDsAttributeSelection : IAttributeSelection
        {
            public string[] CIIDs { get; }
            public MultiCIIDsAttributeSelection(string[] ciids)
            {
                CIIDs = ciids;
            }
            public string WhereClause => "ci_id = ANY(@ci_ids)";
            public void AddParameters(NpgsqlParameterCollection p) => p.AddWithValue("ci_ids", CIIDs);
        }
        class AllCIIDsAttributeSelection : IAttributeSelection
        {
            public string WhereClause => "1=1";
            public void AddParameters(NpgsqlParameterCollection p) { }
        }
        private async Task<IEnumerable<CIAttribute>> GetAttributes(IAttributeSelection selection, bool includeRemoved, long layerID, NpgsqlTransaction trans, DateTimeOffset atTime)
        {
            var ret = new List<CIAttribute>();

            using var command = new NpgsqlCommand($@"
            select distinct on(ci_id, name) id, name, ci_id, type, value, state, changeset_id FROM attribute 
            where timestamp <= @time_threshold and ({selection.WhereClause}) and layer_id = @layer_id
            order by ci_id, name, timestamp DESC
            ", conn, trans);
            selection.AddParameters(command.Parameters);
            command.Parameters.AddWithValue("layer_id", layerID);
            command.Parameters.AddWithValue("time_threshold", atTime);

            using var dr = await command.ExecuteReaderAsync();

            while (dr.Read())
            {
                var id = dr.GetInt64(0);
                var name = dr.GetString(1);
                var CIID = dr.GetString(2);
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

        public async Task<IEnumerable<CIAttribute>> FindAttributesByName(string like, bool includeRemoved, long layerID, NpgsqlTransaction trans, DateTimeOffset atTime, string ciid = null)
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
            command.Parameters.AddWithValue("time_threshold", atTime);
            if (ciid != null)
                command.Parameters.AddWithValue("ci_id", ciid);

            using var dr = await command.ExecuteReaderAsync();
            while (dr.Read())
            {
                var id = dr.GetInt64(0);
                var name = dr.GetString(1);
                var CIID = dr.GetString(2);
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

        private async Task<CIAttribute> GetAttribute(string name, long layerID, string ciid, NpgsqlTransaction trans, DateTimeOffset atTime)
        {
            using var command = new NpgsqlCommand(@"
            select id, ci_id, type, value, state, changeset_id FROM attribute 
            where timestamp <= @time_threshold and ci_id = @ci_id and layer_id = @layer_id and name = @name
            order by timestamp DESC LIMIT 1

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
            var type = dr.GetFieldValue<AttributeValueType>(2);
            var value = dr.GetString(3);
            var av = AttributeValueBuilder.BuildFromDatabase(value, type);
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

            using var command = new NpgsqlCommand(@"INSERT INTO attribute (name, ci_id, type, value, layer_id, state, ""timestamp"", changeset_id) 
                VALUES (@name, @ci_id, @type, @value, @layer_id, @state, now(), @changeset_id) returning id", conn, trans);
            
            command.Parameters.AddWithValue("name", name);
            command.Parameters.AddWithValue("ci_id", ciid);
            command.Parameters.AddWithValue("type", currentAttribute.Value.Type);
            command.Parameters.AddWithValue("value", currentAttribute.Value.ToGeneric().Value2DatabaseString());
            command.Parameters.AddWithValue("layer_id", layerID);
            command.Parameters.AddWithValue("state", AttributeState.Removed);
            command.Parameters.AddWithValue("changeset_id", changesetID);

            using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            var id = reader.GetInt64(0);
            var ret = CIAttribute.Build(id, name, ciid, currentAttribute.Value, AttributeState.Removed, changesetID);

            return ret;
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
                VALUES (@name, @ci_id, @type, @value, @layer_id, @state, now(), @changeset_id) returning id", conn, trans);

            command.Parameters.AddWithValue("name", name);
            command.Parameters.AddWithValue("ci_id", ciid);
            command.Parameters.AddWithValue("type", value.Type);
            command.Parameters.AddWithValue("value", value.ToGeneric().Value2DatabaseString());
            command.Parameters.AddWithValue("layer_id", layerID);
            command.Parameters.AddWithValue("state", state);
            command.Parameters.AddWithValue("changeset_id", changesetID);

            using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            var id = reader.GetInt64(0);
            return CIAttribute.Build(id, name, ciid, value, state, changesetID);
        }

        public async Task<bool> BulkReplaceAttributes<F>(IBulkCIAttributeData<F> data, long changesetID, NpgsqlTransaction trans)
        {
            var outdatedAttributes = (data switch
            {
                BulkCIAttributeDataLayerScope d => (await FindAttributesByName($"{data.NamePrefix}%", false, data.LayerID, trans, DateTimeOffset.Now)),
                BulkCIAttributeDataCIScope d => (await FindAttributesByName($"{data.NamePrefix}%", false, data.LayerID, trans, DateTimeOffset.Now, d.CIID)),
                _ => null
            }).ToDictionary(a => a.InformationHash);


            // get current timestamp in database
            using var commandTime = new NpgsqlCommand(@"SELECT now()", conn, trans);
            var now = ((DateTime)(await commandTime.ExecuteScalarAsync()));

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
                    writer.Write(value.ToGeneric().Value2DatabaseString());
                    writer.Write(data.LayerID);
                    writer.Write(state, "attributestate");
                    writer.Write(now, NpgsqlDbType.TimestampTz);
                    writer.Write(changesetID);
                }

                // remove outdated 
                foreach (var outdatedAttribute in outdatedAttributes.Values)
                {
                    writer.StartRow();
                    writer.Write(outdatedAttribute.Name);
                    writer.Write(outdatedAttribute.CIID);
                    writer.Write(outdatedAttribute.Value.Type, "attributevaluetype");
                    writer.Write(outdatedAttribute.Value.ToGeneric().Value2DatabaseString());
                    writer.Write(data.LayerID);
                    writer.Write(AttributeState.Removed, "attributestate");
                    writer.Write(now, NpgsqlDbType.TimestampTz);
                    writer.Write(changesetID);
                }

                writer.Complete();
            }

            // TODO: update template errors

            return true;
        }
    }
}
