﻿using GraphQL.DataLoader;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Omnikeeper.GraphQL
{
    public static class DataLoaderUtils
    {
        public static IDataLoader<MergedCI, IEnumerable<EffectiveTrait>> SetupEffectiveTraitLoader(IDataLoaderContextAccessor dataLoaderContextAccessor, IEffectiveTraitModel traitModel, ITraitsProvider traitsProvider, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            var loader = dataLoaderContextAccessor.Context.GetOrAddCollectionBatchLoader("GetAllEffectiveTraits", async (IEnumerable<MergedCI> cis) =>
            {
                var traits = (await traitsProvider.GetActiveTraits(trans, timeThreshold)).Values;

                var tmp = new Dictionary<Guid, IList<EffectiveTrait>>();
                var ciMap = cis.ToDictionary(ci => ci.ID);
                foreach (var trait in traits)
                {
                    var etsPerTrait = await traitModel.GetEffectiveTraitsForTrait(trait, cis, layerSet, trans, timeThreshold);

                    foreach (var kv in etsPerTrait)
                    {
                        tmp.AddOrUpdate(kv.Key, () => new List<EffectiveTrait>() { kv.Value }, (l) => { l.Add(kv.Value); return l; });
                    }
                }

                var t = tmp.SelectMany(kv => kv.Value.Select(v => (ciid: kv.Key, et: v)));
                return t.ToLookup(kv => ciMap[kv.ciid], kv => kv.et, new MergedCIComparer());
            });
            return loader;
        }

        private class MergedCIComparer : IEqualityComparer<MergedCI>
        {
            public bool Equals(MergedCI? x, MergedCI? y)
            {
                if (x == null && y == null) return true;
                else if (x == null || y == null) return false;
                else return x.ID.Equals(y.ID);
            }

            public int GetHashCode(MergedCI obj)
            {
                return obj.ID.GetHashCode();
            }
        }


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
                    var combinedRelationsFrom = new HashSet<Guid>();
                    foreach (var rs in relationSelections)
                        combinedRelationsFrom.UnionWith(rs.FromCIIDs);

                    var relationsFrom = await relationModel.GetMergedRelations(RelationSelectionFrom.Build(combinedRelationsFrom), layerSet, trans, timeThreshold);
                    var relationsFromMap = relationsFrom.ToLookup(t => t.Relation.FromCIID);

                    var ret = new List<(RelationSelectionFrom, MergedRelation)>();
                    foreach (var rs in relationSelections)
                        foreach (var ciid in rs.FromCIIDs) ret.AddRange(relationsFromMap[ciid].Select(t => (rs, t)));
                    return ret.ToLookup(t => t.Item1, t => t.Item2);
                });
            return loader;
        }

        private static IDataLoader<RelationSelectionTo, IEnumerable<MergedRelation>> SetupRelationFetchingTo(IDataLoaderContextAccessor dataLoaderContextAccessor, IRelationModel relationModel, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            var loader = dataLoaderContextAccessor.Context.GetOrAddCollectionBatchLoader($"GetMergedRelationsTo_{layerSet}_{timeThreshold}",
                async (IEnumerable<RelationSelectionTo> relationSelections) => {
                    var combinedRelationsTo = new HashSet<Guid>();
                    foreach (var rs in relationSelections)
                        combinedRelationsTo.UnionWith(rs.ToCIIDs);

                    var relationsTo = await relationModel.GetMergedRelations(RelationSelectionTo.Build(combinedRelationsTo), layerSet, trans, timeThreshold);
                    var relationsToMap = relationsTo.ToLookup(t => t.Relation.ToCIID);

                    var ret = new List<(RelationSelectionTo, MergedRelation)>();
                    foreach (var rs in relationSelections)
                        foreach (var ciid in rs.ToCIIDs) ret.AddRange(relationsToMap[ciid].Select(t => (rs, t)));
                    return ret.ToLookup(t => t.Item1, t => t.Item2);
                });
            return loader;
        }

        private static IDataLoader<RelationSelectionAll, IEnumerable<MergedRelation>> SetupRelationFetchingAll(IDataLoaderContextAccessor dataLoaderContextAccessor, IRelationModel relationModel, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            var loader = dataLoaderContextAccessor.Context.GetOrAddCollectionBatchLoader($"GetMergedRelationsAll_{layerSet}_{timeThreshold}",
                async (IEnumerable<RelationSelectionAll> relationSelections) => {
                    var relations = await relationModel.GetMergedRelations(RelationSelectionAll.Instance, layerSet, trans, timeThreshold);
                    return relations.ToLookup(r => RelationSelectionAll.Instance);
                });
            return loader;
        }
    }
}
