using Npgsql;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Utils;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Model
{
    public class CIModel : ICIModel
    {
        private readonly IAttributeModel attributeModel;

        public CIModel(IAttributeModel attributeModel)
        {
            this.attributeModel = attributeModel;
        }

        private string? GetNameFromAttributes(IImmutableDictionary<string, MergedCIAttribute> attributes)
        {
            if (attributes.TryGetValue(ICIModel.NameAttribute, out var nameA))
                return nameA.Attribute.Value.Value2String();
            return null; // TODO: we assume we can convert the name to a string, is this correct?
        }

        private async Task<IDictionary<Guid, string?>> GetCINames(LayerSet layerset, ICIIDSelection selection, IModelContext trans, TimeThreshold atTime)
        {
            IImmutableDictionary<Guid, MergedCIAttribute> attributes = await attributeModel.FindMergedAttributesByFullName(ICIModel.NameAttribute, selection, layerset, trans, atTime);
            var AllSelectedCIIDs = await GetCIIDsFromSelection(selection, trans);
            return AllSelectedCIIDs.Select(ciid =>
            {
                if (attributes.TryGetValue(ciid, out var nameAttribute))
                    return (ciid, name: nameAttribute?.Attribute.Value.Value2String());
                else
                    return (ciid, name: null);
            }).ToDictionary(kv => kv.ciid, kv => kv.name);
        }

        private async Task<IEnumerable<Guid>> GetCIIDsFromSelection(ICIIDSelection selection, IModelContext trans)
        {
            return selection switch
            {
                AllCIIDsSelection _ => await GetCIIDs(trans),
                SpecificCIIDsSelection multiple => multiple.CIIDs,
                _ => throw new NotImplementedException()
            };
        }

        public async Task<IEnumerable<CompactCI>> GetCompactCIs(ICIIDSelection selection, LayerSet visibleLayers, IModelContext trans, TimeThreshold atTime)
        {
            var CIIDs = await GetCIIDsFromSelection(selection, trans);
            var ciNames = await GetCINames(visibleLayers, selection, trans, atTime);
            // TODO: this actually returns empty compact CIs for ANY Guid/CI-ID, even ones that don't exist. check if that's expected, I believe not

            return CIIDs.Select(ciid => new CompactCI(ciid, ciNames[ciid], visibleLayers.LayerHash, atTime));
        }

        public async Task<IEnumerable<Guid>> GetCIIDs(IModelContext trans)
        {
            using var command = new NpgsqlCommand(@"select id from ci", trans.DBConnection, trans.DBTransaction);
            var tmp = new List<Guid>();
            using var s = await command.ExecuteReaderAsync();
            while (await s.ReadAsync())
                tmp.Add(s.GetGuid(0));
            return tmp;
        }

        public async Task<IEnumerable<Guid>> GetCIIDsOfNonEmptyCIs(LayerSet layerset, IModelContext trans, TimeThreshold timeThreshold)
        {
            var ret = new HashSet<Guid>();

            /*
             * CI is non-empty if it has either non-removed attributes or non-removed relations
             */

            // attributes
            using (var command = new NpgsqlCommand(@$"select distinct on(ci_id, name, layer_id) ci_id, state from
                   attribute where timestamp <= @time_threshold and layer_id = ANY(@layer_ids) order by ci_id, name, layer_id, timestamp DESC
                ", trans.DBConnection, trans.DBTransaction))
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
                ", trans.DBConnection, trans.DBTransaction))
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

        public async Task<bool> CIIDExists(Guid id, IModelContext trans)
        {
            using var command = new NpgsqlCommand(@"select id from ci WHERE id = @ciid LIMIT 1", trans.DBConnection, trans.DBTransaction);
            command.Parameters.AddWithValue("ciid", id);

            using var dr = await command.ExecuteReaderAsync();

            if (!await dr.ReadAsync())
                return false;
            return true;
        }

        public async Task<MergedCI> GetMergedCI(Guid ciid, LayerSet layers, IModelContext trans, TimeThreshold atTime)
        {
            var tmp = await attributeModel.GetMergedAttributes(SpecificCIIDsSelection.Build(ciid), layers, trans, atTime);
            var attributes = tmp.GetValueOrDefault(ciid, ImmutableDictionary<string, MergedCIAttribute>.Empty);
            var name = GetNameFromAttributes(attributes);
            return new MergedCI(ciid, name, layers, atTime, attributes);
        }

        public async Task<IEnumerable<MergedCI>> GetMergedCIs(ICIIDSelection selection, LayerSet layers, bool includeEmptyCIs, IModelContext trans, TimeThreshold atTime)
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
                ret.Add(new MergedCI(ga.Key, name, layers, atTime, att));
            }
            return ret;
        }

        private Guid CreateCIID() => Guid.NewGuid();

        public async Task<Guid> CreateCI(IModelContext trans) => await CreateCI(CreateCIID(), trans);
        public async Task<Guid> CreateCI(Guid id, IModelContext trans)
        {
            using var command = new NpgsqlCommand(@"INSERT INTO ci (id) VALUES (@id)", trans.DBConnection, trans.DBTransaction);
            command.Parameters.AddWithValue("id", id);
            await command.ExecuteNonQueryAsync();
            return id;
        }
    }
}
