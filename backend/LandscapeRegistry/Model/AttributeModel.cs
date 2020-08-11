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
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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

        public async Task<CIAttribute> GetAttribute(string name, long layerID, Guid ciid, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            // if layer is online inbound layer, return from proxy
            if (await onlineAccessProxy.IsOnlineInboundLayer(layerID, trans))
            {
                return await onlineAccessProxy.GetAttribute(name, layerID, ciid, trans, atTime);
            }

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

            var id = dr.GetGuid(0);
            var CIID = dr.GetGuid(1);
            var type = dr.GetFieldValue<AttributeValueType>(2);
            var value = dr.GetString(3);
            var av = AttributeValueBuilder.BuildFromDatabase(value, type);
            var state = dr.GetFieldValue<AttributeState>(4);
            var changesetID = dr.GetInt64(5);
            var att = CIAttribute.Build(id, name, CIID, av, state, changesetID);
            return att;
        }

        public async Task<IEnumerable<CIAttribute>> GetAttributes(ICIIDSelection selection, bool includeRemoved, long layerID, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            // if layer is online inbound layer, return from proxy
            if (await onlineAccessProxy.IsOnlineInboundLayer(layerID, trans)) {
                return onlineAccessProxy.GetAttributes(selection, layerID, trans, atTime).ToEnumerable();
            }

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
                var id = dr.GetGuid(0);
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

        public async Task<IEnumerable<CIAttribute>> FindAttributesByName(string like, ICIIDSelection selection, long layerID, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            // if layer is online inbound layer, return from proxy
            if (await onlineAccessProxy.IsOnlineInboundLayer(layerID, trans))
            {
                // HACK: we crudly simulate an SQL like, see https://stackoverflow.com/questions/41757762/use-sql-like-operator-in-c-sharp-linq/41757857
                static string LikeToRegular(string value)
                {
                    return "^" + Regex.Escape(value).Replace("_", ".").Replace("%", ".*") + "$";
                }
                var regex = LikeToRegular(like);

                return onlineAccessProxy.FindAttributesByName(regex, selection, layerID, trans, atTime).ToEnumerable();
            }

            var ret = new List<CIAttribute>();

            using var command = new NpgsqlCommand($@"
            select distinct on(ci_id, name, layer_id) id, name, ci_id, type, value, state, changeset_id from
                attribute where timestamp <= @time_threshold and layer_id = @layer_id and name like @like_name and ({selection.WhereClause}) order by ci_id, name, layer_id, timestamp DESC
            ", conn, trans);

            command.Parameters.AddWithValue("layer_id", layerID);
            command.Parameters.AddWithValue("like_name", like);
            command.Parameters.AddWithValue("time_threshold", atTime.Time);
            selection.AddParameters(command.Parameters);

            using var dr = await command.ExecuteReaderAsync();
            while (dr.Read())
            {
                var id = dr.GetGuid(0);
                var name = dr.GetString(1);
                var CIID = dr.GetGuid(2);
                var type = dr.GetFieldValue<AttributeValueType>(3);
                var value = dr.GetString(4);
                var av = AttributeValueBuilder.BuildFromDatabase(value, type);
                var state = dr.GetFieldValue<AttributeState>(5);
                var changesetID = dr.GetInt64(6);

                if (state != AttributeState.Removed)
                {
                    var att = CIAttribute.Build(id, name, CIID, av, state, changesetID);
                    ret.Add(att);
                }
            }
            return ret;
        }

        public async Task<IEnumerable<CIAttribute>> FindAttributesByFullName(string name, ICIIDSelection selection, long layerID, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            // if layer is online inbound layer, return from proxy
            if (await onlineAccessProxy.IsOnlineInboundLayer(layerID, trans))
            {
                return onlineAccessProxy.FindAttributesByFullName(name, selection, layerID, trans, atTime).ToEnumerable();
            }

            var ret = new List<CIAttribute>();

            using (var command = new NpgsqlCommand(@$"
                select distinct on (ci_id, name) id, ci_id, type, value, state, changeset_id from
                    attribute where timestamp <= @time_threshold and ({selection.WhereClause}) and name = @name and layer_id = @layer_id order by ci_id, name, layer_id, timestamp DESC
            ", conn, trans))
            {
                command.Parameters.AddWithValue("time_threshold", atTime.Time);
                command.Parameters.AddWithValue("name", name);
                command.Parameters.AddWithValue("layer_id", layerID);
                selection.AddParameters(command.Parameters);
                using var dr = await command.ExecuteReaderAsync();

                while (await dr.ReadAsync())
                {
                    var id = dr.GetGuid(0);
                    var CIID = dr.GetGuid(1);
                    var type = dr.GetFieldValue<AttributeValueType>(2);
                    var value = dr.GetString(3);
                    var av = AttributeValueBuilder.BuildFromDatabase(value, type);

                    var state = dr.GetFieldValue<AttributeState>(4);
                    var changesetID = dr.GetInt64(5);

                    if (state != AttributeState.Removed) // TODO: move into SQL
                        ret.Add(CIAttribute.Build(id, name, CIID, av, state, changesetID));
                }
            }

            return ret;
        }


    }
}
