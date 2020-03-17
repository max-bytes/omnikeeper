﻿using Landscape.Base.Model;
using LandscapePrototype.Entity;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapePrototype.Model
{
    public class RelationModel : IRelationModel
    {
        private readonly NpgsqlConnection conn;
        private readonly PredicateModel predicateModel;

        public RelationModel(PredicateModel predicateModel, NpgsqlConnection connection)
        {
            conn = connection;
            this.predicateModel = predicateModel;
        }

        public enum IncludeRelationDirections
        {
            Forward, Backward, Both
        }

        // TODO: make MergedRelation its own type
        // TODO: make it work with list of ciIdentities
        private async Task<NpgsqlCommand> CreateMergedRelationCommand(string ciIdentity, bool includeRemoved, LayerSet layerset, IncludeRelationDirections ird, string additionalWhereClause, NpgsqlTransaction trans, DateTimeOffset? timeThreshold)
        {
            var tempLayersetTableName = await LayerSet.CreateLayerSetTempTable(layerset, "temp_layerset", conn, trans);

            var innerWhereClauses = new List<string>();
            if (ciIdentity != null)
                innerWhereClauses.Add(ird switch
                {
                    IncludeRelationDirections.Forward => "(r.from_ci_id = @ci_identity)",
                    IncludeRelationDirections.Backward => "(r.to_ci_id = @ci_identity)",
                    IncludeRelationDirections.Both => "(r.from_ci_id = @ci_identity OR r.to_ci_id = @ci_identity)",
                    _ => "unused, should not happen, error otherwise",
                });
            if (additionalWhereClause != null)
                innerWhereClauses.Add(additionalWhereClause);
            var innerWhereClause = String.Join(" AND ", innerWhereClauses);
            var query = $@"
            select distinct
            last_value(inn.last_id) over wndOut,
            last_value(inn.last_from_ci_id) over wndOut,
            last_value(inn.last_to_ci_id) over wndOut,
            last_value(inn.last_predicate_id) over wndOut,
            array_agg(inn.last_layer_id) over wndOut,
            last_value(inn.last_state) over wndOut,
            last_value(inn.last_changeset_id) over wndOut
            FROM (
                select distinct
                last_value(r.id) over wnd as last_id,
                last_value(r.from_ci_id) over wnd as last_from_ci_id,
                last_value(r.to_ci_id) over wnd as last_to_ci_id,
                last_value(r.predicate_id) over wnd as last_predicate_id,
                last_value(r.layer_id) over wnd as last_layer_id,
                last_value(r.state) over wnd as last_state,
                last_value(r.changeset_id) over wnd as last_changeset_id
                    from relation r
                    inner join changeset c on c.id = r.changeset_id
                    where c.timestamp <= @time_threshold and ({innerWhereClause})
                WINDOW wnd AS(
                    PARTITION by r.from_ci_id, r.to_ci_id, r.predicate_id, r.layer_id ORDER BY c.timestamp ASC  -- sort by timestamp
                    ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING)
            ) inn
            inner join {tempLayersetTableName} ls ON inn.last_layer_id = ls.id -- inner join to only keep rows that are in the selected layers
            where inn.last_state != ALL(@excluded_states) -- remove entries from layers which' last item is deleted
            WINDOW wndOut AS(PARTITION by inn.last_from_ci_id, inn.last_to_ci_id, inn.last_predicate_id ORDER BY ls.order DESC -- sort by layer order
                ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING)
            ";

            var command = new NpgsqlCommand(query, conn, trans);
            if (ciIdentity != null)
                command.Parameters.AddWithValue("ci_identity", ciIdentity);
            var excludedStates = (includeRemoved) ? new RelationState[] { } : new RelationState[] { RelationState.Removed };
            command.Parameters.AddWithValue("excluded_states", excludedStates);
            var finalTimeThreshold = timeThreshold ?? DateTimeOffset.Now;
            command.Parameters.AddWithValue("time_threshold", finalTimeThreshold);
            return command;
        }

        public async Task<IEnumerable<Relation>> GetMergedRelations(string ciIdentity, bool includeRemoved, LayerSet layerset, IncludeRelationDirections ird, NpgsqlTransaction trans, DateTimeOffset? timeThreshold = null)
        {
            var ret = new List<Relation>();

            var predicates = await predicateModel.GetPredicates(trans, timeThreshold); // TODO: caching?

            using (var command = await CreateMergedRelationCommand(ciIdentity, includeRemoved, layerset, ird, null, trans, timeThreshold))
            {
                using var dr = await command.ExecuteReaderAsync();

                while (await dr.ReadAsync())
                {
                    var id = dr.GetInt64(0);
                    var fromCIID = dr.GetString(1);
                    var toCIID = dr.GetString(2);
                    var predicateID = dr.GetString(3);
                    var layerStack = (long[])dr[4];
                    var state = dr.GetFieldValue<RelationState>(5);
                    var changesetID = dr.GetInt64(6);

                    var predicate = predicates[predicateID];
                    var relation = Relation.Build(id, fromCIID, toCIID, predicate, layerStack, state, changesetID);

                    ret.Add(relation);
                }
            }
            return ret;
        }


        private async Task<Relation> GetMergedRelation(string fromCIID, string toCIID, string predicateID, long layerID, NpgsqlTransaction trans, DateTimeOffset atTime)
        {
            var predicates = await predicateModel.GetPredicates(trans, atTime); // TODO: caching?

            using (var command = new NpgsqlCommand(@"select distinct
            last_value(a.id) over wnd as last_id,
            last_value(a.state) over wnd as last_state,
            last_value(a.changeset_id) over wnd as last_changeset_id
                from relation a
                inner join changeset c on c.id = a.changeset_id
                where c.timestamp <= @time_threshold and a.from_ci_id = @from_ci_id AND a.to_ci_id = @to_ci_id and a.layer_id = @layer_id and a.predicate_id = @predicate_id
            WINDOW wnd AS(
                PARTITION by a.predicate_id, a.from_ci_id, a.to_ci_id ORDER BY c.timestamp
                ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING)
            LIMIT 1", conn, trans))
            {
                command.Parameters.AddWithValue("from_ci_id", fromCIID);
                command.Parameters.AddWithValue("to_ci_id", toCIID);
                command.Parameters.AddWithValue("predicate_id", predicateID);
                command.Parameters.AddWithValue("layer_id", layerID);
                command.Parameters.AddWithValue("time_threshold", atTime);
                using var dr = await command.ExecuteReaderAsync();
                if (!await dr.ReadAsync())
                    return null;

                var id = dr.GetInt64(0);
                var state = dr.GetFieldValue<RelationState>(1);
                var changesetID = dr.GetInt64(2);

                var predicate = predicates[predicateID];

                return Relation.Build(id, fromCIID, toCIID, predicate, new long[] { layerID }, state, changesetID);
            }
        }

        public async Task<IEnumerable<Relation>> GetRelationsWithPredicateID(LayerSet layerset, bool includeRemoved, string predicateID, NpgsqlTransaction trans, DateTimeOffset? timeThreshold = null)
        {
            var ret = new List<Relation>();

            var predicates = await predicateModel.GetPredicates(trans, timeThreshold); // TODO: caching?

            using (var command = await CreateMergedRelationCommand(null, includeRemoved, layerset, IncludeRelationDirections.Both, $"r.predicate_id = '{predicateID}'", trans, timeThreshold))
            {
                using var dr = await command.ExecuteReaderAsync();

                while (await dr.ReadAsync())
                {
                    var id = dr.GetInt64(0);
                    var fromCIID = dr.GetString(1);
                    var toCIID = dr.GetString(2);
                    var predicateIDOut = dr.GetString(3);
                    var layerStack = (long[])dr[4];
                    var state = dr.GetFieldValue<RelationState>(5);
                    var changesetID = dr.GetInt64(6);

                    var predicate = predicates[predicateIDOut];

                    var relation = Relation.Build(id, fromCIID, toCIID, predicate, layerStack, state, changesetID);

                    ret.Add(relation);
                }
            }
            return ret;
        }

        public async Task<Relation> RemoveRelation(string fromCIID, string toCIID, string predicateID, long layerID, long changesetID, NpgsqlTransaction trans)
        {
            var currentRelation = await GetMergedRelation(fromCIID, toCIID, predicateID, layerID, trans, DateTimeOffset.Now);

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

            var predicates = await predicateModel.GetPredicates(trans, DateTimeOffset.Now); // TODO: caching?

            using var command = new NpgsqlCommand(@"INSERT INTO relation (from_ci_id, to_ci_id, predicate_id, layer_id, state, changeset_id) 
                VALUES (@from_ci_id, @to_ci_id, @predicate_id, @layer_id, @state, @changeset_id) returning id", conn, trans);

            command.Parameters.AddWithValue("from_ci_id", fromCIID);
            command.Parameters.AddWithValue("to_ci_id", toCIID);
            command.Parameters.AddWithValue("predicate_id", predicateID);
            command.Parameters.AddWithValue("layer_id", layerID);
            command.Parameters.AddWithValue("state", RelationState.Removed);
            command.Parameters.AddWithValue("changeset_id", changesetID);

            var layerStack = new long[] { layerID }; // TODO: calculate proper layerstack(?)

            var predicate = predicates[predicateID];

            var id = (long)await command.ExecuteScalarAsync();
            return Relation.Build(id, fromCIID, toCIID, predicate, layerStack, RelationState.Removed, changesetID);
        }

        public async Task<Relation> InsertRelation(string fromCIID, string toCIID, string predicateID, long layerID, long changesetID, NpgsqlTransaction trans)
        {
            var currentRelation = await GetMergedRelation(fromCIID, toCIID, predicateID, layerID, trans, DateTimeOffset.Now);

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

            var predicates = await predicateModel.GetPredicates(trans, DateTimeOffset.Now); // TODO: caching?

            using var command = new NpgsqlCommand(@"INSERT INTO relation (from_ci_id, to_ci_id, predicate_id, layer_id, state, changeset_id) 
                VALUES (@from_ci_id, @to_ci_id, @predicate_id, @layer_id, @state, @changeset_id) returning id", conn, trans);

            command.Parameters.AddWithValue("from_ci_id", fromCIID);
            command.Parameters.AddWithValue("to_ci_id", toCIID);
            command.Parameters.AddWithValue("predicate_id", predicateID);
            command.Parameters.AddWithValue("layer_id", layerID);
            command.Parameters.AddWithValue("state", state);
            command.Parameters.AddWithValue("changeset_id", changesetID);

            var layerStack = new long[] { layerID }; // TODO: calculate proper layerstack(?)

            var predicate = predicates[predicateID];

            var id = (long)await command.ExecuteScalarAsync();
            return Relation.Build(id, fromCIID, toCIID, predicate, layerStack, state, changesetID);
        }

        public async Task<bool> BulkReplaceRelations(BulkRelationData data, long changesetID, NpgsqlTransaction trans)
        {
            var predicates = await predicateModel.GetPredicates(trans, DateTimeOffset.Now); // TODO: caching?

            var layerSet = new LayerSet(data.LayerID);
            var outdatedRelations = (await GetRelationsWithPredicateID(layerSet, false, data.PredicateID, trans))
                .ToDictionary(r => r.InformationHash);

            // TODO: use postgres COPY feature instead of manual inserts https://www.npgsql.org/doc/copy.html
            foreach (var ciidPair in data.FromToCIIDPairs)
            {
                var informationHash = Relation.CreateInformationHash(ciidPair.Item1, ciidPair.Item2, data.PredicateID);
                // remove the current relation from the list of relations to remove
                outdatedRelations.Remove(informationHash, out var currentRelation);

                var state = RelationState.New;
                if (currentRelation != null)
                {
                    if (currentRelation.State == RelationState.Removed)
                        state = RelationState.Renewed;
                    else // same predicate already exists and is present, go to next pair
                        continue;
                }

                using var command = new NpgsqlCommand(@"INSERT INTO relation (from_ci_id, to_ci_id, predicate_id, layer_id, state, changeset_id) 
                    VALUES (@from_ci_id, @to_ci_id, @predicate_id, @layer_id, @state, @changeset_id) returning id", conn, trans);

                command.Parameters.AddWithValue("from_ci_id", ciidPair.Item1);
                command.Parameters.AddWithValue("to_ci_id", ciidPair.Item2);
                command.Parameters.AddWithValue("predicate_id", data.PredicateID);
                command.Parameters.AddWithValue("layer_id", data.LayerID);
                command.Parameters.AddWithValue("state", state);
                command.Parameters.AddWithValue("changeset_id", changesetID);

                //var layerStack = new long[] { data.LayerID }; // TODO: calculate proper layerstack(?)

                //var predicate = predicates[data.PredicateID];

                var id = (long)await command.ExecuteScalarAsync();
                //Relation.Build(id, ciidPair.Item1, ciidPair.Item2, predicate, layerStack, state, changesetID);
            }

            // remove outdated 
            foreach(var outdatedRelation in outdatedRelations.Values)
                await RemoveRelation(outdatedRelation.FromCIID, outdatedRelation.ToCIID, data.PredicateID, data.LayerID, changesetID, trans);

            return true;
        }
    }
}
