using DotLiquid.Tags;
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
        private readonly IOnlineAccessProxy onlineAccessProxy;
        private readonly IBaseRelationModel baseModel;

        public RelationModel(IOnlineAccessProxy onlineAccessProxy, IBaseRelationModel baseModel, NpgsqlConnection connection)
        {
            conn = connection;
            this.onlineAccessProxy = onlineAccessProxy;
            this.baseModel = baseModel;
        }

        public async Task<Relation> GetRelation(Guid fromCIID, Guid toCIID, string predicateID, long layerID, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            return await baseModel.GetRelation(fromCIID, toCIID, predicateID, layerID, trans, atTime);
        }

        public async Task<IEnumerable<Relation>> GetRelations(IRelationSelection rs, bool includeRemoved, long layerID, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            return await baseModel.GetRelations(rs, includeRemoved, layerID, trans, atTime);
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
            return await baseModel.RemoveRelation(fromCIID, toCIID, predicateID, layerID, changesetProxy, trans);
        }

        public async Task<Relation> InsertRelation(Guid fromCIID, Guid toCIID, string predicateID, long layerID, IChangesetProxy changesetProxy, NpgsqlTransaction trans)
        {
            return await baseModel.InsertRelation(fromCIID, toCIID, predicateID, layerID, changesetProxy, trans);
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
