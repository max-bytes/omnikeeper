﻿using Npgsql;
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
        private async Task<NpgsqlCommand> CreateRelationCommand(IRelationSelection rl, string layerID, IModelContext trans, TimeThreshold atTime)
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
                var query = $@"
                select id, from_ci_id, to_ci_id, predicate_id, changeset_id from relation_latest
                    where layer_id = @layer_id and ({innerWhereClause})
                ";
                command = new NpgsqlCommand(query, trans.DBConnection, trans.DBTransaction);
                foreach (var p in parameters)
                    command.Parameters.Add(p);
                command.Parameters.AddWithValue("layer_id", layerID);
                command.Prepare();
            }
            else
            {
                var query = $@"select id, from_ci_id, to_ci_id, predicate_id, changeset_id from (
                select distinct on (from_ci_id, to_ci_id, predicate_id) id, from_ci_id, to_ci_id, predicate_id, removed, changeset_id from relation 
                    where timestamp <= @time_threshold and ({innerWhereClause}) and layer_id = @layer_id 
                    and partition_index >= @partition_index
                    order by from_ci_id, to_ci_id, predicate_id, layer_id, timestamp DESC NULLS LAST
                ) i where i.removed = false
                "; // TODO: remove order by layer_id, but consider not breaking indices first
                var partitionIndex = await partitionModel.GetLatestPartitionIndex(atTime, trans);
                command = new NpgsqlCommand(query, trans.DBConnection, trans.DBTransaction);
                foreach (var p in parameters)
                    command.Parameters.Add(p);
                command.Parameters.AddWithValue("time_threshold", atTime.Time);
                command.Parameters.AddWithValue("layer_id", layerID);
                command.Parameters.AddWithValue("partition_index", partitionIndex);
                command.Prepare();
            }
            return command;
        }


        public async Task<Relation?> GetRelation(Guid fromCIID, Guid toCIID, string predicateID, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            var partitionIndex = await partitionModel.GetLatestPartitionIndex(atTime, trans);

            // TODO: use latest table
            using var command = new NpgsqlCommand(@"select id, changeset_id from (select id, removed, changeset_id from relation where 
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
            using var dr = await command.ExecuteReaderAsync();
            if (!await dr.ReadAsync())
                return null;

            var id = dr.GetGuid(0);
            var changesetID = dr.GetGuid(1);

            return new Relation(id, fromCIID, toCIID, predicateID, changesetID);
        }

        public async Task<IEnumerable<Relation>> GetRelations(IRelationSelection rs, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            var relations = new List<Relation>();
            using (var command = await CreateRelationCommand(rs, layerID, trans, atTime))
            {
                using var dr = await command.ExecuteReaderAsync();

                while (await dr.ReadAsync())
                {
                    var id = dr.GetGuid(0);
                    var fromCIID = dr.GetGuid(1);
                    var toCIID = dr.GetGuid(2);
                    var predicateID = dr.GetString(3);
                    var changesetID = dr.GetGuid(4);

                    var relation = new Relation(id, fromCIID, toCIID, predicateID, changesetID);

                    relations.Add(relation);
                }
            }

            return relations;
        }



        public async Task<IEnumerable<Relation>> GetRelationsOfChangeset(Guid changesetID, bool getRemoved, IModelContext trans)
        {
            var ret = new List<Relation>();
            using var command = new NpgsqlCommand($@"
            select id, from_ci_id, to_ci_id, predicate_id FROM relation 
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

                var relation = new Relation(id, fromCIID, toCIID, predicateID, changesetID);
                ret.Add(relation);
            }
            return ret;
        }

        public async Task<(Relation relation, bool changed)> InsertRelation(Guid fromCIID, Guid toCIID, string predicateID, string layerID, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        {
            if (fromCIID == toCIID)
                throw new Exception("From and To CIID must not be the same!");
            if (predicateID.IsEmpty())
                throw new Exception("PredicateID must not be empty");
            IDValidations.ValidatePredicateIDThrow(predicateID);

            var currentRelation = await GetRelation(fromCIID, toCIID, predicateID, layerID, trans, changesetProxy.TimeThreshold);

            if (currentRelation != null)
            {
                // same predicate already exists and is present // TODO: think about different user inserting
                return (currentRelation, false);
            }

            var partitionIndex = await partitionModel.GetLatestPartitionIndex(changesetProxy.TimeThreshold, trans);
            var changeset = await changesetProxy.GetChangeset(layerID, origin, trans);
            var id = Guid.NewGuid();

            using var commandHistoric = new NpgsqlCommand(@"INSERT INTO relation (id, from_ci_id, to_ci_id, predicate_id, layer_id, removed, changeset_id, timestamp, partition_index) 
                VALUES (@id, @from_ci_id, @to_ci_id, @predicate_id, @layer_id, @removed, @changeset_id, @timestamp, @partition_index)", trans.DBConnection, trans.DBTransaction);
            commandHistoric.Parameters.AddWithValue("id", id);
            commandHistoric.Parameters.AddWithValue("from_ci_id", fromCIID);
            commandHistoric.Parameters.AddWithValue("to_ci_id", toCIID);
            commandHistoric.Parameters.AddWithValue("predicate_id", predicateID);
            commandHistoric.Parameters.AddWithValue("layer_id", layerID);
            commandHistoric.Parameters.AddWithValue("removed", false);
            commandHistoric.Parameters.AddWithValue("changeset_id", changeset.ID);
            commandHistoric.Parameters.AddWithValue("timestamp", changeset.Timestamp);
            commandHistoric.Parameters.AddWithValue("partition_index", partitionIndex);
            await commandHistoric.ExecuteNonQueryAsync();

            using var commandLatest = new NpgsqlCommand(@"INSERT INTO relation_latest (id, from_ci_id, to_ci_id, predicate_id, layer_id, changeset_id, timestamp) 
                VALUES (@id, @from_ci_id, @to_ci_id, @predicate_id, @layer_id, @changeset_id, @timestamp)", trans.DBConnection, trans.DBTransaction);
            commandLatest.Parameters.AddWithValue("id", id);
            commandLatest.Parameters.AddWithValue("from_ci_id", fromCIID);
            commandLatest.Parameters.AddWithValue("to_ci_id", toCIID);
            commandLatest.Parameters.AddWithValue("predicate_id", predicateID);
            commandLatest.Parameters.AddWithValue("layer_id", layerID);
            commandLatest.Parameters.AddWithValue("changeset_id", changeset.ID);
            commandLatest.Parameters.AddWithValue("timestamp", changeset.Timestamp);
            await commandLatest.ExecuteNonQueryAsync();

            return (new Relation(id, fromCIID, toCIID, predicateID, changeset.ID), true);
        }

        public async Task<(Relation relation, bool changed)> RemoveRelation(Guid fromCIID, Guid toCIID, string predicateID, string layerID, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        {
            var currentRelation = await GetRelation(fromCIID, toCIID, predicateID, layerID, trans, changesetProxy.TimeThreshold);

            if (currentRelation == null)
            {
                // relation does not exist
                throw new Exception("Trying to remove relation that does not exist");
            }

            var changeset = await changesetProxy.GetChangeset(layerID, origin, trans);
            var partitionIndex = await partitionModel.GetLatestPartitionIndex(changesetProxy.TimeThreshold, trans);

            var id = Guid.NewGuid();

            using var commandHistoric = new NpgsqlCommand(@"INSERT INTO relation (id, from_ci_id, to_ci_id, predicate_id, layer_id, removed, changeset_id, timestamp, partition_index) 
                VALUES (@id, @from_ci_id, @to_ci_id, @predicate_id, @layer_id, @removed, @changeset_id, @timestamp, @partition_index)", trans.DBConnection, trans.DBTransaction);
            commandHistoric.Parameters.AddWithValue("id", id);
            commandHistoric.Parameters.AddWithValue("from_ci_id", fromCIID);
            commandHistoric.Parameters.AddWithValue("to_ci_id", toCIID);
            commandHistoric.Parameters.AddWithValue("predicate_id", predicateID);
            commandHistoric.Parameters.AddWithValue("layer_id", layerID);
            commandHistoric.Parameters.AddWithValue("removed", true);
            commandHistoric.Parameters.AddWithValue("changeset_id", changeset.ID);
            commandHistoric.Parameters.AddWithValue("timestamp", changeset.Timestamp);
            commandHistoric.Parameters.AddWithValue("partition_index", partitionIndex);
            await commandHistoric.ExecuteNonQueryAsync();

            using var commandLatest = new NpgsqlCommand(@"
                DELETE FROM relation_latest WHERE id = @id", trans.DBConnection, trans.DBTransaction);
            commandLatest.Parameters.AddWithValue("id", currentRelation.ID);
            await commandLatest.ExecuteNonQueryAsync();

            return (new Relation(id, fromCIID, toCIID, predicateID, changeset.ID), true);
        }


        // NOTE: this bulk operation DOES check if the relations that are inserted are "unique":
        // it is not possible to insert the "same" relation (same from_ciid, to_ciid, predicate_id and layer) multiple times
        // if this operation detects a duplicate, an exception is thrown;
        // the caller is responsible for making sure there are no duplicates
        public async Task<IEnumerable<(Guid fromCIID, Guid toCIID, string predicateID)>> BulkReplaceRelations<F>(IBulkRelationData<F> data, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        {
            var outdatedRelations = (data switch
            {
                BulkRelationDataPredicateScope p => (await GetRelations(RelationSelectionWithPredicate.Build(p.PredicateID), data.LayerID, trans, changesetProxy.TimeThreshold)),
                BulkRelationDataLayerScope l => (await GetRelations(RelationSelectionAll.Instance, data.LayerID, trans, changesetProxy.TimeThreshold)),
                _ => null
            }).ToDictionary(r => r.InformationHash, relation => (relation, Guid.NewGuid()));

            var actualInserts = new List<(Guid fromCIID, Guid toCIID, string predicateID, Guid newRelationID, Guid? existingRelationID)>();
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

                var informationHash = Relation.CreateInformationHash(fromCIID, toCIID, predicateID);
                // remove the current relation from the list of relations to remove
                outdatedRelations.Remove(informationHash, out var currentRelation);

                if (currentRelation.relation != null)
                {
                    continue;
                }

                Guid relationID = Guid.NewGuid();
                actualInserts.Add((fromCIID, toCIID, predicateID, relationID, currentRelation.relation?.ID));
            }

            // changeset is only created and copy mode is only entered when there is actually anything inserted
            if (!actualInserts.IsEmpty() || !outdatedRelations.IsEmpty())
            {
                Changeset changeset = await changesetProxy.GetChangeset(data.LayerID, origin, trans);

                var partitionIndex = await partitionModel.GetLatestPartitionIndex(changesetProxy.TimeThreshold, trans);

                // historic
                using var writerHistoric = trans.DBConnection.BeginBinaryImport(@"COPY relation (id, from_ci_id, to_ci_id, predicate_id, changeset_id, layer_id, removed, ""timestamp"", partition_index) FROM STDIN (FORMAT BINARY)");
                foreach (var (fromCIID, toCIID, predicateID, newRelationID, _) in actualInserts)
                {
                    writerHistoric.StartRow();
                    writerHistoric.Write(newRelationID);
                    writerHistoric.Write(fromCIID);
                    writerHistoric.Write(toCIID);
                    writerHistoric.Write(predicateID);
                    writerHistoric.Write(changeset.ID);
                    writerHistoric.Write(data.LayerID);
                    writerHistoric.Write(false);
                    writerHistoric.Write(changeset.Timestamp, NpgsqlDbType.TimestampTz);
                    writerHistoric.Write(partitionIndex, NpgsqlDbType.TimestampTz);
                }

                // remove outdated 
                foreach (var (outdatedRelation, newRelationID) in outdatedRelations.Values)
                {
                    writerHistoric.StartRow();
                    writerHistoric.Write(newRelationID);
                    writerHistoric.Write(outdatedRelation.FromCIID);
                    writerHistoric.Write(outdatedRelation.ToCIID);
                    writerHistoric.Write(outdatedRelation.PredicateID);
                    writerHistoric.Write(changeset.ID);
                    writerHistoric.Write(data.LayerID);
                    writerHistoric.Write(true);
                    writerHistoric.Write(changeset.Timestamp, NpgsqlDbType.TimestampTz);
                    writerHistoric.Write(partitionIndex, NpgsqlDbType.TimestampTz);
                }
                writerHistoric.Complete();
                writerHistoric.Close();

                // latest
                // new inserts
                if (!actualInserts.IsEmpty())
                {
                    using var writerLatest = trans.DBConnection.BeginBinaryImport(@"COPY relation_latest (id, from_ci_id, to_ci_id, predicate_id, changeset_id, layer_id, ""timestamp"") FROM STDIN (FORMAT BINARY)");
                    foreach (var (fromCIID, toCIID, predicateID, newRelationID, _) in actualInserts)
                    {
                        writerLatest.StartRow();
                        writerLatest.Write(newRelationID);
                        writerLatest.Write(fromCIID);
                        writerLatest.Write(toCIID);
                        writerLatest.Write(predicateID);
                        writerLatest.Write(changeset.ID);
                        writerLatest.Write(data.LayerID);
                        writerLatest.Write(changeset.Timestamp, NpgsqlDbType.TimestampTz);
                    }
                    writerLatest.Complete();
                    writerLatest.Close();
                }

                // removals
                // TODO: performance improvements: CTEs, indices
                foreach (var (outdatedRelation, newRelationID) in outdatedRelations.Values)
                {
                    using var commandRemoveLatest = new NpgsqlCommand(@"DELETE FROM relation_latest WHERE id = @id", trans.DBConnection, trans.DBTransaction);
                    commandRemoveLatest.Parameters.AddWithValue("id", outdatedRelation.ID);
                    await commandRemoveLatest.ExecuteNonQueryAsync();
                }
            }

            return actualInserts.Select(r => (r.fromCIID, r.toCIID, r.predicateID))
                .Concat(outdatedRelations.Values.Select(r => (r.relation.FromCIID, r.relation.ToCIID, r.relation.PredicateID)));
        }
    }
}
