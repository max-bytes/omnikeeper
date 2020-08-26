using DotLiquid.Tags;
using Hangfire.States;
using Landscape.Base.Entity;
using Landscape.Base.Inbound;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapeRegistry.Model
{
    public class BaseRelationModel : IBaseRelationModel
    {
        private readonly NpgsqlConnection conn;
        private readonly IPredicateModel predicateModel;

        public BaseRelationModel(IPredicateModel predicateModel, NpgsqlConnection connection)
        {
            conn = connection;
            this.predicateModel = predicateModel;
        }

        private NpgsqlCommand CreateRelationCommand(IRelationSelection rl, long layerID, NpgsqlTransaction trans, TimeThreshold atTime)
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
                select distinct on (from_ci_id, to_ci_id, predicate_id) id, from_ci_id, to_ci_id, predicate_id, state, changeset_id from
                    relation where timestamp <= @time_threshold and ({innerWhereClause}) and layer_id = @layer_id order by from_ci_id, to_ci_id, predicate_id, layer_id, timestamp DESC
            "; // TODO: remove order by layer_id, but consider not breaking indices first

            var command = new NpgsqlCommand(query, conn, trans);
            foreach(var p in parameters)
                command.Parameters.Add(p);
            command.Parameters.AddWithValue("time_threshold", atTime.Time);
            command.Parameters.AddWithValue("layer_id", layerID);
            return command;
        }


        public async Task<Relation> GetRelation(Guid fromCIID, Guid toCIID, string predicateID, long layerID, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            var predicates = await predicateModel.GetPredicates(trans, atTime, AnchorStateFilter.All); // TODO: don't get predicates all the time

            using (var command = new NpgsqlCommand(@"select id, state, changeset_id from relation where 
                timestamp <= @time_threshold AND from_ci_id = @from_ci_id AND to_ci_id = @to_ci_id and layer_id = @layer_id and predicate_id = @predicate_id order by timestamp DESC 
                LIMIT 1", conn, trans))
            {
                command.Parameters.AddWithValue("from_ci_id", fromCIID);
                command.Parameters.AddWithValue("to_ci_id", toCIID);
                command.Parameters.AddWithValue("predicate_id", predicateID);
                command.Parameters.AddWithValue("layer_id", layerID);
                command.Parameters.AddWithValue("time_threshold", atTime.Time);
                using var dr = await command.ExecuteReaderAsync();
                if (!await dr.ReadAsync())
                    return null;

                var id = dr.GetGuid(0);
                var state = dr.GetFieldValue<RelationState>(1);
                var changesetID = dr.GetGuid(2);

                var predicate = predicates[predicateID];

                return Relation.Build(id, fromCIID, toCIID, predicate, state, changesetID);
            }
        }

        public async Task<IEnumerable<Relation>> GetRelations(IRelationSelection rs, long layerID, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            var predicates = await predicateModel.GetPredicates(trans, atTime, AnchorStateFilter.All);

            var relations = new List<Relation>();
            using (var command = CreateRelationCommand(rs, layerID, trans, atTime))
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

                    var predicate = predicates[predicateID];
                    var relation = Relation.Build(id, fromCIID, toCIID, predicate, state, changesetID);

                    if (state != RelationState.Removed)
                        relations.Add(relation);
                }
            }

            return relations;
        }

        public async Task<Relation> RemoveRelation(Guid fromCIID, Guid toCIID, string predicateID, long layerID, IChangesetProxy changesetProxy, NpgsqlTransaction trans)
        {
            var timeThreshold = TimeThreshold.BuildLatest();
            var currentRelation = await GetRelation(fromCIID, toCIID, predicateID, layerID, trans, timeThreshold);

            if (currentRelation == null)
            {
                // relation does not exist
                throw new Exception("Trying to remove relation that does not exist");
            }
            if (currentRelation.State == RelationState.Removed)
            {
                // the relation is already removed, no-op(?)
                return currentRelation;
            }

            var predicates = await predicateModel.GetPredicates(trans, timeThreshold, AnchorStateFilter.All);

            using var command = new NpgsqlCommand(@"INSERT INTO relation (id, from_ci_id, to_ci_id, predicate_id, layer_id, state, changeset_id, timestamp) 
                VALUES (@id, @from_ci_id, @to_ci_id, @predicate_id, @layer_id, @state, @changeset_id, @timestamp)", conn, trans);

            var changeset = await changesetProxy.GetChangeset(trans);

            var id = Guid.NewGuid();
            command.Parameters.AddWithValue("id", id);
            command.Parameters.AddWithValue("from_ci_id", fromCIID);
            command.Parameters.AddWithValue("to_ci_id", toCIID);
            command.Parameters.AddWithValue("predicate_id", predicateID);
            command.Parameters.AddWithValue("layer_id", layerID);
            command.Parameters.AddWithValue("state", RelationState.Removed);
            command.Parameters.AddWithValue("changeset_id", changeset.ID);
            command.Parameters.AddWithValue("timestamp", changeset.Timestamp);

            var predicate = predicates[predicateID]; // TODO: only get one predicate?

            await command.ExecuteNonQueryAsync();
            return Relation.Build(id, fromCIID, toCIID, predicate, RelationState.Removed, changeset.ID);
        }

        public async Task<Relation> InsertRelation(Guid fromCIID, Guid toCIID, string predicateID, long layerID, IChangesetProxy changesetProxy, NpgsqlTransaction trans)
        {
            var timeThreshold = TimeThreshold.BuildLatest();
            var currentRelation = await GetRelation(fromCIID, toCIID, predicateID, layerID, trans, timeThreshold);

            if (fromCIID == toCIID)
                throw new Exception("From and To CIID must not be the same!");

            var state = RelationState.New;
            if (currentRelation != null)
            {
                if (currentRelation.State == RelationState.Removed)
                    state = RelationState.Renewed;
                else
                {
                    // same predicate already exists and is present // TODO: think about different user inserting
                    return currentRelation;
                }
            }

            var predicates = await predicateModel.GetPredicates(trans, timeThreshold, AnchorStateFilter.ActiveOnly); // only active predicates allowed

            if (!predicates.ContainsKey(predicateID))
                throw new KeyNotFoundException($"Predicate ID {predicateID} does not exist");

            using var command = new NpgsqlCommand(@"INSERT INTO relation (id, from_ci_id, to_ci_id, predicate_id, layer_id, state, changeset_id, timestamp) 
                VALUES (@id, @from_ci_id, @to_ci_id, @predicate_id, @layer_id, @state, @changeset_id, @timestamp)", conn, trans);

            var changeset = await changesetProxy.GetChangeset(trans);

            var id = Guid.NewGuid();
            command.Parameters.AddWithValue("id", id);
            command.Parameters.AddWithValue("from_ci_id", fromCIID);
            command.Parameters.AddWithValue("to_ci_id", toCIID);
            command.Parameters.AddWithValue("predicate_id", predicateID);
            command.Parameters.AddWithValue("layer_id", layerID);
            command.Parameters.AddWithValue("state", state);
            command.Parameters.AddWithValue("changeset_id", changeset.ID);
            command.Parameters.AddWithValue("timestamp", changeset.Timestamp);

            var predicate = predicates[predicateID]; // TODO: only get one predicate?

            await command.ExecuteNonQueryAsync();
            return Relation.Build(id, fromCIID, toCIID, predicate, state, changeset.ID);
        }


        public async Task<bool> BulkReplaceRelations<F>(IBulkRelationData<F> data, IChangesetProxy changesetProxy, NpgsqlTransaction trans)
        {
            var timeThreshold = TimeThreshold.BuildLatest();
            var outdatedRelations = (data switch
            {
                BulkRelationDataPredicateScope p => (await GetRelations(new RelationSelectionWithPredicate(p.PredicateID), data.LayerID, trans, timeThreshold)),
                BulkRelationDataLayerScope l => (await GetRelations(new RelationSelectionAll(), data.LayerID, trans, timeThreshold)),
                _ => null
            }).ToDictionary(r => r.InformationHash);

            var actualInserts = new List<(Guid fromCIID, Guid toCIID, string predicateID, RelationState state)>();
            foreach (var fragment in data.Fragments)
            {
                var id = Guid.NewGuid();
                var fromCIID = data.GetFromCIID(fragment);
                var toCIID = data.GetToCIID(fragment);

                var predicateID = data.GetPredicateID(fragment); // TODO: check if predicates are active
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
                Changeset changeset = await changesetProxy.GetChangeset(trans);

                // use postgres COPY feature instead of manual inserts https://www.npgsql.org/doc/copy.html
                using var writer = conn.BeginBinaryImport(@"COPY relation (id, from_ci_id, to_ci_id, predicate_id, changeset_id, layer_id, state, ""timestamp"") FROM STDIN (FORMAT BINARY)");
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
                }
                writer.Complete();
            }

            return true;
        }
    }
}
