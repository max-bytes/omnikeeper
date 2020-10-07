using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Model
{
    public partial class BaseAttributeModel : IBaseAttributeModel
    {
        private readonly NpgsqlConnection conn;

        public BaseAttributeModel(NpgsqlConnection connection)
        {
            conn = connection;
        }

        public async Task<CIAttribute> GetAttribute(string name, Guid ciid, long layerID, NpgsqlTransaction trans, TimeThreshold atTime)
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

            var id = dr.GetGuid(0);
            var CIID = dr.GetGuid(1);
            var type = dr.GetFieldValue<AttributeValueType>(2);
            var value = dr.GetString(3);
            var av = AttributeValueBuilder.BuildFromDatabase(value, type);
            var state = dr.GetFieldValue<AttributeState>(4);
            var changesetID = dr.GetGuid(5);
            var att = CIAttribute.Build(id, name, CIID, av, state, changesetID);
            return att;
        }

        private string CIIDSelection2WhereClause(ICIIDSelection selection)
        {
            return selection switch
            {
                AllCIIDsSelection _ => "1=1",
                SpecificCIIDsSelection _ => "ci_id = ANY(@ci_ids)",
                _ => throw new NotImplementedException("")
            };
        }

        private void AddQueryParametersFromCIIDSelection(ICIIDSelection selection, NpgsqlParameterCollection p)
        {
            switch (selection)
            {
                case SpecificCIIDsSelection m:
                    p.AddWithValue("ci_ids", m.CIIDs);
                    break;
                default:
                    break;
            };
        }

        public async Task<IEnumerable<CIAttribute>> GetAttributes(ICIIDSelection selection, long layerID, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            var ret = new List<CIAttribute>();

            using var command = new NpgsqlCommand($@"
            select distinct on(ci_id, name) id, name, ci_id, type, value, state, changeset_id FROM attribute 
            where timestamp <= @time_threshold and ({CIIDSelection2WhereClause(selection)}) and layer_id = @layer_id
            order by ci_id, name, timestamp DESC
            ", conn, trans);
            AddQueryParametersFromCIIDSelection(selection, command.Parameters);
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
                var changesetID = dr.GetGuid(6);

                var att = CIAttribute.Build(id, name, CIID, av, state, changesetID);
                if (state != AttributeState.Removed)
                {
                    ret.Add(att);
                }
            }
            return ret;
        }

        public async Task<IEnumerable<CIAttribute>> FindAttributesByName(string regex, ICIIDSelection selection, long layerID, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            var ret = new List<CIAttribute>();

            using var command = new NpgsqlCommand($@"
            select distinct on(ci_id, name, layer_id) id, name, ci_id, type, value, state, changeset_id from
                attribute where timestamp <= @time_threshold and layer_id = @layer_id and name ~ @regex and ({CIIDSelection2WhereClause(selection)}) order by ci_id, name, layer_id, timestamp DESC
            ", conn, trans); // TODO: remove order by layer_id, but consider not breaking indices first

            command.Parameters.AddWithValue("layer_id", layerID);
            command.Parameters.AddWithValue("regex", regex);
            command.Parameters.AddWithValue("time_threshold", atTime.Time);
            AddQueryParametersFromCIIDSelection(selection, command.Parameters);

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
                var changesetID = dr.GetGuid(6);

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
            var ret = new List<CIAttribute>();

            using (var command = new NpgsqlCommand(@$"
                select distinct on (ci_id, name) id, ci_id, type, value, state, changeset_id from
                    attribute where timestamp <= @time_threshold and ({CIIDSelection2WhereClause(selection)}) and name = @name and layer_id = @layer_id order by ci_id, name, layer_id, timestamp DESC
            ", conn, trans))// TODO: remove order by layer_id, but consider not breaking indices first
            {
                command.Parameters.AddWithValue("time_threshold", atTime.Time);
                command.Parameters.AddWithValue("name", name);
                command.Parameters.AddWithValue("layer_id", layerID);
                AddQueryParametersFromCIIDSelection(selection, command.Parameters);
                using var dr = await command.ExecuteReaderAsync();

                while (await dr.ReadAsync())
                {
                    var id = dr.GetGuid(0);
                    var CIID = dr.GetGuid(1);
                    var type = dr.GetFieldValue<AttributeValueType>(2);
                    var value = dr.GetString(3);
                    var av = AttributeValueBuilder.BuildFromDatabase(value, type);

                    var state = dr.GetFieldValue<AttributeState>(4);
                    var changesetID = dr.GetGuid(5);

                    if (state != AttributeState.Removed)
                        ret.Add(CIAttribute.Build(id, name, CIID, av, state, changesetID));
                }
            }

            return ret;
        }


    }
}
