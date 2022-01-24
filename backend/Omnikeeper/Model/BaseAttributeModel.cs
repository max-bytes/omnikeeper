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

        private async Task<CIAttribute?> _GetAttribute(string name, Guid ciid, string layerID, IModelContext trans, TimeThreshold atTime, bool fullBinary)
        {
            NpgsqlCommand command;
            if (atTime.IsLatest && _USE_LATEST_TABLE)
            {
                command = new NpgsqlCommand(@"
                select id, ci_id, type, value_text, value_binary, value_control, changeset_id FROM attribute_latest
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
                select id, ci_id, type, value_text, value_binary, value_control, changeset_id
                from (
                    select id, ci_id, type, value_text, value_binary, value_control, changeset_id FROM attribute 
                    where timestamp <= @time_threshold and ci_id = @ci_id and layer_id = @layer_id and name = @name and partition_index >= @partition_index
                    order by timestamp DESC NULLS LAST LIMIT 1
                ) i where removed = false
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
            var av = AttributeValueHelper.Unmarshal(valueText, valueBinary, valueControl, type, fullBinary);
            var changesetID = dr.GetGuid(6);
            var att = new CIAttribute(id, name, CIID, av, changesetID);
            return att;
        }

        private string CIIDSelection2WhereClause(ICIIDSelection selection)
        {
            return selection switch
            {
                AllCIIDsSelection _ => "1=1",
                SpecificCIIDsSelection _ => "t.ci_id is not null",
                AllCIIDsExceptSelection _ => "t.ci_id is null",
                NoCIIDsSelection _ => "1=0",
                _ => throw new NotImplementedException("")
            };
        }

        private string AttributeSelection2WhereClause(IAttributeSelection selection)
        {
            return selection switch
            {
                AllAttributeSelection _ => "1=1",
                NoAttributesSelection _ => "1=0",
                RegexAttributeSelection _ => "name ~ @name_regex",
                NamedAttributesSelection _ => "name = ANY(@names)",
                _ => throw new NotImplementedException("")
            };
        }

        private IEnumerable<NpgsqlParameter> AttributeSelection2Parameters(IAttributeSelection selection)
        {
            switch (selection)
            {
                case AllAttributeSelection _:
                    break;
                case NoAttributesSelection _:
                    break;
                case RegexAttributeSelection r:
                    yield return new NpgsqlParameter("@name_regex", r.RegexStr);
                    break;
                case NamedAttributesSelection n:
                    yield return new NpgsqlParameter("@names", n.AttributeNames.ToArray());
                    break;
                default:
                    throw new NotImplementedException("");
            };
        }

        // NOTE: doing
        // ci_id = ANY(@ci_ids)
        // and/or
        // ci_id <> ALL(@ci_ids)
        // is really slow in postgres for larger arrays of @ci_ids and the query optimizer does not properly optimize it
        // hence, we use CTEs/with clause at the start to create a temporary joinable relation
        // see here for a discussion about the options https://stackoverflow.com/questions/17037508/sql-when-it-comes-to-not-in-and-not-equal-to-which-is-more-efficient-and-why/17038097#17038097
        private string CIIDSelection2CTEClause(ICIIDSelection selection)
        {
            return selection switch
            {
                AllCIIDsSelection _ => "",
                SpecificCIIDsSelection s => string.Join("",
                    "WITH included(ci_id) AS (VALUES ",
                    string.Join(",", s.CIIDs.Select(ciid => $"('{ciid}'::uuid)")),
                    " )"),
                AllCIIDsExceptSelection e => string.Join("",
                    "WITH excluded(ci_id) AS (VALUES ",
                    string.Join(",", e.ExceptCIIDs.Select(ciid => $"('{ciid}'::uuid)")),
                    " )"),
                NoCIIDsSelection _ => "",
                _ => throw new NotImplementedException("")
            };
        }

        private string CIIDSelection2JoinClause(ICIIDSelection selection)
        {
            return selection switch
            {
                AllCIIDsSelection _ => "",
                SpecificCIIDsSelection _ => "left join included t ON t.ci_id = a.ci_id",
                AllCIIDsExceptSelection _ => "left join excluded t ON t.ci_id = a.ci_id",
                NoCIIDsSelection _ => "",
                _ => throw new NotImplementedException("")
            };
        }

        // NOTE: this exists because querying using AllCIIDsExceptSelection is very slow when the list of excluded CIIDs gets large
        // that's why we - under certain circumstances - flip the selection and turn the AllCIIDsExceptSelection into a SpecificCIIDsSelection
        // TODO: check if this is still needed after rewrite to CTE
        // or: check if we should also do it the other way round?
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

        private async IAsyncEnumerable<(CIAttribute attribute, string layerID)> _GetAttributes(ICIIDSelection selection, string[] layerIDs, IModelContext trans, TimeThreshold atTime, IAttributeSelection attributeSelection)
        {
            NpgsqlCommand command;

            var ciidSelection2CTEClause = CIIDSelection2CTEClause(selection);

            if (atTime.IsLatest && _USE_LATEST_TABLE)
            {
                command = new NpgsqlCommand($@"
                    {ciidSelection2CTEClause}
                    select id, name, a.ci_id, type, value_text, value_binary, value_control, changeset_id, layer_id FROM attribute_latest a
                    {CIIDSelection2JoinClause(selection)}
                    where ({CIIDSelection2WhereClause(selection)}) and layer_id = ANY(@layer_ids)
                    and ({AttributeSelection2WhereClause(attributeSelection)})", trans.DBConnection, trans.DBTransaction);
                command.Parameters.AddWithValue("layer_ids", layerIDs);
                foreach (var p in AttributeSelection2Parameters(attributeSelection))
                    command.Parameters.Add(p);
            }
            else
            {
                var partitionIndex = await partitionModel.GetLatestPartitionIndex(atTime, trans);

                command = new NpgsqlCommand($@"
                    {ciidSelection2CTEClause}
                    select id, name, ci_id, type, value_text, value_binary, value_control, changeset_id, layer_id from (
                        select distinct on(a.ci_id, name, layer_id) removed, id, name, a.ci_id, type, value_text, value_binary, value_control, changeset_id, layer_id FROM attribute a
                        {CIIDSelection2JoinClause(selection)}
                        where ({CIIDSelection2WhereClause(selection)}) and timestamp <= @time_threshold and layer_id = ANY(@layer_ids) and partition_index >= @partition_index
                        and ({AttributeSelection2WhereClause(attributeSelection)})
                        order by a.ci_id, name, layer_id, timestamp DESC NULLS LAST
                    ) i where removed = false
                    ", trans.DBConnection, trans.DBTransaction);
                command.Parameters.AddWithValue("layer_ids", layerIDs);
                command.Parameters.AddWithValue("time_threshold", atTime.Time);
                command.Parameters.AddWithValue("partition_index", partitionIndex);
                foreach (var p in AttributeSelection2Parameters(attributeSelection))
                    command.Parameters.Add(p);
            }

            if (ciidSelection2CTEClause == "")
                command.Prepare(); // NOTE: preparing only makes sense if the query is somewhat static, which it won't be when a highly dynamic CTE is involved

            using var dr = await command.ExecuteReaderAsync();

            command.Dispose();

            while (dr.Read())
            {
                var id = dr.GetGuid(0);
                var name = dr.GetString(1);
                var CIID = dr.GetGuid(2);
                var type = dr.GetFieldValue<AttributeValueType>(3);
                var valueText = dr.GetString(4);
                var valueBinary = dr.GetFieldValue<byte[]>(5);
                var valueControl = dr.GetFieldValue<byte[]>(6);
                var av = AttributeValueHelper.Unmarshal(valueText, valueBinary, valueControl, type, false);
                var changesetID = dr.GetGuid(7);
                var layerID = dr.GetString(8);

                var att = new CIAttribute(id, name, CIID, av, changesetID);
                yield return (att, layerID);
            }
        }


        public async Task<CIAttribute?> GetFullBinaryAttribute(string name, Guid ciid, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            return await _GetAttribute(name, ciid, layerID, trans, atTime, true);
        }

        // NOTE: returns a full array (one item for each layer), even when layer contains no attributes
        // NOTE: returns only entries for CIs and attributes where there actually are any attributes, so it can contain less items than the CI selection specifies
        public async Task<IDictionary<Guid, IDictionary<string, CIAttribute>>[]> GetAttributes(ICIIDSelection selection, IAttributeSelection attributeSelection, string[] layerIDs, IModelContext trans, TimeThreshold atTime)
        {
            selection = await OptimizeCIIDSelection(selection, trans);

            var tmp = new Dictionary<string, IDictionary<Guid, IDictionary<string, CIAttribute>>>(layerIDs.Length);
            foreach (var layerID in layerIDs)
                tmp[layerID] = new Dictionary<Guid, IDictionary<string, CIAttribute>>();
            await foreach (var (att, layerID) in _GetAttributes(selection, layerIDs, trans, atTime, attributeSelection))
            {
                var r = tmp[layerID];
                if (r.TryGetValue(att.CIID, out var l))
                    l.Add(att.Name, att);
                else
                    r.Add(att.CIID, new Dictionary<string, CIAttribute>() { { att.Name, att } });
            }

            var ret = new IDictionary<Guid, IDictionary<string, CIAttribute>>[layerIDs.Length];
            for (var i = 0; i < layerIDs.Length; i++)
            {
                ret[i] = tmp[layerIDs[i]];
            }

            return ret;
        }

        // TODO: test
        public async Task<ISet<Guid>> GetCIIDsWithAttributes(ICIIDSelection selection, string[] layerIDs, IModelContext trans, TimeThreshold atTime)
        {
            NpgsqlCommand command;
            if (atTime.IsLatest && _USE_LATEST_TABLE)
            {
                command = new NpgsqlCommand($@"
                    {CIIDSelection2CTEClause(selection)}
                    select distinct a.ci_id FROM attribute_latest a
                    {CIIDSelection2JoinClause(selection)}
                    where ({CIIDSelection2WhereClause(selection)}) and layer_id = ANY(@layer_ids)
                    ", trans.DBConnection, trans.DBTransaction);
                command.Parameters.AddWithValue("layer_ids", layerIDs);
            }
            else
            {
                var partitionIndex = await partitionModel.GetLatestPartitionIndex(atTime, trans);

                command = new NpgsqlCommand($@"
                    {CIIDSelection2CTEClause(selection)}
                    select distinct i.ci_id from (
                        select distinct on(a.ci_id, name, layer_id) a.ci_id as ci_id, removed FROM attribute a
                        {CIIDSelection2JoinClause(selection)}
                        where ({CIIDSelection2WhereClause(selection)}) and timestamp <= @time_threshold and layer_id = ANY(@layer_ids) and partition_index >= @partition_index
                        order by a.ci_id, name, layer_id, timestamp DESC NULLS LAST
                    ) i WHERE i.removed = false
                    ", trans.DBConnection, trans.DBTransaction);
                command.Parameters.AddWithValue("layer_ids", layerIDs);
                command.Parameters.AddWithValue("time_threshold", atTime.Time);
                command.Parameters.AddWithValue("partition_index", partitionIndex);
            }

            command.Prepare();

            using var dr = await command.ExecuteReaderAsync();

            command.Dispose();

            var ret = new HashSet<Guid>();
            while (dr.Read())
            {
                var ciid = dr.GetGuid(0);
                ret.Add(ciid);
            }
            return ret;
        }


        public async Task<IEnumerable<CIAttribute>> GetAttributesOfChangeset(Guid changesetID, bool getRemoved, IModelContext trans)
        {
            var ret = new List<CIAttribute>();
            using var command = new NpgsqlCommand($@"
            select id, name, ci_id, type, value_text, value_binary, value_control FROM attribute 
            where changeset_id = @changeset_id AND removed = @removed
            ", trans.DBConnection, trans.DBTransaction);
            command.Parameters.AddWithValue("changeset_id", changesetID);
            command.Parameters.AddWithValue("removed", getRemoved);

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
                var av = AttributeValueHelper.Unmarshal(valueText, valueBinary, valueControl, type, false);

                var att = new CIAttribute(id, name, CIID, av, changesetID);
                ret.Add(att);
            }
            return ret;
        }

    }
}
