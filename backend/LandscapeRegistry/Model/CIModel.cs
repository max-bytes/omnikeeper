using Landscape.Base.Entity;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using LandscapeRegistry.Utils;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapeRegistry.Model
{
    public class CIModel : ICIModel
    {
        private readonly NpgsqlConnection conn;
        private readonly IAttributeModel attributeModel;
        //private static readonly AnchorState DefaultState = AnchorState.Active;

        public CIModel(IAttributeModel attributeModel, NpgsqlConnection connection)
        {
            this.attributeModel = attributeModel;
            conn = connection;
        }

        private string GetNameFromAttributes(IImmutableDictionary<string, MergedCIAttribute> attributes)
        {
            var nameA = attributes.GetValueOrDefault(ICIModel.NameAttribute, null);
            return nameA?.Attribute.Value.Value2String(); // TODO
        }
        private string GetNameFromAttributes(IEnumerable<CIAttribute> attributes)
        {
            var nameA = attributes.FirstOrDefault(a => a.Name == ICIModel.NameAttribute);
            return nameA?.Value.Value2String(); // TODO
        }

        private async Task<IDictionary<Guid, string>> GetCINames(LayerSet layerset, ICIIDSelection selection, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            var attributes = await attributeModel.FindMergedAttributesByFullName(ICIModel.NameAttribute, selection, layerset, trans, atTime);
            var AllSelectedCIIDs = await GetCIIDsFromSelection(selection, trans);
            return AllSelectedCIIDs.Select(ciid =>
            {
                attributes.TryGetValue(ciid, out var nameAttribute);
                return (ciid, name: nameAttribute?.Attribute.Value.Value2String());
            }).ToDictionary(kv => kv.ciid, kv => kv.name);
        }

        public async Task<CI> GetCI(Guid ciid, long layerID, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            var attributes = await attributeModel.GetAttributes(SpecificCIIDsSelection.Build(ciid), layerID, trans, atTime);
            var name = GetNameFromAttributes(attributes);
            return CI.Build(ciid, name, layerID, atTime, attributes);
        }

        public async Task<IEnumerable<CI>> GetCIs(ICIIDSelection selection, long layerID, bool includeEmptyCIs, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            var attributes = await attributeModel.GetAttributes(selection, layerID, trans, atTime);
            var groupedAttributes = attributes.GroupBy(a => a.CIID).ToDictionary(a => a.Key, a => a.ToList());
            if (includeEmptyCIs)
            {
                var allCIIds = await GetCIIDsFromSelection(selection, trans);
                var emptyCIs = allCIIds.Except(groupedAttributes.Select(a => a.Key)).ToDictionary(a => a, a => new List<CIAttribute>());
                groupedAttributes = groupedAttributes.Concat(emptyCIs).ToDictionary(a => a.Key, a => a.Value);
            }
            var t = groupedAttributes.Select(ga =>
            {
                var att = ga.Value;
                var name = GetNameFromAttributes(att);
                return CI.Build(ga.Key, name, layerID, atTime, att);
            });
            return t;
        }

        private async Task<IEnumerable<Guid>> GetCIIDsFromSelection(ICIIDSelection selection, NpgsqlTransaction trans)
        {
            return selection switch
            {
                AllCIIDsSelection _ => await GetCIIDs(trans),
                SpecificCIIDsSelection multiple => multiple.CIIDs,
                _ => null,// must not be
            };
        }

        public async Task<IEnumerable<CompactCI>> GetCompactCIs(ICIIDSelection selection, LayerSet visibleLayers, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            var CIIDs = await GetCIIDsFromSelection(selection, trans);
            var ciNames = await GetCINames(visibleLayers, selection, trans, atTime);
            // TODO: this actually returns empty compact CIs for ANY Guid/CI-ID, even ones that don't exist. check if that's expected, I believe not

            return CIIDs.Select(ciid => CompactCI.Build(ciid, ciNames[ciid], visibleLayers.LayerHash, atTime));
        }

        public async Task<IEnumerable<Guid>> GetCIIDs(NpgsqlTransaction trans)
        {
            using var command = new NpgsqlCommand(@"select id from ci", conn, trans);
            var tmp = new List<Guid>();
            using var s = await command.ExecuteReaderAsync();
            while (await s.ReadAsync())
                tmp.Add(s.GetGuid(0));
            return tmp;
        }

        public async Task<IEnumerable<Guid>> GetCIIDsOfNonEmptyCIs(LayerSet layerset, NpgsqlTransaction trans, TimeThreshold timeThreshold)
        {
            var ret = new HashSet<Guid>();

            /*
             * CI is non-empty if it has either non-removed attributes or non-removed relations
             */

            // attributes
            using (var command = new NpgsqlCommand(@$"select distinct on(ci_id, name, layer_id) ci_id, state from
                   attribute where timestamp <= @time_threshold and layer_id = ANY(@layer_ids) order by ci_id, name, layer_id, timestamp DESC
                ", conn, trans))
            {
                command.Parameters.AddWithValue("time_threshold", timeThreshold.Time);
                command.Parameters.AddWithValue("layer_ids", layerset.ToArray());
                using var dr = await command.ExecuteReaderAsync();

                while (await dr.ReadAsync())
                {
                    var CIID = dr.GetGuid(0);
                    var state = dr.GetFieldValue<AttributeState>(1);

                    if (state != AttributeState.Removed)
                        ret.Add(CIID);
                }
            }

            // relations
            using (var command = new NpgsqlCommand(@$"select distinct on(from_ci_id, to_ci_id, predicate_id, layer_id) from_ci_id, to_ci_id, state from
                    relation where timestamp <= @time_threshold and layer_id = ANY(@layer_ids) order by from_ci_id, to_ci_id, predicate_id, layer_id, timestamp DESC
                ", conn, trans))
            {
                command.Parameters.AddWithValue("time_threshold", timeThreshold.Time);
                command.Parameters.AddWithValue("layer_ids", layerset.ToArray());
                using var dr = await command.ExecuteReaderAsync();

                while (await dr.ReadAsync())
                {
                    var fromCIID = dr.GetGuid(0);
                    var toCIID = dr.GetGuid(1);
                    var state = dr.GetFieldValue<RelationState>(2);

                    if (state != RelationState.Removed)
                    {
                        ret.Add(fromCIID);
                        ret.Add(toCIID);
                    }
                }
            }

            return ret;
        }

        public async Task<bool> CIIDExists(Guid id, NpgsqlTransaction trans)
        {
            using var command = new NpgsqlCommand(@"select id from ci WHERE id = @ciid LIMIT 1", conn, trans);
            command.Parameters.AddWithValue("ciid", id);

            using var dr = await command.ExecuteReaderAsync();

            if (!await dr.ReadAsync())
                return false;
            return true;
        }

        public async Task<MergedCI> GetMergedCI(Guid ciid, LayerSet layers, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            var tmp = await attributeModel.GetMergedAttributes(SpecificCIIDsSelection.Build(ciid), layers, trans, atTime);
            var attributes = tmp.GetValueOrDefault(ciid, ImmutableDictionary<string, MergedCIAttribute>.Empty);
            var name = GetNameFromAttributes(attributes);
            return MergedCI.Build(ciid, name, layers, atTime, attributes);
        }

        public async Task<IEnumerable<MergedCI>> GetMergedCIs(ICIIDSelection selection, LayerSet layers, bool includeEmptyCIs, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            var attributes = await attributeModel.GetMergedAttributes(selection, layers, trans, atTime);

            if (includeEmptyCIs)
            {
                // check which ciids we already got and which are empty, add the empty ones
                var AllSelectedCIIDs = await GetCIIDsFromSelection(selection, trans);
                IImmutableDictionary<Guid, IImmutableDictionary<string, MergedCIAttribute>> emptyCIs = AllSelectedCIIDs.Except(attributes.Keys).ToImmutableDictionary(a => a, a => (IImmutableDictionary<string, MergedCIAttribute>)ImmutableDictionary<string, MergedCIAttribute>.Empty);
                attributes = attributes.Concat(emptyCIs).ToImmutableDictionary(a => a.Key, a => a.Value);
            }

            var ret = new List<MergedCI>();
            foreach (var ga in attributes)
            {
                var att = ga.Value;
                var name = GetNameFromAttributes(att);
                ret.Add(MergedCI.Build(ga.Key, name, layers, atTime, att));
            }
            return ret;
        }

        private Guid CreateCIID() => Guid.NewGuid();

        public async Task<Guid> CreateCI(NpgsqlTransaction trans) => await CreateCI(CreateCIID(), trans);
        public async Task<Guid> CreateCI(Guid id, NpgsqlTransaction trans)
        {
            using var command = new NpgsqlCommand(@"INSERT INTO ci (id) VALUES (@id)", conn, trans);
            command.Parameters.AddWithValue("id", id);
            await command.ExecuteNonQueryAsync();
            return id;
        }
    }
}
