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

        private string AttributeSelection2WhereClause(IAttributeSelection selection, ref int parameterIndex)
        {
            return selection switch
            {
                AllAttributeSelection _ => "1=1",
                NoAttributesSelection _ => "1=0",
                NamedAttributesSelection _ => $"name = ANY({SetParameter(ref parameterIndex)})",
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
                case NamedAttributesSelection n:
                    yield return new NpgsqlParameter(null, n.AttributeNames.ToArray());
                    break;
                default:
                    throw new NotImplementedException("");
            };
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

        private string SetParameter(ref int cur)
        {
            return $"${++cur}";
        }

        private async IAsyncEnumerable<(CIAttribute attribute, string layerID)> _GetAttributes(ICIIDSelection selection, string[] layerIDs, IModelContext trans, TimeThreshold atTime, IAttributeSelection attributeSelection, bool fullBinary)
        {
            NpgsqlCommand command;

            var ciidSelection2CTEClause = CIIDSelection2CTEClause(selection);


            int parameterIndex = 0;
            if (atTime.IsLatest && _USE_LATEST_TABLE)
            {
                command = new NpgsqlCommand($@"
                    {ciidSelection2CTEClause}
                    select id, name, a.ci_id, type, value_text, value_binary, value_json, value_integer, value_double, value_boolean, value_timestamptz, value_control, changeset_id, layer_id FROM attribute_latest a
                    {CIIDSelection2JoinClause(selection)}
                    where ({CIIDSelection2WhereClause(selection)}) and layer_id = ANY({SetParameter(ref parameterIndex)})
                    and ({AttributeSelection2WhereClause(attributeSelection, ref parameterIndex)})", trans.DBConnection, trans.DBTransaction);
                command.Parameters.Add(new NpgsqlParameter { Value = layerIDs });
                foreach (var p in AttributeSelection2Parameters(attributeSelection))
                    command.Parameters.Add(p);
            }
            else
            {
                var partitionIndex = await partitionModel.GetLatestPartitionIndex(atTime, trans);

                command = new NpgsqlCommand($@"
                    {ciidSelection2CTEClause}
                    select id, name, ci_id, type, value_text, value_binary, value_json, value_integer, value_double, value_boolean, value_timestamptz, value_control, changeset_id, layer_id from (
                        select distinct on(a.ci_id, name, layer_id) removed, id, name, a.ci_id, type, value_text, value_binary, value_json, value_integer, value_double, value_boolean, value_timestamptz, value_control, changeset_id, layer_id FROM attribute a
                        {CIIDSelection2JoinClause(selection)}
                        where ({CIIDSelection2WhereClause(selection)}) and timestamp <= {SetParameter(ref parameterIndex)} and layer_id = ANY({SetParameter(ref parameterIndex)}) and partition_index >= {SetParameter(ref parameterIndex)}
                        and ({AttributeSelection2WhereClause(attributeSelection, ref parameterIndex)})
                        order by a.ci_id, name, layer_id, timestamp DESC NULLS LAST
                    ) i where removed = false
                    ", trans.DBConnection, trans.DBTransaction);
                command.Parameters.AddWithValue(atTime.Time.ToUniversalTime());
                command.Parameters.AddWithValue(layerIDs);
                command.Parameters.AddWithValue(partitionIndex);
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
                var valueControl = dr.GetFieldValue<byte[]>(11);
                var av = AttributeValueHelper.Unmarshal(valueText, valueBinary, valueControl, type, fullBinary);
                var changesetID = dr.GetGuid(12);
                var layerID = dr.GetString(13);

                var att = new CIAttribute(id, name, CIID, av, changesetID);
                yield return (att, layerID);
            }
        }


        public async Task<CIAttribute?> GetFullBinaryAttribute(string name, Guid ciid, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            var a = await _GetAttributes(SpecificCIIDsSelection.Build(ciid), new string[] { layerID }, trans, atTime, NamedAttributesSelection.Build(name), true).FirstOrDefaultAsync();
            if (a == default) return null;
            return a.attribute;
        }

        // NOTE: returns a full array (one item for each layer), even when layer contains no attributes
        // NOTE: returns only entries for CIs and attributes where there actually are any attributes, so it can contain less items than the CI selection specifies
        public async Task<IDictionary<Guid, IDictionary<string, CIAttribute>>[]> GetAttributes(ICIIDSelection selection, IAttributeSelection attributeSelection, string[] layerIDs, IModelContext trans, TimeThreshold atTime, IGeneratedDataHandling generatedDataHandling)
        {
            selection = await OptimizeCIIDSelection(selection, trans);

            var tmp = new Dictionary<string, IDictionary<Guid, IDictionary<string, CIAttribute>>>(layerIDs.Length);
            foreach (var layerID in layerIDs)
                tmp[layerID] = new Dictionary<Guid, IDictionary<string, CIAttribute>>();
            await foreach (var (att, layerID) in _GetAttributes(selection, layerIDs, trans, atTime, attributeSelection, false))
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

        public async Task<IReadOnlySet<Guid>> GetCIIDsWithAttributes(ICIIDSelection selection, string[] layerIDs, IModelContext trans, TimeThreshold atTime)
        {
            NpgsqlCommand command;
            int parameterIndex = 0;
            if (atTime.IsLatest && _USE_LATEST_TABLE)
            {
                command = new NpgsqlCommand($@"
                    {CIIDSelection2CTEClause(selection)}
                    select distinct a.ci_id FROM attribute_latest a
                    {CIIDSelection2JoinClause(selection)}
                    where ({CIIDSelection2WhereClause(selection)}) and layer_id = ANY({SetParameter(ref parameterIndex)})
                    ", trans.DBConnection, trans.DBTransaction);
                command.Parameters.AddWithValue(layerIDs);
            }
            else
            {
                var partitionIndex = await partitionModel.GetLatestPartitionIndex(atTime, trans);

                command = new NpgsqlCommand($@"
                    {CIIDSelection2CTEClause(selection)}
                    select distinct i.ci_id from (
                        select distinct on(a.ci_id, name, layer_id) a.ci_id as ci_id, removed FROM attribute a
                        {CIIDSelection2JoinClause(selection)}
                        where ({CIIDSelection2WhereClause(selection)}) and timestamp <= {SetParameter(ref parameterIndex)} and layer_id = ANY({SetParameter(ref parameterIndex)}) and partition_index >= {SetParameter(ref parameterIndex)}
                        order by a.ci_id, name, layer_id, timestamp DESC NULLS LAST
                    ) i WHERE i.removed = false
                    ", trans.DBConnection, trans.DBTransaction);
                command.Parameters.AddWithValue(atTime.Time.ToUniversalTime());
                command.Parameters.AddWithValue(layerIDs);
                command.Parameters.AddWithValue(partitionIndex);
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

        // TODO: test
        public async Task<IReadOnlyList<CIAttribute>> GetAttributesOfChangeset(Guid changesetID, bool getRemoved, IModelContext trans)
        {
            int parameterIndex = 0;
            var ret = new List<CIAttribute>();
            using var command = new NpgsqlCommand($@"
            select id, name, ci_id, type, value_text, value_binary, value_control FROM attribute 
            where changeset_id = {SetParameter(ref parameterIndex)} AND removed = {SetParameter(ref parameterIndex)}
            ", trans.DBConnection, trans.DBTransaction);
            command.Parameters.AddWithValue(changesetID);
            command.Parameters.AddWithValue(getRemoved);

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
