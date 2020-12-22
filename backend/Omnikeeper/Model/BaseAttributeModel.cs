using Npgsql;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Model
{
    public partial class BaseAttributeModel : IBaseAttributeModel
    {
        public async Task<CIAttribute?> GetAttribute(string name, Guid ciid, long layerID, IModelContext trans, TimeThreshold atTime)
        {
            return await _GetAttribute(name, ciid, layerID, trans, atTime, false);
        }
        public async Task<CIAttribute?> GetFullBinaryAttribute(string name, Guid ciid, long layerID, IModelContext trans, TimeThreshold atTime)
        {
            return await _GetAttribute(name, ciid, layerID, trans, atTime, true);
        }

        private async Task<CIAttribute?> _GetAttribute(string name, Guid ciid, long layerID, IModelContext trans, TimeThreshold atTime, bool fullBinary)
        {
            using var command = new NpgsqlCommand(@"
            select id, ci_id, type, value_text, value_binary, value_control, state, changeset_id, origin_type FROM attribute 
            where timestamp <= @time_threshold and ci_id = @ci_id and layer_id = @layer_id and name = @name
            order by timestamp DESC LIMIT 1
            ", trans.DBConnection, trans.DBTransaction);
            command.Parameters.AddWithValue("ci_id", ciid);
            command.Parameters.AddWithValue("layer_id", layerID);
            command.Parameters.AddWithValue("name", name);
            command.Parameters.AddWithValue("time_threshold", atTime.Time);

            command.Prepare();

            using var dr = await command.ExecuteReaderAsync();

            if (!await dr.ReadAsync())
                return null;

            var id = dr.GetGuid(0);
            var CIID = dr.GetGuid(1);
            var type = dr.GetFieldValue<AttributeValueType>(2);
            var valueText = dr.GetString(3);
            var valueBinary = dr.GetFieldValue<byte[]>(4);
            var valueControl = dr.GetFieldValue<byte[]>(5);
            var av = Unmarshal(valueText, valueBinary, valueControl, type, fullBinary);
            var state = dr.GetFieldValue<AttributeState>(6);
            var changesetID = dr.GetGuid(7);
            var dataOriginType = dr.GetFieldValue<DataOriginType>(8);
            var origin = new DataOriginV1(dataOriginType);
            var att = new CIAttribute(id, name, CIID, av, state, changesetID, origin);
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

        public async Task<IEnumerable<CIAttribute>> GetAttributes(ICIIDSelection selection, long layerID, IModelContext trans, TimeThreshold atTime)
        {
            var ret = new List<CIAttribute>();

            using var command = new NpgsqlCommand($@"
            select distinct on(ci_id, name) state, id, name, ci_id, type, value_text, value_binary, value_control, changeset_id, origin_type FROM attribute 
            where timestamp <= @time_threshold and ({CIIDSelection2WhereClause(selection)}) and layer_id = @layer_id
            order by ci_id, name, timestamp DESC
            ", trans.DBConnection, trans.DBTransaction);
            AddQueryParametersFromCIIDSelection(selection, command.Parameters);
            command.Parameters.AddWithValue("layer_id", layerID);
            command.Parameters.AddWithValue("time_threshold", atTime.Time);

            command.Prepare();

            using var dr = await command.ExecuteReaderAsync();

            while (dr.Read())
            {
                var state = dr.GetFieldValue<AttributeState>(0);
                if (state != AttributeState.Removed)
                {
                    var id = dr.GetGuid(1);
                    var name = dr.GetString(2);
                    var CIID = dr.GetGuid(3);
                    var type = dr.GetFieldValue<AttributeValueType>(4);
                    var valueText = dr.GetString(5);
                    var valueBinary = dr.GetFieldValue<byte[]>(6);
                    var valueControl = dr.GetFieldValue<byte[]>(7);
                    var av = Unmarshal(valueText, valueBinary, valueControl, type, false);
                    var changesetID = dr.GetGuid(8);
                    var dataOriginType = dr.GetFieldValue<DataOriginType>(9);
                    var origin = new DataOriginV1(dataOriginType);

                    var att = new CIAttribute(id, name, CIID, av, state, changesetID, origin);
                    ret.Add(att);
                }
            }
            return ret;
        }

        public async Task<IEnumerable<CIAttribute>> FindAttributesByName(string regex, ICIIDSelection selection, long layerID, IModelContext trans, TimeThreshold atTime)
        {
            var ret = new List<CIAttribute>();

            using var command = new NpgsqlCommand($@"
            select distinct on(ci_id, name) id, name, ci_id, type, value_text, value_binary, value_control, state, changeset_id, origin_type from
                attribute where timestamp <= @time_threshold and layer_id = @layer_id and name ~ @regex and ({CIIDSelection2WhereClause(selection)}) order by ci_id, name, timestamp DESC
            ", trans.DBConnection, trans.DBTransaction);

            command.Parameters.AddWithValue("layer_id", layerID);
            command.Parameters.AddWithValue("regex", regex);
            command.Parameters.AddWithValue("time_threshold", atTime.Time);
            AddQueryParametersFromCIIDSelection(selection, command.Parameters);

            command.Prepare();

            using var dr = await command.ExecuteReaderAsync();
            while (dr.Read())
            {
                var id = dr.GetGuid(0);
                var name = dr.GetString(1);
                var CIID = dr.GetGuid(2);
                var type = dr.GetFieldValue<AttributeValueType>(3);
                var valueText = dr.GetString(4);
                var valueBinary = dr.GetFieldValue<byte[]>(5);
                var valueControl = dr.GetFieldValue<byte[]>(6);
                var av = Unmarshal(valueText, valueBinary, valueControl, type, false);
                var state = dr.GetFieldValue<AttributeState>(7);
                var changesetID = dr.GetGuid(8);
                var dataOriginType = dr.GetFieldValue<DataOriginType>(9);
                var origin = new DataOriginV1(dataOriginType);

                if (state != AttributeState.Removed)
                {
                    var att = new CIAttribute(id, name, CIID, av, state, changesetID, origin);
                    ret.Add(att);
                }
            }
            return ret;
        }

        public async Task<IEnumerable<CIAttribute>> FindAttributesByFullName(string name, ICIIDSelection selection, long layerID, IModelContext trans, TimeThreshold atTime)
        {
            var ret = new List<CIAttribute>();

            using (var command = new NpgsqlCommand(@$"
                select distinct on (ci_id) id, ci_id, type, value_text, value_binary, value_control, state, changeset_id, origin_type from
                    attribute where timestamp <= @time_threshold and ({CIIDSelection2WhereClause(selection)}) and name = @name and layer_id = @layer_id order by ci_id, name, timestamp DESC
            ", trans.DBConnection, trans.DBTransaction))
            {
                command.Parameters.AddWithValue("time_threshold", atTime.Time);
                command.Parameters.AddWithValue("name", name);
                command.Parameters.AddWithValue("layer_id", layerID);
                AddQueryParametersFromCIIDSelection(selection, command.Parameters);

                command.Prepare();

                using var dr = await command.ExecuteReaderAsync();

                while (await dr.ReadAsync())
                {
                    var id = dr.GetGuid(0);
                    var CIID = dr.GetGuid(1);
                    var type = dr.GetFieldValue<AttributeValueType>(2);
                    var valueText = dr.GetString(3);
                    var valueBinary = dr.GetFieldValue<byte[]>(4);
                    var valueControl = dr.GetFieldValue<byte[]>(5);
                    var av = Unmarshal(valueText, valueBinary, valueControl, type, false);
                    var state = dr.GetFieldValue<AttributeState>(6);
                    var changesetID = dr.GetGuid(7);
                    var dataOriginType = dr.GetFieldValue<DataOriginType>(8);
                    var origin = new DataOriginV1(dataOriginType);

                    if (state != AttributeState.Removed)
                        ret.Add(new CIAttribute(id, name, CIID, av, state, changesetID, origin));
                }
            }

            return ret;
        }

        public async Task<IEnumerable<Guid>> FindCIIDsWithAttribute(string name, ICIIDSelection selection, long layerID, IModelContext trans, TimeThreshold atTime)
        {
            var ret = new List<Guid>();

            using (var command = new NpgsqlCommand(@$"
                select distinct on (ci_id) ci_id from
                    attribute where timestamp <= @time_threshold and ({CIIDSelection2WhereClause(selection)}) and name = @name and layer_id = @layer_id order by ci_id, name, timestamp DESC
            ", trans.DBConnection, trans.DBTransaction))
            {
                command.Parameters.AddWithValue("time_threshold", atTime.Time);
                command.Parameters.AddWithValue("name", name);
                command.Parameters.AddWithValue("layer_id", layerID);
                AddQueryParametersFromCIIDSelection(selection, command.Parameters);

                command.Prepare();

                using var dr = await command.ExecuteReaderAsync();

                while (await dr.ReadAsync())
                {
                    var CIID = dr.GetGuid(0);
                    ret.Add(CIID);
                }
            }

            return ret;
        }

    }
}
