using DotLiquid.Tags;
using Hangfire.States;
using Landscape.Base.Entity;
using Landscape.Base.Model;
using Npgsql;
using System;
using System.Collections.Generic;
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

        // TODO: make MergedRelation its own type
        // TODO: make it work with list of ciIdentities
        private async Task<NpgsqlCommand> CreateMergedRelationCommand(Guid? ciid, bool includeRemoved, LayerSet layerset, IncludeRelationDirections ird, string additionalWhereClause, NpgsqlTransaction trans, DateTimeOffset? timeThreshold)
        {
            var tempLayersetTableName = await LayerSet.CreateLayerSetTempTable(layerset, "temp_layerset", conn, trans);

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
            var innerWhereClause = String.Join(" AND ", innerWhereClauses);
            if (innerWhereClause == "") innerWhereClause = "1=1";
            var query = $@"
            select distinct
            last_value(inn.id) over wndOut,
            last_value(inn.from_ci_id) over wndOut,
            last_value(inn.to_ci_id) over wndOut,
            last_value(inn.predicate_id) over wndOut,
            array_agg(inn.layer_id) over wndOut,
            last_value(inn.state) over wndOut,
            last_value(inn.changeset_id) over wndOut
            FROM (
                select distinct on (from_ci_id, to_ci_id, predicate_id, layer_id) * from
                    relation where timestamp <= @time_threshold and ({innerWhereClause}) order by from_ci_id, to_ci_id, predicate_id, layer_id, timestamp DESC
            ) inn
            inner join {tempLayersetTableName} ls ON inn.layer_id = ls.id -- inner join to only keep rows that are in the selected layers
            where inn.state != ALL(@excluded_states) -- remove entries from layers which' last item is deleted
            WINDOW wndOut AS(PARTITION by inn.from_ci_id, inn.to_ci_id, inn.predicate_id ORDER BY ls.order DESC -- sort by layer order
                ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING)
            ";

            var command = new NpgsqlCommand(query, conn, trans);
            if (ciid != null)
                command.Parameters.AddWithValue("ci_identity", ciid.Value);
            var excludedStates = (includeRemoved) ? new RelationState[] { } : new RelationState[] { RelationState.Removed };
            command.Parameters.AddWithValue("excluded_states", excludedStates);
            var finalTimeThreshold = timeThreshold ?? DateTimeOffset.Now;
            command.Parameters.AddWithValue("time_threshold", finalTimeThreshold);
            return command;
        }

        public async Task<IEnumerable<Relation>> GetMergedRelations(Guid? ciid, bool includeRemoved, LayerSet layerset, IncludeRelationDirections ird, NpgsqlTransaction trans, DateTimeOffset? timeThreshold = null)
        {
            var ret = new List<Relation>();

            var predicates = await predicateModel.GetPredicates(trans, timeThreshold, AnchorStateFilter.All);

            using (var command = await CreateMergedRelationCommand(ciid, includeRemoved, layerset, ird, null, trans, timeThreshold))
            {
                using var dr = await command.ExecuteReaderAsync();

                while (await dr.ReadAsync())
                {
                    var id = dr.GetInt64(0);
                    var fromCIID = dr.GetGuid(1);
                    var toCIID = dr.GetGuid(2);
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


        private async Task<Relation> GetRelation(Guid fromCIID, Guid toCIID, string predicateID, long layerID, NpgsqlTransaction trans, DateTimeOffset atTime)
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

        public async Task<IEnumerable<Relation>> GetMergedRelationsWithPredicateID(LayerSet layerset, bool includeRemoved, string predicateID, NpgsqlTransaction trans, DateTimeOffset? timeThreshold = null)
        {
            var ret = new List<Relation>();

            var predicates = await predicateModel.GetPredicates(trans, timeThreshold, AnchorStateFilter.All);

            using (var command = await CreateMergedRelationCommand(null, includeRemoved, layerset, IncludeRelationDirections.Both, $"predicate_id = '{predicateID}'", trans, timeThreshold))
            {
                using var dr = await command.ExecuteReaderAsync();

                while (await dr.ReadAsync())
                {
                    var id = dr.GetInt64(0);
                    var fromCIID = dr.GetGuid(1);
                    var toCIID = dr.GetGuid(2);
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

        public async Task<Relation> RemoveRelation(Guid fromCIID, Guid toCIID, string predicateID, long layerID, long changesetID, NpgsqlTransaction trans)
        {
            var currentRelation = await GetRelation(fromCIID, toCIID, predicateID, layerID, trans, DateTimeOffset.Now);

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

            var predicates = await predicateModel.GetPredicates(trans, DateTimeOffset.Now, AnchorStateFilter.All);

            using var command = new NpgsqlCommand(@"INSERT INTO relation (from_ci_id, to_ci_id, predicate_id, layer_id, state, changeset_id, timestamp) 
                VALUES (@from_ci_id, @to_ci_id, @predicate_id, @layer_id, @state, @changeset_id, now()) returning id", conn, trans);

            command.Parameters.AddWithValue("from_ci_id", fromCIID);
            command.Parameters.AddWithValue("to_ci_id", toCIID);
            command.Parameters.AddWithValue("predicate_id", predicateID);
            command.Parameters.AddWithValue("layer_id", layerID);
            command.Parameters.AddWithValue("state", RelationState.Removed);
            command.Parameters.AddWithValue("changeset_id", changesetID);

            var layerStack = new long[] { layerID }; // TODO: calculate proper layerstack(?)

            var predicate = predicates[predicateID]; // TODO: only get one predicate?

            var id = (long)await command.ExecuteScalarAsync();
            return Relation.Build(id, fromCIID, toCIID, predicate, layerStack, RelationState.Removed, changesetID);
        }

        public async Task<Relation> InsertRelation(Guid fromCIID, Guid toCIID, string predicateID, long layerID, long changesetID, NpgsqlTransaction trans)
        {
            var currentRelation = await GetRelation(fromCIID, toCIID, predicateID, layerID, trans, DateTimeOffset.Now);

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

            var predicates = await predicateModel.GetPredicates(trans, DateTimeOffset.Now, AnchorStateFilter.ActiveOnly); // only active predicates allowed

            using var command = new NpgsqlCommand(@"INSERT INTO relation (from_ci_id, to_ci_id, predicate_id, layer_id, state, changeset_id, timestamp) 
                VALUES (@from_ci_id, @to_ci_id, @predicate_id, @layer_id, @state, @changeset_id, now()) returning id", conn, trans);

            command.Parameters.AddWithValue("from_ci_id", fromCIID);
            command.Parameters.AddWithValue("to_ci_id", toCIID);
            command.Parameters.AddWithValue("predicate_id", predicateID);
            command.Parameters.AddWithValue("layer_id", layerID);
            command.Parameters.AddWithValue("state", state);
            command.Parameters.AddWithValue("changeset_id", changesetID);

            var layerStack = new long[] { layerID }; // TODO: calculate proper layerstack(?)

            var predicate = predicates[predicateID]; // TODO: only get one predicate?

            var id = (long)await command.ExecuteScalarAsync();
            return Relation.Build(id, fromCIID, toCIID, predicate, layerStack, state, changesetID);
        }

        public async Task<bool> BulkReplaceRelations<F>(IBulkRelationData<F> data, long changesetID, NpgsqlTransaction trans)
        {
            var layerSet = new LayerSet(data.LayerID);
            var outdatedRelations = (data switch {
                BulkRelationDataPredicateScope p => (await GetMergedRelationsWithPredicateID(layerSet, false, p.PredicateID, trans)),
                BulkRelationDataLayerScope l => (await GetMergedRelations(null, false, layerSet, IncludeRelationDirections.Both, trans)),
                _ => null
            }).ToDictionary(r => r.InformationHash);

            // TODO: use postgres COPY feature instead of manual inserts https://www.npgsql.org/doc/copy.html
            foreach (var fragment in data.Fragments)
            {
                var fromCIID = data.GetFromCIID(fragment);
                var toCIID = data.GetToCIID(fragment);
                var predicateID = data.GetPredicateID(fragment);
                var informationHash = Relation.CreateInformationHash(fromCIID, toCIID, predicateID);
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

                using var command = new NpgsqlCommand(@"INSERT INTO relation (from_ci_id, to_ci_id, predicate_id, layer_id, state, changeset_id, timestamp) 
                    VALUES (@from_ci_id, @to_ci_id, @predicate_id, @layer_id, @state, @changeset_id, now()) returning id", conn, trans);

                command.Parameters.AddWithValue("from_ci_id", fromCIID);
                command.Parameters.AddWithValue("to_ci_id", toCIID);
                command.Parameters.AddWithValue("predicate_id", predicateID);
                command.Parameters.AddWithValue("layer_id", data.LayerID);
                command.Parameters.AddWithValue("state", state);
                command.Parameters.AddWithValue("changeset_id", changesetID);

                var id = (long)await command.ExecuteScalarAsync();
            }

            // remove outdated 
            foreach (var outdatedRelation in outdatedRelations.Values)
                await RemoveRelation(outdatedRelation.FromCIID, outdatedRelation.ToCIID, outdatedRelation.PredicateID, data.LayerID, changesetID, trans);

            return true;
        }
    }
}
