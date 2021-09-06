using Npgsql;
using Omnikeeper.Base.AttributeValues;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Model
{
    public partial class BaseAttributeModel : IBaseAttributeModel
    {
        private readonly IPartitionModel partitionModel;
        private readonly ICIIDModel ciidModel;
        public static bool _USE_LATEST_TABLE = true;

        public BaseAttributeModel(IPartitionModel partitionModel, ICIIDModel ciidModel)
        {
            this.partitionModel = partitionModel;
            this.ciidModel = ciidModel;
        }

        public async Task<CIAttribute?> GetAttribute(string name, Guid ciid, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            return await _GetAttribute(name, ciid, layerID, trans, atTime, false);
        }
        public async Task<CIAttribute?> GetFullBinaryAttribute(string name, Guid ciid, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            return await _GetAttribute(name, ciid, layerID, trans, atTime, true);
        }

        private async Task<CIAttribute?> _GetAttribute(string name, Guid ciid, string layerID, IModelContext trans, TimeThreshold atTime, bool fullBinary)
        {
            NpgsqlCommand command;
            if (atTime.IsLatest && _USE_LATEST_TABLE)
            {
                command = new NpgsqlCommand(@"
                select id, ci_id, type, value_text, value_binary, value_control, state, changeset_id FROM attribute_latest
                where ci_id = @ci_id and layer_id = @layer_id and name = @name LIMIT 1
                ", trans.DBConnection, trans.DBTransaction);
                command.Parameters.AddWithValue("ci_id", ciid);
                command.Parameters.AddWithValue("layer_id", layerID);
                command.Parameters.AddWithValue("name", name);
            }
            else
            {
                var partitionIndex = await partitionModel.GetLatestPartitionIndex(atTime, trans);

                command = new NpgsqlCommand(@"
                select id, ci_id, type, value_text, value_binary, value_control, state, changeset_id FROM attribute 
                where timestamp <= @time_threshold and ci_id = @ci_id and layer_id = @layer_id and name = @name and partition_index >= @partition_index
                order by timestamp DESC NULLS LAST LIMIT 1
                ", trans.DBConnection, trans.DBTransaction);
                command.Parameters.AddWithValue("ci_id", ciid);
                command.Parameters.AddWithValue("layer_id", layerID);
                command.Parameters.AddWithValue("name", name);
                command.Parameters.AddWithValue("time_threshold", atTime.Time);
                command.Parameters.AddWithValue("partition_index", partitionIndex);
            }

            command.Prepare();

            using var dr = await command.ExecuteReaderAsync();

            command.Dispose();

            if (!await dr.ReadAsync())
                return null;

            var id = dr.GetGuid(0);
            var CIID = dr.GetGuid(1);
            var type = dr.GetFieldValue<AttributeValueType>(2);
            var valueText = dr.GetString(3);
            var valueBinary = dr.GetFieldValue<byte[]>(4);
            var valueControl = dr.GetFieldValue<byte[]>(5);
            var av = AttributeValueBuilder.Unmarshal(valueText, valueBinary, valueControl, type, fullBinary);
            var state = dr.GetFieldValue<AttributeState>(6);
            var changesetID = dr.GetGuid(7);
            var att = new CIAttribute(id, name, CIID, av, state, changesetID);
            return att;
        }

        private string CIIDSelection2WhereClause(ICIIDSelection selection)
        {
            return selection switch
            {
                AllCIIDsSelection _ => "1=1",
                SpecificCIIDsSelection _ => "ci_id = ANY(@ci_ids)", // TODO: performance test the in, some places suggest its slow: https://dba.stackexchange.com/questions/91247/optimizing-a-postgres-query-with-a-large-in
                AllCIIDsExceptSelection _ => "ci_id <> ALL(@ci_ids)", // TODO: performance test the in, some places suggest its slow: https://dba.stackexchange.com/questions/91247/optimizing-a-postgres-query-with-a-large-in
                NoCIIDsSelection _ => "1=0",
                _ => throw new NotImplementedException("")
            };
        }

        private void AddQueryParametersFromCIIDSelection(ICIIDSelection selection, NpgsqlParameterCollection p)
        {
            switch (selection)
            {
                case SpecificCIIDsSelection m:
                    p.AddWithValue("ci_ids", m.CIIDs.ToList());
                    break;
                case AllCIIDsExceptSelection a:
                    p.AddWithValue("ci_ids", a.ExceptCIIDs.ToList());
                    break;
                default:
                    break;
            };
        }

        // NOTE: this exists because querying using AllCIIDsExceptSelection is very slow when the list of excluded CIIDs gets large
        // that's why we - under certain circumstances - flip the selection and turn the AllCIIDsExceptSelection into a SpecificCIIDsSelection
        private static readonly int CIIDSELECTION_ALL_EXCEPT_ABS_THRESHOLD = 100;
        private static readonly float CIIDSELECTION_ALL_EXCEPT_PERCENTAGE_THRESHOLD = 0.5f;
        private async Task<ICIIDSelection> OptimizeCIIDSelection(ICIIDSelection selection, IModelContext trans)
        {
            if (selection is AllCIIDsExceptSelection ae)
            {
                if (ae.ExceptCIIDs.Count >= CIIDSELECTION_ALL_EXCEPT_ABS_THRESHOLD)
                {
                    var allCIIDs = await ciidModel.GetCIIDs(trans);
                    var allCIIDsCount = allCIIDs.Count();
                    if (allCIIDsCount > 0 && (float)ae.ExceptCIIDs.Count / (float)allCIIDsCount >= CIIDSELECTION_ALL_EXCEPT_PERCENTAGE_THRESHOLD)
                    {
                        var specific = allCIIDs.Except(ae.ExceptCIIDs).ToHashSet();
                        return SpecificCIIDsSelection.Build(specific);
                    }
                }
            }

            return selection;
        }

        public async Task<IEnumerable<CIAttribute>> GetAttributes(ICIIDSelection selection, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            selection = await OptimizeCIIDSelection(selection, trans);

            NpgsqlCommand command;
            if (atTime.IsLatest && _USE_LATEST_TABLE)
            {
                command = new NpgsqlCommand($@"
                select state, id, name, ci_id, type, value_text, value_binary, value_control, changeset_id FROM attribute_latest
                where ({CIIDSelection2WhereClause(selection)}) and layer_id = @layer_id", trans.DBConnection, trans.DBTransaction);
                AddQueryParametersFromCIIDSelection(selection, command.Parameters);
                command.Parameters.AddWithValue("layer_id", layerID);
            }
            else
            {
                var partitionIndex = await partitionModel.GetLatestPartitionIndex(atTime, trans);

                command = new NpgsqlCommand($@"
                select distinct on(ci_id, name) state, id, name, ci_id, type, value_text, value_binary, value_control, changeset_id FROM attribute 
                where timestamp <= @time_threshold and ({CIIDSelection2WhereClause(selection)}) and layer_id = @layer_id and partition_index >= @partition_index
                order by ci_id, name, timestamp DESC NULLS LAST
                ", trans.DBConnection, trans.DBTransaction);
                AddQueryParametersFromCIIDSelection(selection, command.Parameters);
                command.Parameters.AddWithValue("layer_id", layerID);
                command.Parameters.AddWithValue("time_threshold", atTime.Time);
                command.Parameters.AddWithValue("partition_index", partitionIndex);
            }

            command.Prepare();

            using var dr = await command.ExecuteReaderAsync();

            command.Dispose();

            var ret = new List<CIAttribute>();
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
                    var av = AttributeValueBuilder.Unmarshal(valueText, valueBinary, valueControl, type, false);
                    var changesetID = dr.GetGuid(8);

                    var att = new CIAttribute(id, name, CIID, av, state, changesetID);
                    ret.Add(att);
                }
            }
            return ret;
        }


        public async Task<IEnumerable<CIAttribute>> GetAttributesOfChangeset(Guid changesetID, IModelContext trans)
        {
            var ret = new List<CIAttribute>();
            using var command = new NpgsqlCommand($@"
            select state, id, name, ci_id, type, value_text, value_binary, value_control FROM attribute 
            where changeset_id = @changeset_id
            ", trans.DBConnection, trans.DBTransaction);
            command.Parameters.AddWithValue("changeset_id", changesetID);

            command.Prepare();

            using var dr = await command.ExecuteReaderAsync();

            while (dr.Read())
            {
                var state = dr.GetFieldValue<AttributeState>(0);
                var id = dr.GetGuid(1);
                var name = dr.GetString(2);
                var CIID = dr.GetGuid(3);
                var type = dr.GetFieldValue<AttributeValueType>(4);
                var valueText = dr.GetString(5);
                var valueBinary = dr.GetFieldValue<byte[]>(6);
                var valueControl = dr.GetFieldValue<byte[]>(7);
                var av = AttributeValueBuilder.Unmarshal(valueText, valueBinary, valueControl, type, false);

                var att = new CIAttribute(id, name, CIID, av, state, changesetID);
                ret.Add(att);
            }
            return ret;
        }

        public async Task<IEnumerable<CIAttribute>> FindAttributesByName(string regex, ICIIDSelection selection, string layerID, bool returnRemoved, IModelContext trans, TimeThreshold atTime)
        {
            selection = await OptimizeCIIDSelection(selection, trans);

            NpgsqlCommand command;
            if (atTime.IsLatest && _USE_LATEST_TABLE)
            {
                command = new NpgsqlCommand($@"
                select state, id, name, ci_id, type, value_text, value_binary, value_control, changeset_id from
                    attribute_latest where layer_id = @layer_id and name ~ @regex and ({CIIDSelection2WhereClause(selection)})
                ", trans.DBConnection, trans.DBTransaction);

                command.Parameters.AddWithValue("layer_id", layerID);
                command.Parameters.AddWithValue("regex", regex);
                AddQueryParametersFromCIIDSelection(selection, command.Parameters);
            }
            else
            {
                var partitionIndex = await partitionModel.GetLatestPartitionIndex(atTime, trans);

                command = new NpgsqlCommand($@"
                select distinct on(ci_id, name) state, id, name, ci_id, type, value_text, value_binary, value_control, changeset_id from
                    attribute where timestamp <= @time_threshold and layer_id = @layer_id and name ~ @regex and ({CIIDSelection2WhereClause(selection)}) 
                    and partition_index >= @partition_index
                    order by ci_id, name, timestamp DESC NULLS LAST
                ", trans.DBConnection, trans.DBTransaction);

                command.Parameters.AddWithValue("layer_id", layerID);
                command.Parameters.AddWithValue("regex", regex);
                command.Parameters.AddWithValue("time_threshold", atTime.Time);
                command.Parameters.AddWithValue("partition_index", partitionIndex);
                AddQueryParametersFromCIIDSelection(selection, command.Parameters);
            }

            command.Prepare();

            var ret = new List<CIAttribute>();
            using var dr = await command.ExecuteReaderAsync();
            while (dr.Read())
            {
                var state = dr.GetFieldValue<AttributeState>(0);
                if (state != AttributeState.Removed || returnRemoved)
                {
                    var id = dr.GetGuid(1);
                    var name = dr.GetString(2);
                    var CIID = dr.GetGuid(3);
                    var type = dr.GetFieldValue<AttributeValueType>(4);
                    var valueText = dr.GetString(5);
                    var valueBinary = dr.GetFieldValue<byte[]>(6);
                    var valueControl = dr.GetFieldValue<byte[]>(7);
                    var av = AttributeValueBuilder.Unmarshal(valueText, valueBinary, valueControl, type, false);
                    var changesetID = dr.GetGuid(8);
                    var att = new CIAttribute(id, name, CIID, av, state, changesetID);
                    ret.Add(att);
                }
            }

            command.Dispose();

            return ret;
        }

        public async Task<IEnumerable<CIAttribute>> FindAttributesByFullName(string name, ICIIDSelection selection, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            selection = await OptimizeCIIDSelection(selection, trans);

            NpgsqlCommand command;
            if (atTime.IsLatest && _USE_LATEST_TABLE)
            {
                command = new NpgsqlCommand(@$"
                    select state, id, ci_id, type, value_text, value_binary, value_control, changeset_id from
                        attribute_latest where ({CIIDSelection2WhereClause(selection)}) and name = @name and layer_id = @layer_id
                ", trans.DBConnection, trans.DBTransaction);

                command.Parameters.AddWithValue("name", name);
                command.Parameters.AddWithValue("layer_id", layerID);
                AddQueryParametersFromCIIDSelection(selection, command.Parameters);
            }
            else
            {
                var partitionIndex = await partitionModel.GetLatestPartitionIndex(atTime, trans);

                command = new NpgsqlCommand(@$"
                    select distinct on (ci_id) state, id, ci_id, type, value_text, value_binary, value_control, changeset_id from
                        attribute where timestamp <= @time_threshold and ({CIIDSelection2WhereClause(selection)}) and name = @name and layer_id = @layer_id 
                        and partition_index >= @partition_index
                        order by ci_id, timestamp DESC NULLS LAST
                ", trans.DBConnection, trans.DBTransaction);

                command.Parameters.AddWithValue("time_threshold", atTime.Time);
                command.Parameters.AddWithValue("name", name);
                command.Parameters.AddWithValue("layer_id", layerID);
                command.Parameters.AddWithValue("partition_index", partitionIndex);
                AddQueryParametersFromCIIDSelection(selection, command.Parameters);
            }

            command.Prepare();

            using var dr = await command.ExecuteReaderAsync();

            var ret = new List<CIAttribute>();
            while (await dr.ReadAsync())
            {
                var state = dr.GetFieldValue<AttributeState>(0);
                if (state != AttributeState.Removed)
                {
                    var id = dr.GetGuid(1);
                    var CIID = dr.GetGuid(2);
                    var type = dr.GetFieldValue<AttributeValueType>(3);
                    var valueText = dr.GetString(4);
                    var valueBinary = dr.GetFieldValue<byte[]>(5);
                    var valueControl = dr.GetFieldValue<byte[]>(6);
                    var av = AttributeValueBuilder.Unmarshal(valueText, valueBinary, valueControl, type, false);
                    var changesetID = dr.GetGuid(7);

                    ret.Add(new CIAttribute(id, name, CIID, av, state, changesetID));
                }
            }

            command.Dispose();

            return ret;
        }

        public async Task<IDictionary<Guid, string>> GetCINames(ICIIDSelection selection, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            selection = await OptimizeCIIDSelection(selection, trans);

            // NOTE: re-using FindAttributesByFullName() because the custom implementation is not very different
            var attributes = await FindAttributesByFullName(ICIModel.NameAttribute, selection, layerID, trans, atTime);
            return attributes.ToDictionary(a => a.CIID, a => a.Value.Value2String());
        }

        public async Task<IEnumerable<Guid>> FindCIIDsWithAttributeNameAndValue(string name, IAttributeValue value, ICIIDSelection selection, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            selection = await OptimizeCIIDSelection(selection, trans);

            var (valueText, valueBinary, valueControl) = AttributeValueBuilder.Marshal(value);

            NpgsqlCommand command;
            if (atTime.IsLatest && _USE_LATEST_TABLE)
            {
                command = new NpgsqlCommand(@$"
                    select state, ci_id from
                        attribute_latest where name = @name and layer_id = @layer_id and ({CIIDSelection2WhereClause(selection)})
                        and type = @type and value_text = @value_text and value_binary = @value_binary and value_control = @value_control
                ", trans.DBConnection, trans.DBTransaction);

                command.Parameters.AddWithValue("name", name);
                command.Parameters.AddWithValue("layer_id", layerID);
                command.Parameters.AddWithValue("type", value.Type);
                command.Parameters.AddWithValue("value_text", valueText);
                command.Parameters.AddWithValue("value_binary", valueBinary);
                command.Parameters.AddWithValue("value_control", valueControl);
                AddQueryParametersFromCIIDSelection(selection, command.Parameters);
            }
            else
            {
                var partitionIndex = await partitionModel.GetLatestPartitionIndex(atTime, trans);

                // TODO: check if query is well optimized regarding index use
                command = new NpgsqlCommand(@$"
                    select distinct on (ci_id) state, ci_id from
                        attribute where timestamp <= @time_threshold and ({CIIDSelection2WhereClause(selection)}) and name = @name and layer_id = @layer_id 
                        and type = @type and value_text = @value_text and value_binary = @value_binary and value_control = @value_control
                        and partition_index >= @partition_index
                        order by ci_id, timestamp DESC NULLS LAST
                ", trans.DBConnection, trans.DBTransaction);

                command.Parameters.AddWithValue("time_threshold", atTime.Time);
                command.Parameters.AddWithValue("name", name);
                command.Parameters.AddWithValue("layer_id", layerID);
                command.Parameters.AddWithValue("type", value.Type);
                command.Parameters.AddWithValue("value_text", valueText);
                command.Parameters.AddWithValue("value_binary", valueBinary);
                command.Parameters.AddWithValue("value_control", valueControl);
                command.Parameters.AddWithValue("partition_index", partitionIndex);
                AddQueryParametersFromCIIDSelection(selection, command.Parameters);
            }

            command.Prepare();

            using var dr = await command.ExecuteReaderAsync();

            command.Dispose();

            var ret = new HashSet<Guid>();
            while (await dr.ReadAsync())
            {
                var state = dr.GetFieldValue<AttributeState>(0);
                if (state != AttributeState.Removed)
                {
                    var CIID = dr.GetGuid(1);
                    ret.Add(CIID);
                }
            }

            return ret;
        }

        // TODO: actually needed? check and remove if not
        public async Task<IEnumerable<Guid>> FindCIIDsWithAttribute(string name, ICIIDSelection selection, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            selection = await OptimizeCIIDSelection(selection, trans);

            NpgsqlCommand command;
            if (atTime.IsLatest && _USE_LATEST_TABLE)
            {
                command = new NpgsqlCommand(@$"
                    select state, ci_id from
                        attribute_latest name = @name and layer_id = @layer_id and ({CIIDSelection2WhereClause(selection)})
                ", trans.DBConnection, trans.DBTransaction);

                command.Parameters.AddWithValue("name", name);
                command.Parameters.AddWithValue("layer_id", layerID);
                AddQueryParametersFromCIIDSelection(selection, command.Parameters);
            }
            else
            {
                var partitionIndex = await partitionModel.GetLatestPartitionIndex(atTime, trans);

                command = new NpgsqlCommand(@$"
                    select distinct on (ci_id) state, ci_id from
                        attribute where timestamp <= @time_threshold and ({CIIDSelection2WhereClause(selection)}) and name = @name and layer_id = @layer_id 
                        and partition_index >= @partition_index
                        order by ci_id, timestamp DESC NULLS LAST
                ", trans.DBConnection, trans.DBTransaction);

                command.Parameters.AddWithValue("time_threshold", atTime.Time);
                command.Parameters.AddWithValue("name", name);
                command.Parameters.AddWithValue("layer_id", layerID);
                command.Parameters.AddWithValue("partition_index", partitionIndex);
                AddQueryParametersFromCIIDSelection(selection, command.Parameters);
            }

            command.Prepare();

            using var dr = await command.ExecuteReaderAsync();

            command.Dispose();

            var ret = new List<Guid>();
            while (await dr.ReadAsync())
            {
                var state = dr.GetFieldValue<AttributeState>(0);
                if (state != AttributeState.Removed)
                {
                    var CIID = dr.GetGuid(1);
                    ret.Add(CIID);
                }
            }

            return ret;
        }

    }
}
