using GraphQL.DataLoader;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.GraphQL
{
    public static class DataLoaderUtils
    {
        public static IDataLoaderResult<IEnumerable<MergedRelation>> SetupAndLoadRelation(IRelationSelection rs, IDataLoaderContextAccessor dataLoaderContextAccessor, IRelationModel relationModel, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            return rs switch
            {
                RelationSelectionFrom f => SetupRelationFetchingFrom(dataLoaderContextAccessor, relationModel, layerSet, timeThreshold, trans).LoadAsync(f),
                RelationSelectionTo t => SetupRelationFetchingTo(dataLoaderContextAccessor, relationModel, layerSet, timeThreshold, trans).LoadAsync(t),
                RelationSelectionAll a => SetupRelationFetchingAll(dataLoaderContextAccessor, relationModel, layerSet, timeThreshold, trans).LoadAsync(a),
                _ => throw new Exception("Not support yet")
            };
        }

        private static IDataLoader<RelationSelectionFrom, IEnumerable<MergedRelation>> SetupRelationFetchingFrom(IDataLoaderContextAccessor dataLoaderContextAccessor, IRelationModel relationModel, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            var loader = dataLoaderContextAccessor.Context.GetOrAddCollectionBatchLoader($"GetMergedRelationsFrom_{layerSet}_{timeThreshold}",
                async (IEnumerable<RelationSelectionFrom> relationSelections) => {
                    var rFrom = await FetchRelationsFrom(layerSet, timeThreshold, trans, relationSelections, relationModel);
                    return rFrom.ToLookup(t => t.Item1, t => t.Item2);
                });
            return loader;
        }

        private static IDataLoader<RelationSelectionTo, IEnumerable<MergedRelation>> SetupRelationFetchingTo(IDataLoaderContextAccessor dataLoaderContextAccessor, IRelationModel relationModel, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            var loader = dataLoaderContextAccessor.Context.GetOrAddCollectionBatchLoader($"GetMergedRelationsTo_{layerSet}_{timeThreshold}",
                async (IEnumerable<RelationSelectionTo> relationSelections) => {
                    var rFrom = await FetchRelationsTo(layerSet, timeThreshold, trans, relationSelections, relationModel);
                    return rFrom.ToLookup(t => t.Item1, t => t.Item2);
                });
            return loader;
        }

        private static IDataLoader<RelationSelectionAll, IEnumerable<MergedRelation>> SetupRelationFetchingAll(IDataLoaderContextAccessor dataLoaderContextAccessor, IRelationModel relationModel, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            var loader = dataLoaderContextAccessor.Context.GetOrAddCollectionBatchLoader($"GetMergedRelationsAll_{layerSet}_{timeThreshold}",
                async (IEnumerable<RelationSelectionAll> relationSelections) => {
                    var rFrom = await FetchRelationsAll(layerSet, timeThreshold, trans, relationSelections, relationModel);
                    return rFrom;
                });
            return loader;
        }

        private static async Task<IList<(RelationSelectionFrom, MergedRelation)>> FetchRelationsFrom(LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans, IEnumerable<RelationSelectionFrom> relationSelections, IRelationModel relationModel)
        {
            var combinedRelationsFrom = new HashSet<Guid>();
            foreach (var rs in relationSelections)
                combinedRelationsFrom.UnionWith(rs.FromCIIDs);

            var relationsFrom = await relationModel.GetMergedRelations(RelationSelectionFrom.Build(combinedRelationsFrom), layerSet, trans, timeThreshold);

            var relationsFromMap = relationsFrom.ToLookup(t => t.Relation.FromCIID);

            var ret = new List<(RelationSelectionFrom, MergedRelation)>();
            foreach (var rs in relationSelections)
                foreach (var ciid in rs.FromCIIDs) ret.AddRange(relationsFromMap[ciid].Select(t => (rs, t)));
            return ret;
        }

        private static async Task<IList<(RelationSelectionTo, MergedRelation)>> FetchRelationsTo(LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans, IEnumerable<RelationSelectionTo> relationSelections, IRelationModel relationModel)
        {
            var combinedRelationsTo = new HashSet<Guid>();
            foreach (var rs in relationSelections)
                combinedRelationsTo.UnionWith(rs.ToCIIDs);

            var relationsTo = await relationModel.GetMergedRelations(RelationSelectionTo.Build(combinedRelationsTo), layerSet, trans, timeThreshold);

            var relationsToMap = relationsTo.ToLookup(t => t.Relation.ToCIID);

            var ret = new List<(RelationSelectionTo, MergedRelation)>();
            foreach (var rs in relationSelections)
                foreach (var ciid in rs.ToCIIDs) ret.AddRange(relationsToMap[ciid].Select(t => (rs, t)));
            return ret;
        }

        private static async Task<ILookup<RelationSelectionAll, MergedRelation>> FetchRelationsAll(LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans, IEnumerable<RelationSelectionAll> relationSelections, IRelationModel relationModel)
        {
            var relations = await relationModel.GetMergedRelations(RelationSelectionAll.Instance, layerSet, trans, timeThreshold);

            return relations.ToLookup(r => RelationSelectionAll.Instance);

            //var ret = new List<(RelationSelectionAll, MergedRelation)>()
            //{
            //};
            //foreach (var rs in relationSelections)
            //    foreach (var ciid in rs.ToCIIDs) ret.AddRange(relationsToMap[ciid].Select(t => (rs, t)));
            //return ret;
        }

        //private static async Task<ILookup<IRelationSelection, MergedRelation>> FetchRelations(LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans, IEnumerable<IRelationSelection> relationSelections, IRelationModel relationModel)
        //{
        //    var combinedRelationsTo = new HashSet<Guid>();
        //    var combinedRelationsFrom = new HashSet<Guid>();
        //    foreach (var rs in relationSelections)
        //    {
        //        switch (rs)
        //        {
        //            case RelationSelectionTo t:
        //                combinedRelationsTo.UnionWith(t.ToCIIDs);
        //                break;
        //            case RelationSelectionFrom f:
        //                combinedRelationsFrom.UnionWith(f.FromCIIDs);
        //                break;
        //            default:
        //                throw new NotSupportedException("Not supported (yet)");
        //        }
        //    }

        //    var relationsTo = await relationModel.GetMergedRelations(RelationSelectionTo.Build(combinedRelationsTo), layerSet, trans, timeThreshold);
        //    var relationsFrom = await relationModel.GetMergedRelations(RelationSelectionFrom.Build(combinedRelationsFrom), layerSet, trans, timeThreshold);

        //    var relationsToMap = relationsTo.ToLookup(t => t.Relation.ToCIID);
        //    var relationsFromMap = relationsFrom.ToLookup(t => t.Relation.FromCIID);

        //    var ret = new List<(IRelationSelection, MergedRelation)>();
        //    foreach (var rs in relationSelections)
        //    {
        //        switch (rs)
        //        {
        //            case RelationSelectionTo t:
        //                foreach (var ciid in t.ToCIIDs) ret.AddRange(relationsToMap[ciid].Select(t => (rs, t)));
        //                break;
        //            case RelationSelectionFrom f:
        //                foreach (var ciid in f.FromCIIDs) ret.AddRange(relationsFromMap[ciid].Select(t => (rs, t)));
        //                break;
        //            default:
        //                throw new NotSupportedException("Not supported (yet)");
        //        }
        //    }
        //    return ret.ToLookup(t => t.Item1, t => t.Item2);
        //}
    }
}
