using DotLiquid.Tags;
using Hangfire.States;
using Landscape.Base.Entity;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using static Landscape.Base.Model.IRelationModel;

namespace LandscapeRegistry.Model
{
    public class RelationModel : IRelationModel
    {
        private readonly NpgsqlConnection conn;
        private readonly IPredicateModel predicateModel;

        public RelationModel(IPredicateModel predicateModel, NpgsqlConnection connection)
        {
            conn = connection;
            this.predicateModel = predicateModel;
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

        // TODO: make it work with list of ciIdentities
        private NpgsqlCommand CreateRelationCommand(Guid? ciid, LayerSet layerset, IncludeRelationDirections ird, string additionalWhereClause, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            var innerWhereClauses = new List<string>();
            if (ciid != null)
                innerWhereClauses.Add(ird switch
                {
                    IncludeRelationDirections.Forward => "(from_ci_id = @ci_identity)",
                    IncludeRelationDirections.Backward => "(to_ci_id = @ci_identity)",
                    IncludeRelationDirections.Both => "(from_ci_id = @ci_identity OR to_ci_id = @ci_identity)",
                    _ => "unused, should not happen, error otherwise",
                });
            if (additionalWhereClause != null)
                innerWhereClauses.Add(additionalWhereClause);
            var innerWhereClause = string.Join(" AND ", innerWhereClauses);
            if (innerWhereClause == "") innerWhereClause = "1=1";
            var query = $@"
                select distinct on (from_ci_id, to_ci_id, predicate_id, layer_id) id, from_ci_id, to_ci_id, predicate_id, layer_id, state, changeset_id from
                    relation where timestamp <= @time_threshold and ({innerWhereClause}) and layer_id = ANY(@layer_ids) order by from_ci_id, to_ci_id, predicate_id, layer_id, timestamp DESC
            ";

            //) inn
            //inner join({ lsValues}) as ls(id, ""order"") ON inn.layer_id = ls.id-- inner join to only keep rows that are in the selected layers
            //where inn.state != ALL(@excluded_states)-- remove entries from layers which' last item is deleted
            //WINDOW wndOut AS(PARTITION by inn.from_ci_id, inn.to_ci_id, inn.predicate_id ORDER BY ls.order DESC-- sort by layer order
            //    ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING)

            var command = new NpgsqlCommand(query, conn, trans);
            if (ciid != null)
                command.Parameters.AddWithValue("ci_identity", ciid.Value);
            command.Parameters.AddWithValue("time_threshold", atTime.Time);
            command.Parameters.AddWithValue("layer_ids", layerset.ToArray());
            return command;
        }

        public async Task<IEnumerable<MergedRelation>> GetMergedRelations(Guid? ciid, bool includeRemoved, LayerSet layerset, IncludeRelationDirections ird, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            if (layerset.IsEmpty)
                return ImmutableList<MergedRelation>.Empty; // return empty, an empty layer list can never produce any relations

            var predicates = await predicateModel.GetPredicates(trans, atTime, AnchorStateFilter.All);

            var relations = new List<(Relation relation, long layerID)>();
            using (var command = CreateRelationCommand(ciid, layerset, ird, null, trans, atTime))
            {
                using var dr = await command.ExecuteReaderAsync();

                while (await dr.ReadAsync())
                {
                    var id = dr.GetInt64(0);
                    var fromCIID = dr.GetGuid(1);
                    var toCIID = dr.GetGuid(2);
                    var predicateID = dr.GetString(3);
                    var layerID = dr.GetInt64(4);
                    var state = dr.GetFieldValue<RelationState>(5);
                    var changesetID = dr.GetInt64(6);

                    var predicate = predicates[predicateID];
                    var relation = Relation.Build(id, fromCIID, toCIID, predicate, state, changesetID);

                    if (includeRemoved || state != RelationState.Removed)
                        relations.Add((relation, layerID));
                }
            }

            return MergeRelations(relations, layerset);
        }


        private async Task<Relation> GetRelation(Guid fromCIID, Guid toCIID, string predicateID, long layerID, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            var predicates = await predicateModel.GetPredicates(trans, atTime, AnchorStateFilter.All);

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

                var id = dr.GetInt64(0);
                var state = dr.GetFieldValue<RelationState>(1);
                var changesetID = dr.GetInt64(2);

                var predicate = predicates[predicateID];

                return Relation.Build(id, fromCIID, toCIID, predicate, state, changesetID);
            }
        }

