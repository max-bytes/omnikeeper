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

        public CIModel(IAttributeModel attributeModel, NpgsqlConnection connection)
        {
            this.attributeModel = attributeModel;
            conn = connection;
        }

        [Obsolete("Please use CreateCIWithType instead")]
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
            var ciType = await GetCITypeByID(typeID, trans);
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

        public async Task<CIType> GetCITypeByID(string typeID, NpgsqlTransaction trans)
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

        public async Task<bool> SetCIType(string ciid, string typeID, NpgsqlTransaction trans)
        {
            using var command = new NpgsqlCommand(@"INSERT INTO citype_assignment (ci_id, citype_id, timestamp) VALUES
                (@ci_id, @citype_id, NOW())", conn, trans);
            command.Parameters.AddWithValue("ci_id", ciid);
            command.Parameters.AddWithValue("citype_id", typeID);
            await command.ExecuteNonQueryAsync();
            return true;
        }
    }
}
