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

        public BaseRelationModel(IPartitionModel partitionModel)
        {
            this.partitionModel = partitionModel;
        }

        private NpgsqlCommand CreateRelationCommand(IRelationSelection rl, string layerID, DateTimeOffset partitionIndex, IModelContext trans, TimeThreshold atTime)
        {
            var innerWhereClauses = new List<string>();
            var parameters = new List<NpgsqlParameter>();
            switch (rl)
            {
                case RelationSelectionFrom rsft:
                    innerWhereClauses.Add("(from_ci_id = @from_ci_id)");
                    parameters.Add(new NpgsqlParameter("from_ci_id", rsft.fromCIID));
                    break;
                case RelationSelectionEitherFromOrTo rsot:
                    innerWhereClauses.Add("(from_ci_id = @ci_identity OR to_ci_id = @ci_identity)");
                    parameters.Add(new NpgsqlParameter("ci_identity", rsot.ciid));
                    break;
                case RelationSelectionWithPredicate rsp:
                    innerWhereClauses.Add("(predicate_id = @predicate_id)");
                    parameters.Add(new NpgsqlParameter("predicate_id", rsp.predicateID));
                    break;
                case RelationSelectionAll _:
                    break;
                default:
                    throw new Exception("Invalid relation selection");
            }
            var innerWhereClause = string.Join(" AND ", innerWhereClauses);
            if (innerWhereClause == "") innerWhereClause = "1=1";
            var query = $@"
                select distinct on (from_ci_id, to_ci_id, predicate_id) id, from_ci_id, to_ci_id, predicate_id, state, changeset_id from relation 
                    where timestamp <= @time_threshold and ({innerWhereClause}) and layer_id = @layer_id 
                    and partition_index >= @partition_index
                    order by from_ci_id, to_ci_id, predicate_id, layer_id, timestamp DESC NULLS LAST
            "; // TODO: remove order by layer_id, but consider not breaking indices first

            var command = new NpgsqlCommand(query, trans.DBConnection, trans.DBTransaction);
            foreach (var p in parameters)
                command.Parameters.Add(p);
            command.Parameters.AddWithValue("time_threshold", atTime.Time);
            command.Parameters.AddWithValue("layer_id", layerID);
            command.Parameters.AddWithValue("partition_index", partitionIndex);
            command.Prepare();
            return command;
        }


        public async Task<Relation?> GetRelation(Guid fromCIID, Guid toCIID, string predicateID, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            var partitionIndex = await partitionModel.GetLatestPartitionIndex(atTime, trans);

            using var command = new NpgsqlCommand(@"select id, state, changeset_id from relation where 
                timestamp <= @time_threshold AND from_ci_id = @from_ci_id AND to_ci_id = @to_ci_id and layer_id = @layer_id and predicate_id = @predicate_id 
                and partition_index >= @partition_index
                order by timestamp DESC NULLS LAST
                LIMIT 1", trans.DBConnection, trans.DBTransaction);
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
            var state = dr.GetFieldValue<RelationState>(1);
            var changesetID = dr.GetGuid(2);

            return new Relation(id, fromCIID, toCIID, predicateID, state, changesetID);
        }

        public async Task<IEnumerable<Relation>> GetRelations(IRelationSelection rs, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            var partitionIndex = await partitionModel.GetLatestPartitionIndex(atTime, trans);

            var relations = new List<Relation>();
            using (var command = CreateRelationCommand(rs, layerID, partitionIndex, trans, atTime))
            {
                using var dr = await command.ExecuteReaderAsync();

                while (await dr.ReadAsync())
                {
                    var id = dr.GetGuid(0);
                    var fromCIID = dr.GetGuid(1);
                    var toCIID = dr.GetGuid(2);
                    var predicateID = dr.GetString(3);
                    var state = dr.GetFieldValue<RelationState>(4);
                    var changesetID = dr.GetGuid(5);

                    var relation = new Relation(id, fromCIID, toCIID, predicateID, state, changesetID);

                    if (state != RelationState.Removed)
                        relations.Add(relation);
                }
            }

            return relations;
        }



        public async Task<IEnumerable<Relation>> GetRelationsOfChangeset(Guid changesetID, IModelContext trans)
        {
            var ret = new List<Relation>();
            using var command = new NpgsqlCommand($@"
            select id, from_ci_id, to_ci_id, predicate_id, state FROM relation 
            where changeset_id = @changeset_id
            ", trans.DBConnection, trans.DBTransaction);
            command.Parameters.AddWithValue("changeset_id", changesetID);

            command.Prepare();

            using var dr = await command.ExecuteReaderAsync();

            while (dr.Read())
            {
                var id = dr.GetGuid(0);
                var fromCIID = dr.GetGuid(1);
                var toCIID = dr.GetGuid(2);
                var predicateID = dr.GetString(3);
                var state = dr.GetFieldValue<RelationState>(4);

                var relation = new Relation(id, fromCIID, toCIID, predicateID, state, changesetID);
                ret.Add(relation);
            }
            return ret;
        }

        public async Task<(Relation relation, bool changed)> RemoveRelation(Guid fromCIID, Guid toCIID, string predicateID, string layerID, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        {
            var currentRelation = await GetRelation(fromCIID, toCIID, predicateID, layerID, trans, changesetProxy.TimeThreshold);

            if (currentRelation == null)
            {
                // relation does not exist
                throw new Exception("Trying to remove relation that does not exist");
            }
            if (currentRelation.State == RelationState.Removed)
            {
                // the relation is already removed, no-op(?)
                return (currentRelation, false);
            }

            var partitionIndex = await partitionModel.GetLatestPartitionIndex(changesetProxy.TimeThreshold, trans);

            using var command = new NpgsqlCommand(@"INSERT INTO relation (id, from_ci_id, to_ci_id, predicate_id, layer_id, state, changeset_id, timestamp, partition_index) 
                VALUES (@id, @from_ci_id, @to_ci_id, @predicate_id, @layer_id, @state, @changeset_id, @timestamp, @partition_index)", trans.DBConnection, trans.DBTransaction);

            var changeset = await changesetProxy.GetChangeset(layerID, origin, trans);

            var id = Guid.NewGuid();
            command.Parameters.AddWithValue("id", id);
            command.Parameters.AddWithValue("from_ci_id", fromCIID);
            command.Parameters.AddWithValue("to_ci_id", toCIID);
            command.Parameters.AddWithValue("predicate_id", predicateID);
            command.Parameters.AddWithValue("layer_id", layerID);
            command.Parameters.AddWithValue("state", RelationState.Removed);
            command.Parameters.AddWithValue("changeset_id", changeset.ID);
            command.Parameters.AddWithValue("timestamp", changeset.Timestamp);
            command.Parameters.AddWithValue("partition_index", partitionIndex);


            await command.ExecuteNonQueryAsync();
            return (new Relation(id, fromCIID, toCIID, predicateID, RelationState.Removed, changeset.ID), true);
        }

        public async Task<(Relation relation, bool changed)> InsertRelation(Guid fromCIID, Guid toCIID, string predicateID, string layerID, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        {
            if (fromCIID == toCIID)
                throw new Exception("From and To CIID must not be the same!");
            if (predicateID.IsEmpty())
                throw new Exception("PredicateID must not be empty");
            if (!PredicateModel.ValidatePredicateID(predicateID))
                throw new Exception("Invalid predicateID");

            var currentRelation = await GetRelation(fromCIID, toCIID, predicateID, layerID, trans, changesetProxy.TimeThreshold);

            var state = RelationState.New;
            if (currentRelation != null)
            {
                if (currentRelation.State == RelationState.Removed)
                    state = RelationState.Renewed;
                else
                {
                    // same predicate already exists and is present // TODO: think about different user inserting
                    return (currentRelation, false);
                }
            }

            var partitionIndex = await partitionModel.GetLatestPartitionIndex(changesetProxy.TimeThreshold, trans);

            using var command = new NpgsqlCommand(@"INSERT INTO relation (id, from_ci_id, to_ci_id, predicate_id, layer_id, state, changeset_id, timestamp, partition_index) 
                VALUES (@id, @from_ci_id, @to_ci_id, @predicate_id, @layer_id, @state, @changeset_id, @timestamp, @partition_index)", trans.DBConnection, trans.DBTransaction);

            var changeset = await changesetProxy.GetChangeset(layerID, origin, trans);

            var id = Guid.NewGuid();
            command.Parameters.AddWithValue("id", id);
            command.Parameters.AddWithValue("from_ci_id", fromCIID);
            command.Parameters.AddWithValue("to_ci_id", toCIID);
            command.Parameters.AddWithValue("predicate_id", predicateID);
            command.Parameters.AddWithValue("layer_id", layerID);
            command.Parameters.AddWithValue("state", state);
            command.Parameters.AddWithValue("changeset_id", changeset.ID);
            command.Parameters.AddWithValue("timestamp", changeset.Timestamp);
            command.Parameters.AddWithValue("partition_index", partitionIndex);

            await command.ExecuteNonQueryAsync();
            return (new Relation(id, fromCIID, toCIID, predicateID, state, changeset.ID), true);
        }


        // NOTE: this bulk operation does not check if the relations that are inserted are "unique":
        // it is possible to insert the "same" relation (same from_ciid, to_ciid, predicate_id and layer) multiple times
        // the caller is responsible for making sure there are no duplicates
        public async Task<IEnumerable<(Guid fromCIID, Guid toCIID, string predicateID, RelationState state)>> BulkReplaceRelations<F>(IBulkRelationData<F> data, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        {
            var outdatedRelations = (data switch
            {
                BulkRelationDataPredicateScope p => (await GetRelations(new RelationSelectionWithPredicate(p.PredicateID), data.LayerID, trans, changesetProxy.TimeThreshold)),
                BulkRelationDataLayerScope l => (await GetRelations(new RelationSelectionAll(), data.LayerID, trans, changesetProxy.TimeThreshold)),
                _ => null
            }).ToDictionary(r => r.InformationHash);

            var actualInserts = new List<(Guid fromCIID, Guid toCIID, string predicateID, RelationState state)>();
            foreach (var fragment in data.Fragments)
            {
                var id = Guid.NewGuid();
                var fromCIID = data.GetFromCIID(fragment);
                var toCIID = data.GetToCIID(fragment);
                if (fromCIID == toCIID)
                    throw new Exception("From and To CIID must not be the same!");

                var predicateID = data.GetPredicateID(fragment);
                if (predicateID.IsEmpty())
                    throw new Exception("PredicateID must not be empty");
                if (!PredicateModel.ValidatePredicateID(predicateID))
                    throw new Exception("Invalid predicateID");

                var informationHash = Relation.CreateInformationHash(fromCIID, toCIID, predicateID);
                // remove the current relation from the list of relations to remove
                outdatedRelations.Remove(informationHash, out var currentRelation);

                var state = RelationState.New;
                if (currentRelation != null)
                {
                    if (currentRelation.State == RelationState.Removed)
                        state = RelationState.Renewed;
                    else
                        continue;
                }

                actualInserts.Add((fromCIID, toCIID, predicateID, state));
            }

            // changeset is only created and copy mode is only entered when there is actually anything inserted
            if (!actualInserts.IsEmpty() || !outdatedRelations.IsEmpty())
            {
                Changeset changeset = await changesetProxy.GetChangeset(data.LayerID, origin, trans);

                var partitionIndex = await partitionModel.GetLatestPartitionIndex(changesetProxy.TimeThreshold, trans);

                // use postgres COPY feature instead of manual inserts https://www.npgsql.org/doc/copy.html
                using var writer = trans.DBConnection.BeginBinaryImport(@"COPY relation (id, from_ci_id, to_ci_id, predicate_id, changeset_id, layer_id, state, ""timestamp"", partition_index) FROM STDIN (FORMAT BINARY)");
                foreach (var (fromCIID, toCIID, predicateID, state) in actualInserts)
                {
                    writer.StartRow();
                    writer.Write(Guid.NewGuid());
                    writer.Write(fromCIID);
                    writer.Write(toCIID);
                    writer.Write(predicateID);
                    writer.Write(changeset.ID);
                    writer.Write(data.LayerID);
                    writer.Write(state, "relationstate");
                    writer.Write(changeset.Timestamp, NpgsqlDbType.TimestampTz);
                    writer.Write(partitionIndex, NpgsqlDbType.TimestampTz);
                }

                // remove outdated 
                foreach (var outdatedRelation in outdatedRelations.Values)
                {
                    writer.StartRow();
                    writer.Write(Guid.NewGuid());
                    writer.Write(outdatedRelation.FromCIID);
                    writer.Write(outdatedRelation.ToCIID);
                    writer.Write(outdatedRelation.PredicateID);
                    writer.Write(changeset.ID);
                    writer.Write(data.LayerID);
                    writer.Write(RelationState.Removed, "relationstate");
                    writer.Write(changeset.Timestamp, NpgsqlDbType.TimestampTz);
                    writer.Write(partitionIndex, NpgsqlDbType.TimestampTz);
                }
                writer.Complete();
            }

            return actualInserts;
        }
    }
}