        public async Task<IEnumerable<MergedRelation>> GetMergedRelationsWithPredicateID(LayerSet layerset, bool includeRemoved, string predicateID, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            if (layerset.IsEmpty)
                return ImmutableList<MergedRelation>.Empty; // return empty, an empty layer list can never produce any relations

            var predicates = await predicateModel.GetPredicates(trans, atTime, AnchorStateFilter.All);

            var relations = new List<(Relation relation, long layerID)>();
            using (var command = CreateRelationCommand(null, layerset, IncludeRelationDirections.Both, $"predicate_id = '{predicateID}'", trans, atTime))
            {
                using var dr = await command.ExecuteReaderAsync();

                while (await dr.ReadAsync())
                {
                    var id = dr.GetInt64(0);
                    var fromCIID = dr.GetGuid(1);
                    var toCIID = dr.GetGuid(2);
                    var predicateIDOut = dr.GetString(3);
                    var layerID = dr.GetInt64(4);
                    var state = dr.GetFieldValue<RelationState>(5);
                    var changesetID = dr.GetInt64(6);

                    var predicate = predicates[predicateIDOut];

                    var relation = Relation.Build(id, fromCIID, toCIID, predicate, state, changesetID);

                    if (includeRemoved || state != RelationState.Removed)
                        relations.Add((relation, layerID));
                }
            }
            return MergeRelations(relations, layerset);
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

            using var command = new NpgsqlCommand(@"INSERT INTO relation (from_ci_id, to_ci_id, predicate_id, layer_id, state, changeset_id, timestamp) 
                VALUES (@from_ci_id, @to_ci_id, @predicate_id, @layer_id, @state, @changeset_id, @timestamp) returning id", conn, trans);

            var changeset = await changesetProxy.GetChangeset(trans);

            command.Parameters.AddWithValue("from_ci_id", fromCIID);
            command.Parameters.AddWithValue("to_ci_id", toCIID);
            command.Parameters.AddWithValue("predicate_id", predicateID);
            command.Parameters.AddWithValue("layer_id", layerID);
            command.Parameters.AddWithValue("state", RelationState.Removed);
            command.Parameters.AddWithValue("changeset_id", changeset.ID);
            command.Parameters.AddWithValue("timestamp", changeset.Timestamp);

            var predicate = predicates[predicateID]; // TODO: only get one predicate?

            var id = (long)await command.ExecuteScalarAsync();
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

            using var command = new NpgsqlCommand(@"INSERT INTO relation (from_ci_id, to_ci_id, predicate_id, layer_id, state, changeset_id, timestamp) 
                VALUES (@from_ci_id, @to_ci_id, @predicate_id, @layer_id, @state, @changeset_id, @timestamp) returning id", conn, trans);

            var changeset = await changesetProxy.GetChangeset(trans);

            command.Parameters.AddWithValue("from_ci_id", fromCIID);
            command.Parameters.AddWithValue("to_ci_id", toCIID);
            command.Parameters.AddWithValue("predicate_id", predicateID);
            command.Parameters.AddWithValue("layer_id", layerID);
            command.Parameters.AddWithValue("state", state);
            command.Parameters.AddWithValue("changeset_id", changeset.ID);
            command.Parameters.AddWithValue("timestamp", changeset.Timestamp);

            var predicate = predicates[predicateID]; // TODO: only get one predicate?

            var id = (long)await command.ExecuteScalarAsync();
            return Relation.Build(id, fromCIID, toCIID, predicate, state, changeset.ID);
        }

        public async Task<bool> BulkReplaceRelations<F>(IBulkRelationData<F> data, IChangesetProxy changesetProxy, NpgsqlTransaction trans)
        {
            var timeThreshold = TimeThreshold.BuildLatest();
            var layerSet = new LayerSet(data.LayerID); // TODO: rework to work with non-merged relations
            var outdatedRelations = (data switch {
                BulkRelationDataPredicateScope p => (await GetMergedRelationsWithPredicateID(layerSet, false, p.PredicateID, trans, timeThreshold)),
                BulkRelationDataLayerScope l => (await GetMergedRelations(null, false, layerSet, IncludeRelationDirections.Both, trans, timeThreshold)),
                _ => null
            }).ToDictionary(r => r.InformationHash);

            // TODO: use postgres COPY feature instead of manual inserts https://www.npgsql.org/doc/copy.html
            foreach (var fragment in data.Fragments)
            {
                var fromCIID = data.GetFromCIID(fragment);
                var toCIID = data.GetToCIID(fragment);

                if (fromCIID == toCIID)
                    throw new Exception("From and To CIID must not be the same!");

                var predicateID = data.GetPredicateID(fragment);
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

                using var command = new NpgsqlCommand(@"INSERT INTO relation (from_ci_id, to_ci_id, predicate_id, layer_id, state, changeset_id, timestamp) 
                    VALUES (@from_ci_id, @to_ci_id, @predicate_id, @layer_id, @state, @changeset_id, @timestamp) returning id", conn, trans);

                var changeset = await changesetProxy.GetChangeset(trans);

                command.Parameters.AddWithValue("from_ci_id", fromCIID);
                command.Parameters.AddWithValue("to_ci_id", toCIID);
                command.Parameters.AddWithValue("predicate_id", predicateID);
                command.Parameters.AddWithValue("layer_id", data.LayerID);
                command.Parameters.AddWithValue("state", state);
                command.Parameters.AddWithValue("changeset_id", changeset.ID);
                command.Parameters.AddWithValue("timestamp", changeset.Timestamp);

                var id = (long)await command.ExecuteScalarAsync();
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
