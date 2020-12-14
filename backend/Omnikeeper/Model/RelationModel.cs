using Npgsql;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Model
{
    public class RelationModel : IRelationModel
    {
        private readonly IBaseRelationModel baseModel;

        public RelationModel(IBaseRelationModel baseModel)
        {
            this.baseModel = baseModel;
        }

        public async Task<Relation?> GetRelation(Guid fromCIID, Guid toCIID, string predicateID, long layerID, IModelContext trans, TimeThreshold atTime)
        {
            return await baseModel.GetRelation(fromCIID, toCIID, predicateID, layerID, trans, atTime);
        }

        public async Task<IEnumerable<Relation>> GetRelations(IRelationSelection rs, long layerID, IModelContext trans, TimeThreshold atTime)
        {
            return await baseModel.GetRelations(rs, layerID, trans, atTime);
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

            return compound.Select(t => new MergedRelation(t.Value.First().Value.relation, layerStackIDs: t.Value.Select(tt => tt.Value.layerID).Reverse().ToArray()));
        }
        public async Task<IEnumerable<MergedRelation>> GetMergedRelations(IRelationSelection rl, LayerSet layerset, IModelContext trans, TimeThreshold atTime)
        {
            if (layerset.IsEmpty)
                return ImmutableList<MergedRelation>.Empty; // return empty, an empty layer list can never produce any relations

            var relations = new List<(Relation relation, long layerID)>();

            foreach (var layerID in layerset)
            {
                var lr = await GetRelations(rl, layerID, trans, atTime);
                foreach (var r in lr)
                    relations.Add((r, layerID));
            }

            return MergeRelations(relations, layerset);
        }

        public async Task<MergedRelation?> GetMergedRelation(Guid fromCIID, Guid toCIID, string predicateID, LayerSet layerset, IModelContext trans, TimeThreshold atTime)
        {
            if (layerset.IsEmpty)
                return null; // return empty, an empty layer list can never produce any relations

            var relations = new List<(Relation relation, long layerID)>();

            foreach (var layerID in layerset)
            {
                var r = await GetRelation(fromCIID, toCIID, predicateID, layerID, trans, atTime);
                if (r != null)
                    relations.Add((r, layerID));
            }

            var mergedRelations = MergeRelations(relations, layerset);

            if (mergedRelations.Count() > 1)
                throw new Exception("Should never happen!");

            return mergedRelations.FirstOrDefault();
        }


        public async Task<(Relation relation, bool changed)> RemoveRelation(Guid fromCIID, Guid toCIID, string predicateID, long layerID, IChangesetProxy changesetProxy, IModelContext trans)
        {
            return await baseModel.RemoveRelation(fromCIID, toCIID, predicateID, layerID, changesetProxy, trans);
        }

        public async Task<(Relation relation, bool changed)> InsertRelation(Guid fromCIID, Guid toCIID, string predicateID, long layerID, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        {
            return await baseModel.InsertRelation(fromCIID, toCIID, predicateID, layerID, changesetProxy, origin, trans);
        }

        public async Task<IEnumerable<(Guid fromCIID, Guid toCIID, string predicateID, RelationState state)>> BulkReplaceRelations<F>(IBulkRelationData<F> data, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        {
            return await baseModel.BulkReplaceRelations(data, changesetProxy, origin, trans);
        }
    }
}
