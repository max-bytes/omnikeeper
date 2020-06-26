using Landscape.Base.Entity;
using Landscape.Base.Inbound;
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
    public partial class AttributeModel : IAttributeModel
    {
        private readonly IOnlineAccessProxy onlineAccessProxy;
        private readonly NpgsqlConnection conn;

        public AttributeModel(IOnlineAccessProxy onlineAccessProxy, NpgsqlConnection connection)
        {
            this.onlineAccessProxy = onlineAccessProxy;
            conn = connection;
        }

        public async Task<IDictionary<string, MergedCIAttribute>> GetMergedAttributes(Guid ciid, bool includeRemoved, LayerSet layers, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            var d = await GetMergedAttributes(new Guid[] { ciid }, includeRemoved, layers, trans, atTime);
            return d.GetValueOrDefault(ciid, () => new Dictionary<string, MergedCIAttribute>());
        }

        private IEnumerable<MergedCIAttribute> MergeAttributes(IEnumerable<(CIAttribute attribute, long layerID)> attributes, LayerSet layers)
        {
            var compound = new Dictionary<(Guid ciid, string name), SortedList<int, (CIAttribute attribute, long layerID)>>();

            foreach(var (attribute, layerID) in attributes) {
                var layerSortOrder = layers.GetOrder(layerID);

                compound.AddOrUpdate((attribute.CIID, attribute.Name),
                    () => new SortedList<int, (CIAttribute attribute, long layerID)>() { { layerSortOrder, (attribute, layerID) } },
                    (old) => { old.Add(layerSortOrder, (attribute, layerID)); return old; }
                );
            }

            return compound.Select(t => MergedCIAttribute.Build(t.Value.First().Value.attribute, layerStackIDs: t.Value.Select(tt => tt.Value.layerID).Reverse().ToArray()));
        }

        public async Task<IDictionary<Guid, IDictionary<string, MergedCIAttribute>>> GetMergedAttributes(IEnumerable<Guid> ciids, bool includeRemoved, LayerSet layers, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            var ret = new Dictionary<Guid, IDictionary<string, MergedCIAttribute>>();

            if (layers.IsEmpty)
                return ret; // return empty, an empty layer list can never produce any attributes

            var attributes = new List<(CIAttribute attribute, long layerID)>();

            // we get the most recent value for each CI+attribute_name combination using SQL, then sort programmatically later (due to external layers)

            // internal attributes
            using (var command = new NpgsqlCommand(@$"select distinct on(ci_id, name, layer_id) id, name, ci_id, type, value, state, changeset_id, layer_id from
                   attribute where timestamp <= @time_threshold and ci_id = ANY(@ci_identities) and layer_id = ANY(@layer_ids) order by ci_id, name, layer_id, timestamp DESC
            ", conn, trans))
            {
                command.Parameters.AddWithValue("ci_identities", ciids.ToArray());
                command.Parameters.AddWithValue("time_threshold", atTime.Time);
                command.Parameters.AddWithValue("layer_ids", layers.ToArray());
                using var dr = await command.ExecuteReaderAsync();

                while (await dr.ReadAsync())
                {
                    var id = dr.GetInt64(0);
                    var name = dr.GetString(1);
                    var CIID = dr.GetGuid(2);
                    var type = dr.GetFieldValue<AttributeValueType>(3);
                    var value = dr.GetString(4);
                    var av = AttributeValueBuilder.BuildFromDatabase(value, type);

                    var state = dr.GetFieldValue<AttributeState>(5);
                    var changesetID = dr.GetInt64(6);
                    var layerID = dr.GetInt64(7); // TODO: optimization to only get the layerID, name and CIID first, check if it is even above the current stack, discard if so

                    if (includeRemoved || state != AttributeState.Removed)
                        attributes.Add((CIAttribute.Build(id, name, CIID, av, state, changesetID), layerID));
                }
            }

            // TODO: keep async nature further?
            var onlineAttributes = await onlineAccessProxy.GetAttributes(ciids.ToHashSet(), layers, trans).ToListAsync(); // TODO: rework ciids to set from the start

            var mergedAttributes = MergeAttributes(attributes.Concat(onlineAttributes), layers);

            foreach (var ma in mergedAttributes)
            {
                var CIID = ma.Attribute.CIID;
                if (!ret.ContainsKey(CIID))
                    ret.Add(CIID, new Dictionary<string, MergedCIAttribute>());
                ret[CIID].Add(ma.Attribute.Name, ma);
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

            if (layers.IsEmpty)
                return ret; // return empty, an empty layer list can never produce any attributes

            var attributes = new List<(CIAttribute attribute, long layerID)>();
            using (var command = new NpgsqlCommand(@$"
                select distinct on (ci_id, name, layer_id) id, ci_id, type, value, state, changeset_id, layer_id from
                    attribute where timestamp <= @time_threshold and ({selection.WhereClause}) and name = @name and layer_id = ANY(@layer_ids) order by ci_id, name, layer_id, timestamp DESC
            ", conn, trans))
            {
                command.Parameters.AddWithValue("time_threshold", atTime.Time);
                command.Parameters.AddWithValue("name", name);
                command.Parameters.AddWithValue("layer_ids", layers.ToArray());
                selection.AddParameters(command.Parameters);
                using var dr = await command.ExecuteReaderAsync();

                while (await dr.ReadAsync())
                {
                    var id = dr.GetInt64(0);
                    var CIID = dr.GetGuid(1);
                    var type = dr.GetFieldValue<AttributeValueType>(2);
                    var value = dr.GetString(3);
                    var av = AttributeValueBuilder.BuildFromDatabase(value, type);

                    var state = dr.GetFieldValue<AttributeState>(4);
                    var changesetID = dr.GetInt64(5);
                    var layerID = dr.GetInt64(6);

                    if (includeRemoved || state != AttributeState.Removed)
                        attributes.Add((CIAttribute.Build(id, name, CIID, av, state, changesetID), layerID));
                }
            }

            // TODO: keep async nature further?
            var onlineAttributes = await onlineAccessProxy.GetAttributesWithName(name, layers, trans).ToListAsync(); // TODO: rework ciids to set from the start

            var mergedAttributes = MergeAttributes(attributes.Concat(onlineAttributes), layers);

            foreach (var ma in mergedAttributes)
            {
                var CIID = ma.Attribute.CIID;
                ret.Add(CIID, ma);
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
    }
}
