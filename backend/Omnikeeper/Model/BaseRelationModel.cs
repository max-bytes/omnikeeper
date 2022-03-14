using Npgsql;
using NpgsqlTypes;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Model
{
    public class BaseRelationModel : IBaseRelationModel
    {
        private readonly IPartitionModel partitionModel;

        public static bool _USE_LATEST_TABLE = true;

        public BaseRelationModel(IPartitionModel partitionModel)
        {
            this.partitionModel = partitionModel;
        }

        private (string? whereClause, IEnumerable<NpgsqlParameter> parameters) Eval(IRelationSelection rl)
        {
            switch (rl)
            {
                case RelationSelectionFrom rsft:
                    return ("(from_ci_id = ANY(@from_ci_ids))", new[] { new NpgsqlParameter("from_ci_ids", rsft.FromCIIDs.ToArray()) });
                case RelationSelectionTo rst:
                    return ("(to_ci_id = ANY(@to_ci_ids))", new[] { new NpgsqlParameter("to_ci_ids", rst.ToCIIDs.ToArray()) });
                case RelationSelectionWithPredicate rsp:
                    return ("(predicate_id = @predicate_id)", new[] { new NpgsqlParameter("predicate_id", rsp.PredicateID) });
                case RelationSelectionSpecific rss:
                {
                    var sqlClause = "(" + string.Join(" OR ", rss.Specifics.Select((s, index) => $"(from_ci_id = @from_ci_id{index} AND to_ci_id = @to_ci_id{index} AND predicate_id = @predicate_id{index})")) + ")";
                    var parameters = rss.Specifics.SelectMany((s, index) => new[] { 
                        new NpgsqlParameter($"from_ci_id{index}", s.from),
                        new NpgsqlParameter($"to_ci_id{index}", s.to),
                        new NpgsqlParameter($"predicate_id{index}", s.predicateID)
                    });
                    return (sqlClause, parameters);
                }
                case RelationSelectionAll _:
                    return (null, new NpgsqlParameter[0]);
                case RelationSelectionNone _:
                    return ("(1=0)", new NpgsqlParameter[0]);
                //case RelationSelectionOr or:
                //    var (whereClause, parameters) = or.inners.Select(t => Eval(t)).Aggregate((tPrev, tNew) => (tPrev.whereClause + " or " + tNew.whereClause, tPrev.parameters.Concat(tNew.parameters)));
                //    return ("(" + whereClause + ")", parameters);
                default:
                    throw new Exception("Invalid relation selection");
            }
        }

        // TODO: rework to use CTEs, like attributes use -> performs much better
        private async Task<NpgsqlCommand> CreateRelationCommand(IRelationSelection rl, string[] layerIDs, IModelContext trans, TimeThreshold atTime)
        {
            var innerWhereClauses = new List<string>();
            var parameters = new List<NpgsqlParameter>();

            var (rlInnerWhereClause, rlParameters) = Eval(rl);
            if (rlInnerWhereClause != null)
                innerWhereClauses.Add(rlInnerWhereClause);
            parameters.AddRange(rlParameters);

            var innerWhereClause = string.Join(" AND ", innerWhereClauses);
            if (innerWhereClause == "") innerWhereClause = "1=1";

            NpgsqlCommand command;
            if (atTime.IsLatest && _USE_LATEST_TABLE)
            {
                var query = $@"select id, from_ci_id, to_ci_id, predicate_id, changeset_id, layer_id, mask from relation_latest
                    where layer_id = ANY(@layer_ids) and ({innerWhereClause})";
                command = new NpgsqlCommand(query, trans.DBConnection, trans.DBTransaction);
                foreach (var p in parameters)
                    command.Parameters.Add(p);
                command.Parameters.AddWithValue("layer_ids", layerIDs);
                command.Prepare();
            }
            else
            {
                var query = $@"select id, from_ci_id, to_ci_id, predicate_id, changeset_id, layer_id, mask from (
                select distinct on (from_ci_id, to_ci_id, predicate_id) id, from_ci_id, to_ci_id, predicate_id, removed, changeset_id, layer_id, mask from relation 
                    where timestamp <= @time_threshold and ({innerWhereClause}) and layer_id = ANY(@layer_ids)
                    and partition_index >= @partition_index
                    order by from_ci_id, to_ci_id, predicate_id, layer_id, timestamp DESC NULLS LAST
                ) i where i.removed = false
                "; // TODO: remove order by layer_id, but consider not breaking indices first
                var partitionIndex = await partitionModel.GetLatestPartitionIndex(atTime, trans);
                command = new NpgsqlCommand(query, trans.DBConnection, trans.DBTransaction);
                foreach (var p in parameters)
                    command.Parameters.Add(p);
                command.Parameters.AddWithValue("time_threshold", atTime.Time);
                command.Parameters.AddWithValue("layer_ids", layerIDs);
                command.Parameters.AddWithValue("partition_index", partitionIndex);
                command.Prepare();
            }
            return command;
        }

        public async Task<IEnumerable<Relation>[]> GetRelations(IRelationSelection rs, string[] layerIDs, IModelContext trans, TimeThreshold atTime, IGeneratedDataHandling generatedDataHandling)
        {
            var tmp = new Dictionary<string, List<Relation>>();
            using (var command = await CreateRelationCommand(rs, layerIDs, trans, atTime))
            {
                using var dr = await command.ExecuteReaderAsync();

                while (await dr.ReadAsync())
                {
                    var id = dr.GetGuid(0);
                    var fromCIID = dr.GetGuid(1);
                    var toCIID = dr.GetGuid(2);
                    var predicateID = dr.GetString(3);
                    var changesetID = dr.GetGuid(4);
                    var layerID = dr.GetString(5);
                    var mask = dr.GetBoolean(6);

                    var relation = new Relation(id, fromCIID, toCIID, predicateID, changesetID, mask);
                    if (tmp.TryGetValue(layerID, out var e))
                        e.Add(relation);
                    else
                        tmp.Add(layerID, new List<Relation>() { relation });
                }
            }

            var relations = new List<Relation>[layerIDs.Length];
            for (var i = 0; i < layerIDs.Length; i++)
            {
                var layerID = layerIDs[i];
                if (tmp.TryGetValue(layerID, out var t))
                    relations[i] = t;
                else
                    relations[i] = new List<Relation>();
            }

            return relations;
        }

        public async Task<IEnumerable<Relation>> GetRelationsOfChangeset(Guid changesetID, bool getRemoved, IModelContext trans)
        {
            var ret = new List<Relation>();
            using var command = new NpgsqlCommand($@"
            select id, from_ci_id, to_ci_id, predicate_id, mask FROM relation 
            where changeset_id = @changeset_id and removed = @removed
            ", trans.DBConnection, trans.DBTransaction);
            command.Parameters.AddWithValue("changeset_id", changesetID);
            command.Parameters.AddWithValue("removed", getRemoved);

            command.Prepare();

            using var dr = await command.ExecuteReaderAsync();

            while (dr.Read())
            {
                var id = dr.GetGuid(0);
                var fromCIID = dr.GetGuid(1);
                var toCIID = dr.GetGuid(2);
                var predicateID = dr.GetString(3);
                var mask = dr.GetBoolean(4);

                var relation = new Relation(id, fromCIID, toCIID, predicateID, changesetID, mask);
                ret.Add(relation);
            }
            return ret;
        }

        public async Task<(bool changed, Guid changesetID)> BulkUpdate(
            IList<(Guid fromCIID, Guid toCIID, string predicateID, Guid? existingRelationID, Guid newRelationID, bool mask)> inserts,
            IList<(Guid fromCIID, Guid toCIID, string predicateID, Guid existingRelationID, Guid newRelationID, bool mask)> removes,
            string layerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans)
        {
            if (!inserts.IsEmpty() || !removes.IsEmpty())
            {
                Changeset changeset = await changesetProxy.GetChangeset(layerID, dataOrigin, trans);
                var partitionIndex = await partitionModel.GetLatestPartitionIndex(changesetProxy.TimeThreshold, trans);

                // historic
                using var writerHistoric = trans.DBConnection.BeginBinaryImport(@"COPY relation (id, from_ci_id, to_ci_id, predicate_id, changeset_id, layer_id, removed, ""timestamp"", partition_index, mask) FROM STDIN (FORMAT BINARY)");
                foreach (var (fromCIID, toCIID, predicateID, _existingRelationID, newRelationID, mask) in inserts)
                {
                    writerHistoric.StartRow();
                    writerHistoric.Write(newRelationID);
                    writerHistoric.Write(fromCIID);
                    writerHistoric.Write(toCIID);
                    writerHistoric.Write(predicateID);
                    writerHistoric.Write(changeset.ID);
                    writerHistoric.Write(layerID);
                    writerHistoric.Write(false);
                    writerHistoric.Write(changeset.Timestamp, NpgsqlDbType.TimestampTz);
                    writerHistoric.Write(partitionIndex, NpgsqlDbType.TimestampTz);
                    writerHistoric.Write(mask);
                }

                // remove outdated 
                foreach (var (fromCIID, toCIID, predicateID, _existingRelationID, newRelationID, mask) in removes)
                {
                    writerHistoric.StartRow();
                    writerHistoric.Write(newRelationID);
                    writerHistoric.Write(fromCIID);
                    writerHistoric.Write(toCIID);
                    writerHistoric.Write(predicateID);
                    writerHistoric.Write(changeset.ID);
                    writerHistoric.Write(layerID);
                    writerHistoric.Write(true);
                    writerHistoric.Write(changeset.Timestamp, NpgsqlDbType.TimestampTz);
                    writerHistoric.Write(partitionIndex, NpgsqlDbType.TimestampTz);
                    writerHistoric.Write(mask);
                }
                writerHistoric.Complete();
                writerHistoric.Close();


                // latest
                // new inserts
                // NOTE: actual new inserts are only those that have no existing relation ID, which must be equivalent to NOT having an entry in the latest table
                // that allows us to do COPY insertion, because we guarantee that there are no unique constraint violations
                // should this ever throw a unique constraint violation, means there is a bug and _latest and _historic are out of sync
                var actualNewInserts = inserts.Where(t => t.existingRelationID == null);
                if (!actualNewInserts.IsEmpty())
                {
                    using var writerLatest = trans.DBConnection.BeginBinaryImport(@"COPY relation_latest (id, from_ci_id, to_ci_id, predicate_id, changeset_id, layer_id, mask) FROM STDIN (FORMAT BINARY)");
                    foreach (var (fromCIID, toCIID, predicateID, _existingRelationID, newRelationID, mask) in actualNewInserts)
                    {
                        writerLatest.StartRow();
                        writerLatest.Write(newRelationID);
                        writerLatest.Write(fromCIID);
                        writerLatest.Write(toCIID);
                        writerLatest.Write(predicateID);
                        writerLatest.Write(changeset.ID);
                        writerLatest.Write(layerID);
                        writerLatest.Write(mask);
                    }
                    writerLatest.Complete();
                    writerLatest.Close();
                }
                // updates (actual updates and removals)
                // TODO: improve performance
                // add index, use CTEs
                var actualModified = inserts.Where(t => t.existingRelationID != null);
                foreach (var (_fromCIID, _toCIID, _predicateID, existingRelationID, newRelationID, mask) in actualModified)
                {
                    using var commandUpdateLatest = new NpgsqlCommand(@"
                        UPDATE relation_latest SET id = @id, mask = @mask, changeset_id = @changeset_id
                        WHERE id = @old_id", trans.DBConnection, trans.DBTransaction);
                    commandUpdateLatest.Parameters.AddWithValue("id", newRelationID);
                    commandUpdateLatest.Parameters.AddWithValue("old_id", existingRelationID!);
                    commandUpdateLatest.Parameters.AddWithValue("mask", mask);
                    commandUpdateLatest.Parameters.AddWithValue("changeset_id", changeset.ID);
                    await commandUpdateLatest.ExecuteNonQueryAsync();
                }

                // removals
                // we cannot do COPY commands here, but at least use CTEs to make the deletes perform better
                if (!removes.IsEmpty())
                {
                    // TODO: use string builder for perf instead?
                    var withClause = string.Join("",
                        "WITH to_delete(from_ci_id, to_ci_id, predicate_id, id) AS (VALUES ",
                        string.Join(",", removes.Select(r => $"('{r.fromCIID}'::uuid, '{r.toCIID}'::uuid, '{r.predicateID}', '{r.newRelationID}')")),
                        " )");

                    using var commandRemoveLatest = new NpgsqlCommand(@$"
                        {withClause}
                        DELETE FROM relation_latest r
                        USING to_delete t
                        WHERE r.from_ci_id = t.from_ci_id AND r.to_ci_id = t.to_ci_id AND r.predicate_id = t.predicate_id AND r.layer_id = @layer_id", trans.DBConnection, trans.DBTransaction);
                    commandRemoveLatest.Parameters.AddWithValue("layer_id", layerID);
                    await commandRemoveLatest.ExecuteNonQueryAsync();
                }

                return (true, changeset.ID);
            }
            else
            {
                return (false, default);
            }
        }
    }
}
