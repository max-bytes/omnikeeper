using Landscape.Base.Entity;
using Landscape.Base.Model;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Landscape.Base.Model.IAttributeModel;

namespace LandscapeRegistry.Model
{
    public class CIModel : ICIModel
    {
        private readonly NpgsqlConnection conn;
        private readonly IAttributeModel attributeModel;
        private static readonly AnchorState DefaultState = AnchorState.Active;

        public CIModel(IAttributeModel attributeModel, NpgsqlConnection connection)
        {
            this.attributeModel = attributeModel;
            conn = connection;
        }

        public async Task<string> CreateCI(string identity, NpgsqlTransaction trans)
        {
            using var command = new NpgsqlCommand(@"INSERT INTO ci (id) VALUES (@id)", conn, trans);
            command.Parameters.AddWithValue("id", identity);
            await command.ExecuteNonQueryAsync();
            return identity;
        }

        public async Task<CIType> InsertCIType(string typeID, NpgsqlTransaction trans)
        {
            using var command = new NpgsqlCommand(@"INSERT INTO citype (id) VALUES (@id)", conn, trans);
            command.Parameters.AddWithValue("id", typeID);
            var r = await command.ExecuteNonQueryAsync();
            return CIType.Build(typeID, AnchorState.Active);
        }

        public async Task<CIType> UpsertCIType(string id, AnchorState state, NpgsqlTransaction trans)
        {
            var current = await GetCIType(id, trans, null);

            if (current == null)
                current = await InsertCIType(id, trans);

            // update state
            if (current.State != state)
            {
                using var commandState = new NpgsqlCommand(@"INSERT INTO citype_state (citype_id, state, ""timestamp"")
                    VALUES (@citype_id, @state, now())", conn, trans);
                commandState.Parameters.AddWithValue("citype_id", id);
                commandState.Parameters.AddWithValue("state", state);
                await commandState.ExecuteNonQueryAsync();
                current = CIType.Build(id, state);
            }

            return current;
        }

        public async Task<MergedCI> GetMergedCI(string ciid, LayerSet layers, NpgsqlTransaction trans, DateTimeOffset atTime)
        {
            var type = await GetTypeOfCI(ciid, trans, atTime);
            var attributes = await attributeModel.GetMergedAttributes(ciid, false, layers, trans, atTime);
            return MergedCI.Build(ciid, type, layers, atTime, attributes);
        }

        public async Task<CI> GetCI(string ciid, long layerID, NpgsqlTransaction trans, DateTimeOffset atTime)
        {
            var type = await GetTypeOfCI(ciid, trans, atTime);
            var attributes = await attributeModel.GetAttributes(new SingleCIIDAttributeSelection(ciid), false, layerID, trans, atTime);
            return CI.Build(ciid, type, layerID, atTime, attributes);
        }

        public async Task<IEnumerable<CI>> GetCIs(long layerID, bool includeEmptyCIs, NpgsqlTransaction trans, DateTimeOffset atTime)
        {
            var attributes = await attributeModel.GetAttributes(new AllCIIDsAttributeSelection(), false, layerID, trans, atTime);
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
            // TODO: performance improvements?
            var r = await GetTypeOfCIs(new string[] { ciid }, trans, atTime);
            return r.Values.FirstOrDefault();
        }

        public async Task<IDictionary<string, CIType>> GetTypeOfCIs(IEnumerable<string> ciids, NpgsqlTransaction trans, DateTimeOffset? atTime)
        {
            using var command = new NpgsqlCommand(@"SELECT DISTINCT ON (cta.ci_id) cta.ci_id, inn.id, inn.state FROM citype_assignment cta
                INNER JOIN 
                    (SELECT ct.id, cts.state FROM citype ct
                        LEFT JOIN (SELECT DISTINCT ON (citype_id) citype_id, state FROM citype_state WHERE timestamp <= @atTime ORDER BY citype_id, timestamp DESC) cts ON cts.citype_id = ct.id
                    ) inn ON inn.id = cta.citype_id
                WHERE cta.ci_id = ANY(@ci_ids)
                ORDER BY cta.ci_id, cta.timestamp DESC
            ", conn, trans);
            command.Parameters.AddWithValue("atTime", atTime ?? DateTimeOffset.Now);
            command.Parameters.AddWithValue("ci_ids", ciids.ToArray());

            var ret = new Dictionary<string, CIType>();
            var notYetFoundCIIDs = new HashSet<string>(ciids);
            using (var s = await command.ExecuteReaderAsync())
            {
                while (await s.ReadAsync())
                {
                    var ciid = s.GetString(0);
                    var typeID = s.GetString(1);
                    var state = (s.IsDBNull(2)) ? DefaultState : s.GetFieldValue<AnchorState>(2);
                    ret.Add(ciid, CIType.Build(typeID, state));
                    notYetFoundCIIDs.Remove(ciid);
                }
            }

            foreach (var notFoundCIID in notYetFoundCIIDs)
            {
                ret[notFoundCIID] = CIType.UnspecifiedCIType;
            }

            return ret;
        }

        public async Task<CIType> GetCITypeByID(string typeID, NpgsqlTransaction trans, DateTimeOffset? atTime)
        {
            using var command = new NpgsqlCommand(@"SELECT ct.id, cis.state FROM citype ct
                LEFT JOIN (SELECT DISTINCT ON (citype_id) citype_id, state FROM citype_state WHERE timestamp <= @atTime ORDER BY citype_id, timestamp DESC) cis ON cis.citype_id = ct.id
                WHERE ct.id = @citype_id LIMIT 1", conn, trans);
            command.Parameters.AddWithValue("citype_id", typeID);
            command.Parameters.AddWithValue("atTime", atTime ?? DateTimeOffset.Now);

            using var dr = await command.ExecuteReaderAsync();

            if (!await dr.ReadAsync())
                return null;

            var typeIDOut = dr.GetString(0);
            var state = (dr.IsDBNull(1)) ? DefaultState : dr.GetFieldValue<AnchorState>(1);
            return CIType.Build(typeIDOut, state);
        }

        public async Task<IEnumerable<CIType>> GetCITypes(NpgsqlTransaction trans, DateTimeOffset? atTime)
        {
            var ret = new List<CIType>();
            using var command = new NpgsqlCommand(@"SELECT ct.id, cis.state FROM citype ct
                LEFT JOIN (SELECT DISTINCT ON (citype_id) citype_id, state FROM citype_state WHERE timestamp <= @atTime ORDER BY citype_id, timestamp DESC) cis ON cis.citype_id = ct.id", conn, trans);

            command.Parameters.AddWithValue("atTime", atTime ?? DateTimeOffset.Now);
            using (var s = await command.ExecuteReaderAsync())
            {
                while (await s.ReadAsync())
                {
                    var id = s.GetString(0);
                    var state = (s.IsDBNull(1)) ? DefaultState : s.GetFieldValue<AnchorState>(1);
                    ret.Add(CIType.Build(id, state));
                }
            }

            return ret;
        }
        private async Task<CIType> GetCIType(string typeID, NpgsqlTransaction trans, DateTimeOffset? atTime)
        {
            // TODO: performance improvements
            return (await GetCITypes(trans, atTime)).FirstOrDefault(t => t.ID == typeID); 
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
        public async Task<bool> CIIDExists(string ciid, NpgsqlTransaction trans)
        {
            using var command = new NpgsqlCommand(@"select id from ci WHERE id = @ciid LIMIT 1", conn, trans);
            command.Parameters.AddWithValue("ciid", ciid);

            using var dr = await command.ExecuteReaderAsync();

            if (!await dr.ReadAsync())
                return false;
            return true;
        }

        public async Task<IEnumerable<MergedCI>> GetMergedCIs(LayerSet layers, bool includeEmptyCIs, NpgsqlTransaction trans, DateTimeOffset atTime, IEnumerable<string> CIIDs = null)
        {
            if (CIIDs == null) CIIDs = await GetCIIDs(trans);
            var attributes = await attributeModel.GetMergedAttributes(CIIDs, false, layers, trans, atTime);

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


        private async Task<CIType> CheckIfCITypeIDExists(string ciTypeID, NpgsqlTransaction trans, DateTimeOffset? atTime)
        {
            var inDB = await GetCITypeByID(ciTypeID, trans, atTime);

            if (inDB == null)
            {
                if (CIType.UnspecifiedCIType.ID == ciTypeID)
                {
                    return await InsertCIType(CIType.UnspecifiedCIType.ID, trans);
                }
                else
                {
                    return null;
                }
            }
            else return inDB;
        }
        public async Task<string> CreateCIWithType(string identity, string typeID, NpgsqlTransaction trans)
        {
            var type = await CheckIfCITypeIDExists(typeID, trans, null);
            if (type == null)
                throw new Exception($"Could not find CI-Type {typeID} in database");

            using var command = new NpgsqlCommand(@"INSERT INTO ci (id) VALUES (@id)", conn, trans);
            command.Parameters.AddWithValue("id", identity);
            await command.ExecuteNonQueryAsync();

            using var commandAssignment = new NpgsqlCommand(@"INSERT INTO citype_assignment (ci_id, citype_id, timestamp) VALUES
                (@ci_id, @citype_id, NOW())", conn, trans);
            commandAssignment.Parameters.AddWithValue("ci_id", identity);
            commandAssignment.Parameters.AddWithValue("citype_id", type.ID);
            await commandAssignment.ExecuteNonQueryAsync();

            return identity;
        }

        public async Task<bool> UpdateCI(string ciid, string typeID, NpgsqlTransaction trans)
        {
            if (!await CIIDExists(ciid, trans))
                throw new Exception($"Could not find CI {ciid} in database");
            var type = await CheckIfCITypeIDExists(typeID, trans, null);
            if (type == null)
                throw new Exception($"Could not find CI-Type {typeID} in database");

            using var command = new NpgsqlCommand(@"INSERT INTO citype_assignment (ci_id, citype_id, timestamp) VALUES
                (@ci_id, @citype_id, NOW())", conn, trans);
            command.Parameters.AddWithValue("ci_id", ciid);
            command.Parameters.AddWithValue("citype_id", type.ID);
            await command.ExecuteNonQueryAsync();
            return true;
        }
    }
}
