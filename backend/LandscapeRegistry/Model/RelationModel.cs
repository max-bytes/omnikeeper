﻿using DotLiquid.Tags;
using Hangfire.States;
using Landscape.Base.Entity;
using Landscape.Base.Inbound;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapeRegistry.Model
{
    public class RelationModel : IRelationModel
    {
        private readonly NpgsqlConnection conn;
        private readonly IPredicateModel predicateModel;
        private readonly IOnlineAccessProxy onlineAccessProxy;

        public RelationModel(IOnlineAccessProxy onlineAccessProxy, IPredicateModel predicateModel, NpgsqlConnection connection)
        {
            conn = connection;
            this.onlineAccessProxy = onlineAccessProxy;
            this.predicateModel = predicateModel;
        }

        private NpgsqlCommand CreateRelationCommand(IRelationSelection rl, long layerID, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            var innerWhereClauses = new List<string>();
            var parameters = new List<NpgsqlParameter>();
            switch (rl)
            {
                case RelationSelectionFromTo rsft:
                    if (rsft.fromCIID.HasValue)
                    {
                        innerWhereClauses.Add("(from_ci_id = @from_ci_id)");
                        parameters.Add(new NpgsqlParameter("from_ci_id", rsft.fromCIID.Value));
                    }
                    if (rsft.toCIID.HasValue)
                    {
                        innerWhereClauses.Add("(to_ci_id = @to_ci_id)");
                        parameters.Add(new NpgsqlParameter("to_ci_id", rsft.toCIID.Value));
                    }
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
            // if layer is online inbound layer, return from proxy
            if (await onlineAccessProxy.IsOnlineInboundLayer(layerID, trans))
            {
                return await onlineAccessProxy.GetRelation(fromCIID, toCIID, predicateID, layerID, trans, atTime);
            }

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
                var changesetID = dr.GetInt64(2);

                var predicate = predicates[predicateID];

                return Relation.Build(id, fromCIID, toCIID, predicate, state, changesetID);
            }
        }

        public async Task<IEnumerable<Relation>> GetRelations(IRelationSelection rs, bool includeRemoved, long layerID, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            // if layer is online inbound layer, return from proxy
            if (await onlineAccessProxy.IsOnlineInboundLayer(layerID, trans))
            {
                return onlineAccessProxy.GetRelations(rs, layerID, trans, atTime).ToEnumerable();
            }

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
                    var changesetID = dr.GetInt64(5);

                    var predicate = predicates[predicateID];
                    var relation = Relation.Build(id, fromCIID, toCIID, predicate, state, changesetID);

                    if (includeRemoved || state != RelationState.Removed)
                        relations.Add(relation);
                }
            }

            return relations;
        }



        private IEnumerable<MergedRelation> MergeRelations(IEnumerable<(Relation relation, long layerID)> relations, LayerSet layers)
        {
            var compound = new Dictionary<(Guid from_ciid, Guid to_ciid, string predicate_id), SortedList<int, (Relation relation, long layerID)>>();

            foreach (var (relation, layerID) in relations)
            {
                var layerSortOrder = layers.GetOrder(layerID);

                compound.AddOrUpdate((relation.FromCIID, relation.ToCIID, relation.PredicateID),
                    () => new SortedList<int, (Relation relation, long layerID)>() { { layerSortOrder, (relation, layerID) } },
                    (old) => { old.Add(layerSortOrder, (relation, layerID)); return old; }
                );
            }

            return compound.Select(t => MergedRelation.Build(t.Value.First().Value.relation, layerStackIDs: t.Value.Select(tt => tt.Value.layerID).Reverse().ToArray()));
        }
        public async Task<IEnumerable<MergedRelation>> GetMergedRelations(IRelationSelection rl, bool includeRemoved, LayerSet layerset, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            if (layerset.IsEmpty)
                return ImmutableList<MergedRelation>.Empty; // return empty, an empty layer list can never produce any relations

            var relations = new List<(Relation relation, long layerID)>();

            foreach (var layerID in layerset)
            {
                var lr = await GetRelations(rl, includeRemoved, layerID, trans, atTime);
                foreach (var r in lr)
                    relations.Add((r, layerID));
            }

            return MergeRelations(relations, layerset);
        }


        public async Task<Relation> RemoveRelation(Guid fromCIID, Guid toCIID, string predicateID, long layerID, IChangesetProxy changesetProxy, NpgsqlTransaction trans)
        {
            if (await onlineAccessProxy.IsOnlineInboundLayer(layerID, trans)) throw new Exception("Cannot write to online inbound layer");

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
            if (await onlineAccessProxy.IsOnlineInboundLayer(layerID, trans)) throw new Exception("Cannot write to online inbound layer");

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
            if (await onlineAccessProxy.IsOnlineInboundLayer(data.LayerID, trans)) throw new Exception("Cannot write to online inbound layer");

            var timeThreshold = TimeThreshold.BuildLatest();
            var layerSet = new LayerSet(data.LayerID); // TODO: rework to work with non-merged relations
            var outdatedRelations = (data switch {
                BulkRelationDataPredicateScope p => (await GetMergedRelations(new RelationSelectionWithPredicate(p.PredicateID), false, layerSet, trans, timeThreshold)),
                BulkRelationDataLayerScope l => (await GetMergedRelations(new RelationSelectionAll(), false, layerSet, trans, timeThreshold)),
                _ => null
            }).ToDictionary(r => r.InformationHash);

            // TODO: use postgres COPY feature instead of manual inserts https://www.npgsql.org/doc/copy.html
            foreach (var fragment in data.Fragments)
            {
                var id = Guid.NewGuid();
                var fromCIID = data.GetFromCIID(fragment);
                var toCIID = data.GetToCIID(fragment);

                if (fromCIID == toCIID)
                    throw new Exception("From and To CIID must not be the same!");

                var predicateID = data.GetPredicateID(fragment); // TODO: check if predicates are active
                var informationHash = MergedRelation.CreateInformationHash(fromCIID, toCIID, predicateID);
                // remove the current relation from the list of relations to remove
                outdatedRelations.Remove(informationHash, out var currentRelation);

                var state = RelationState.New;
                if (currentRelation != null)
                {
                    if (currentRelation.Relation.State == RelationState.Removed)
                        state = RelationState.Renewed;
                    else // same predicate already exists and is present, go to next pair
                        continue;
                }

                using var command = new NpgsqlCommand(@"INSERT INTO relation (id, from_ci_id, to_ci_id, predicate_id, layer_id, state, changeset_id, timestamp) 
                    VALUES (@id, @from_ci_id, @to_ci_id, @predicate_id, @layer_id, @state, @changeset_id, @timestamp)", conn, trans);

                var changeset = await changesetProxy.GetChangeset(trans);

                command.Parameters.AddWithValue("id", id);
                command.Parameters.AddWithValue("from_ci_id", fromCIID);
                command.Parameters.AddWithValue("to_ci_id", toCIID);
                command.Parameters.AddWithValue("predicate_id", predicateID);
                command.Parameters.AddWithValue("layer_id", data.LayerID);
                command.Parameters.AddWithValue("state", state);
                command.Parameters.AddWithValue("changeset_id", changeset.ID);
                command.Parameters.AddWithValue("timestamp", changeset.Timestamp);

                await command.ExecuteNonQueryAsync();
            }

            // remove outdated 
            foreach (var outdatedRelation in outdatedRelations.Values)
            {
                await RemoveRelation(outdatedRelation.Relation.FromCIID, outdatedRelation.Relation.ToCIID, outdatedRelation.Relation.PredicateID, data.LayerID, changesetProxy, trans); // TODO: proper timethreshold
            }

            return true;
        }
    }
}
