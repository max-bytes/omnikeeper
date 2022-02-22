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


        private async Task<Relation?> GetRelation(Guid fromCIID, Guid toCIID, string predicateID, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            var partitionIndex = await partitionModel.GetLatestPartitionIndex(atTime, trans);

            NpgsqlCommand command;
            if (atTime.IsLatest && _USE_LATEST_TABLE)
            {
                command = new NpgsqlCommand(@"select id, changeset_id, mask from relation_latest
                where from_ci_id = @from_ci_id AND to_ci_id = @to_ci_id and layer_id = @layer_id and predicate_id = @predicate_id
                LIMIT 1", trans.DBConnection, trans.DBTransaction);
                command.Parameters.AddWithValue("from_ci_id", fromCIID);
                command.Parameters.AddWithValue("to_ci_id", toCIID);
                command.Parameters.AddWithValue("predicate_id", predicateID);
                command.Parameters.AddWithValue("layer_id", layerID);
                command.Prepare();
            }
            else
            {
                command = new NpgsqlCommand(@"select id, changeset_id, mask from (select id, removed, changeset_id, mask from relation where 
                timestamp <= @time_threshold AND from_ci_id = @from_ci_id AND to_ci_id = @to_ci_id and layer_id = @layer_id and predicate_id = @predicate_id 
                and partition_index >= @partition_index
                order by timestamp DESC NULLS LAST
                LIMIT 1) i where i.removed = false", trans.DBConnection, trans.DBTransaction);
                command.Parameters.AddWithValue("from_ci_id", fromCIID);
                command.Parameters.AddWithValue("to_ci_id", toCIID);
                command.Parameters.AddWithValue("predicate_id", predicateID);
                command.Parameters.AddWithValue("layer_id", layerID);
                command.Parameters.AddWithValue("time_threshold", atTime.Time);
                command.Parameters.AddWithValue("partition_index", partitionIndex);
                command.Prepare();
            }
            using var dr = await command.ExecuteReaderAsync();
            if (!await dr.ReadAsync())
                return null;

            command.Dispose();

            var id = dr.GetGuid(0);
            var changesetID = dr.GetGuid(1);
            var mask = dr.GetBoolean(2);

            return new Relation(id, fromCIID, toCIID, predicateID, changesetID, mask);
        }

        public async Task<IEnumerable<Relation>[]> GetRelations(IRelationSelection rs, string[] layerIDs, IModelContext trans, TimeThreshold atTime)
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

        public async Task<(Relation relation, bool changed)> InsertRelation(Guid fromCIID, Guid toCIID, string predicateID, bool mask, string layerID, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        {
            if (fromCIID == toCIID)
                throw new Exception("From and To CIID must not be the same!");
            if (predicateID.IsEmpty())
                throw new Exception("PredicateID must not be empty");
            IDValidations.ValidatePredicateIDThrow(predicateID);

            var currentRelation = await GetRelation(fromCIID, toCIID, predicateID, layerID, trans, changesetProxy.TimeThreshold);

            if (currentRelation != null && currentRelation.Mask == mask)
            {
                // same relation already exists and is present
                return (currentRelation, false);
            }

            var id = Guid.NewGuid();
            var (_, changesetID) = await BulkUpdate(
                new (Guid, Guid, string, Guid?, Guid, bool)[] { (fromCIID, toCIID, predicateID, currentRelation?.ID, id, mask) },
                new (Guid, Guid, string, Guid, Guid, bool)[0],
                layerID, origin, changesetProxy, trans);

            return (new Relation(id, fromCIID, toCIID, predicateID, changesetID, mask), true);
        }

        public async Task<(Relation relation, bool changed)> RemoveRelation(Guid fromCIID, Guid toCIID, string predicateID, string layerID, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        {
            var currentRelation = await GetRelation(fromCIID, toCIID, predicateID, layerID, trans, changesetProxy.TimeThreshold);

            if (currentRelation == null)
            {
                // relation does not exist
                throw new Exception("Trying to remove relation that does not exist");
            }

            var id = Guid.NewGuid();

            var (_, changesetID) = await BulkUpdate(
                new (Guid, Guid, string, Guid?, Guid, bool)[0],
                new (Guid, Guid, string, Guid, Guid, bool)[] { (fromCIID, toCIID, predicateID, currentRelation.ID, id, currentRelation.Mask) },
                layerID, origin, changesetProxy, trans);

            return (new Relation(id, fromCIID, toCIID, predicateID, changesetID, currentRelation.Mask), true);
        }


        // NOTE: this bulk operation DOES check if the relations that are inserted are "unique":
        // it is not possible to insert the "same" relation (same from_ciid, to_ciid, predicate_id and layer) multiple times
        // if this operation detects a duplicate, an exception is thrown;
        // the caller is responsible for making sure there are no duplicates
        public async Task<(
            IList<(Guid fromCIID, Guid toCIID, string predicateID, Guid? existingRelationID, Guid newRelationID, bool mask)> inserts,
            IDictionary<string, Relation> outdatedRelations
            )> PrepareForBulkUpdate<F>(IBulkRelationData<F> data, IModelContext trans, TimeThreshold readTS)
        {

            var outdatedRelations = (data switch
            {
                BulkRelationDataPredicateScope p => (await GetRelations(RelationSelectionWithPredicate.Build(p.PredicateID), new string[] { data.LayerID }, trans, readTS)),
                BulkRelationDataLayerScope l => (await GetRelations(RelationSelectionAll.Instance, new string[] { data.LayerID }, trans, readTS)),
                BulkRelationDataCIAndPredicateScope cp => await cp.GetOutdatedRelationsFromCIAndPredicateScope(this, new string[] { cp.LayerID }, trans, readTS),
                _ => throw new Exception("Unknown scope")
            }).SelectMany(r => r).ToDictionary(r => r.InformationHash);

            var actualInserts = new List<(Guid fromCIID, Guid toCIID, string predicateID, Guid? existingRelationID, Guid newRelationID, bool mask)>();
            var informationHashesToInsert = new HashSet<string>();
            foreach (var fragment in data.Fragments)
            {
                var fromCIID = data.GetFromCIID(fragment);
                var toCIID = data.GetToCIID(fragment);
                if (fromCIID == toCIID)
                    throw new Exception("From and To CIID must not be the same!");

                var predicateID = data.GetPredicateID(fragment);
                if (predicateID.IsEmpty())
                    throw new Exception("PredicateID must not be empty");
                IDValidations.ValidatePredicateIDThrow(predicateID);

                var mask = data.GetMask(fragment);

                var informationHash = Relation.CreateInformationHash(fromCIID, toCIID, predicateID);
                if (informationHashesToInsert.Contains(informationHash))
                {
                    throw new Exception($"Duplicate relation fragment detected! Bulk insertion does not support duplicate relations; relation predicate ID: {predicateID}, from CIID: {fromCIID}, to CIID: {toCIID}");
                }
                informationHashesToInsert.Add(informationHash);

                // remove the current relation from the list of relations to remove
                outdatedRelations.Remove(informationHash, out var currentRelation);

                // compare masks, if mask (and everything else) is equal, skip this relation
                if (currentRelation != null && currentRelation.Mask == mask)
                {
                    continue;
                }

                Guid newRelationID = Guid.NewGuid();
                actualInserts.Add((fromCIID, toCIID, predicateID, currentRelation?.ID, newRelationID, mask));
            }

            return (actualInserts, outdatedRelations);
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
