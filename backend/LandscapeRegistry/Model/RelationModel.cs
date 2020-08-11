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
    public class RelationModel : IRelationModel
    {
        private readonly IBaseRelationModel baseModel;

        public RelationModel(IBaseRelationModel baseModel)
        {
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
            return await BulkReplaceRelations(data, changesetProxy, trans);
        }
    }
}
