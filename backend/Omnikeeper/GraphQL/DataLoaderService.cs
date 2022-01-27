using Autofac;
using GraphQL.DataLoader;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Omnikeeper.GraphQL
{
    public class DataLoaderService : IDataLoaderService
    {
        private readonly IDataLoaderContextAccessor dataLoaderContextAccessor;

        public DataLoaderService(IDataLoaderContextAccessor dataLoaderContextAccessor)
        {
            this.dataLoaderContextAccessor = dataLoaderContextAccessor;
        }

        // TODO: rework to also work with lists of CIs, then use throughout graphql resolvers
        public IDataLoaderResult<IEnumerable<EffectiveTrait>> SetupAndLoadEffectiveTraitLoader(MergedCI ci, ITraitSelection traitSelection, IEffectiveTraitModel traitModel, ITraitsProvider traitsProvider, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            var loader = dataLoaderContextAccessor.Context.GetOrAddCollectionBatchLoader($"GetAllEffectiveTraits_{layerSet}_{timeThreshold}",
                async (IEnumerable<(MergedCI ci, ITraitSelection traitSelection)> selections) =>
            {
                var traits = (await traitsProvider.GetActiveTraits(trans, timeThreshold)).Values;

                var requestedTraits = TraitSelectionExtensions.UnionAll(selections.Select(t => t.traitSelection));

                var finalTraits = traits.Where(t => requestedTraits.Contains(t.ID));

                var cis = selections.Select(t => t.ci).ToList();
                var ciMap = selections.ToDictionary(t => t.ci.ID);

                var tmp = new List<(Guid ciid, EffectiveTrait et)>(finalTraits.Count() * cis.Count);
                foreach (var trait in finalTraits)
                {
                    var etsPerTrait = await traitModel.GetEffectiveTraitsForTrait(trait, cis, layerSet, trans, timeThreshold);

                    foreach (var kv in etsPerTrait)
                    {
                        tmp.Add((kv.Key, kv.Value));
                    }
                }

                return tmp.ToLookup(kv => ciMap[kv.ciid], kv => kv.et, new MergedCIComparer());
            });
            return loader.LoadAsync((ci, traitSelection));
        }

        private class MergedCIComparer : IEqualityComparer<(MergedCI ci, ITraitSelection traitSelection)>
        {
            public bool Equals((MergedCI ci, ITraitSelection traitSelection) x, (MergedCI ci, ITraitSelection traitSelection) y)
            {
                return x.ci.ID.Equals(y.ci.ID) && x.traitSelection.Equals(y.traitSelection);
            }

            public int GetHashCode((MergedCI ci, ITraitSelection traitSelection) obj)
            {
                return HashCode.Combine(obj.ci.ID.GetHashCode(), obj.traitSelection.GetHashCode());
            }
        }

        public IDataLoaderResult<IEnumerable<MergedCI>> SetupAndLoadMergedCIs(ICIIDSelection ciidSelection, IAttributeSelection attributeSelection, bool includeEmptyCIs, ICIModel ciModel, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            var loader = dataLoaderContextAccessor.Context.GetOrAddCollectionBatchLoader($"GetMergedCIs_{layerSet}_{timeThreshold}_{includeEmptyCIs}",
                    async (IEnumerable<(ICIIDSelection ciidSelection, IAttributeSelection attributeSelection)> selections) =>
                    {
                        var combinedCIIDSelection = CIIDSelectionExtensions.UnionAll(selections.Select(s => s.ciidSelection));
                        var combinedAttributeSelection = AttributeSelectionExtensions.UnionAll(selections.Select(s => s.attributeSelection));

                        var combinedCIs = (await ciModel.GetMergedCIs(combinedCIIDSelection, layerSet, includeEmptyCIs, combinedAttributeSelection, trans, timeThreshold)).ToDictionary(ci => ci.ID);

                        var ret = new List<((ICIIDSelection ciidSelection, IAttributeSelection attributeSelection), MergedCI)>(); // NOTE: seems weird, cant lookup be created better?
                        foreach (var s in selections)
                        {
                            var selectedCIs = s.ciidSelection.FilterDictionary(combinedCIs);

                            // NOTE: we are NOT reducing the attributes again here, which means it's possible that this returns more attributes for some CIs than requested according to attributeSelection

                            ret.AddRange(selectedCIs.Select(ci => (s, ci)));
                        }
                        return ret.ToLookup(t => t.Item1, t => t.Item2);
                    });
            return loader.LoadAsync((ciidSelection, attributeSelection));
        }

        public IDataLoaderResult<IEnumerable<Changeset>> SetupAndLoadChangesets(ISet<Guid> ids, IChangesetModel changesetModel, IModelContext trans)
        {
            var loader = dataLoaderContextAccessor.Context.GetOrAddCollectionBatchLoader($"GetChangesets",
                    async (IEnumerable<ISet<Guid>> selections) =>
                    {
                        var combinedIDs = selections.SelectMany(id => id).ToHashSet();

                        var combinedChangesets = (await changesetModel.GetChangesets(combinedIDs, trans)).ToDictionary(ci => ci.ID);

                        // NOTE: seems weird, cant lookup be created better?
                        return selections.SelectMany(s => s.Select(id => (s, combinedChangesets[id]!))).ToLookup(t => t.s, t => t.Item2);
                    });
            return loader.LoadAsync(ids);
        }

        public IDataLoaderResult<IDictionary<Guid, string>> SetupAndLoadCINames(ICIIDSelection ciidSelection, IAttributeModel attributeModel, ICIIDModel ciidModel, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            var loader = dataLoaderContextAccessor.Context.GetOrAddBatchLoader<ICIIDSelection, IDictionary<Guid, string>>($"GetMergedCINames_{layerSet}_{timeThreshold}",
                async (IEnumerable<ICIIDSelection> ciidSelections) =>
                {
                    var combinedCIIDSelection = CIIDSelectionExtensions.UnionAll(ciidSelections);

                    var combinedNames = await attributeModel.GetMergedCINames(combinedCIIDSelection, layerSet, trans, timeThreshold);

                    var ret = new Dictionary<ICIIDSelection, IDictionary<Guid, string>>(ciidSelections.Count());
                    foreach (var ciidSelection in ciidSelections)
                    {
                        var ciids = await ciidSelection.GetCIIDsAsync(async () => await ciidModel.GetCIIDs(trans));
                        var selectedNames = ciids.Where(combinedNames.ContainsKey).ToDictionary(ciid => ciid, ciid => combinedNames[ciid]);
                        ret.Add(ciidSelection, selectedNames);
                    }
                    return ret;
                });
            return loader.LoadAsync(ciidSelection);
        }

        public IDataLoaderResult<IDictionary<string, LayerData>> SetupAndLoadAllLayers(ILayerDataModel layerDataModel, TimeThreshold timeThreshold, IModelContext trans)
        {
            var loader = dataLoaderContextAccessor.Context.GetOrAddLoader($"GetAllLayers_{timeThreshold}", () => layerDataModel.GetLayerData(trans, timeThreshold));
            return loader.LoadAsync();
        }

        public IDataLoaderResult<IEnumerable<MergedRelation>> SetupAndLoadRelation(IRelationSelection rs, IRelationModel relationModel, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            return rs switch
            {
                RelationSelectionFrom f => SetupRelationFetchingFrom(relationModel, layerSet, timeThreshold, trans).LoadAsync(f),
                RelationSelectionTo t => SetupRelationFetchingTo(relationModel, layerSet, timeThreshold, trans).LoadAsync(t),
                RelationSelectionAll a => SetupRelationFetchingAll(relationModel, layerSet, timeThreshold, trans).LoadAsync(a),
                _ => throw new Exception("Not support yet")
            };
        }

        private IDataLoader<RelationSelectionFrom, IEnumerable<MergedRelation>> SetupRelationFetchingFrom(IRelationModel relationModel, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            var loader = dataLoaderContextAccessor.Context.GetOrAddCollectionBatchLoader($"GetMergedRelationsFrom_{layerSet}_{timeThreshold}",
                async (IEnumerable<RelationSelectionFrom> relationSelections) =>
                {
                    var combinedRelationsFrom = new HashSet<Guid>();
                    foreach (var rs in relationSelections)
                        combinedRelationsFrom.UnionWith(rs.FromCIIDs);

                    // TODO: masking
                    var relationsFrom = await relationModel.GetMergedRelations(RelationSelectionFrom.Build(combinedRelationsFrom), layerSet, trans, timeThreshold, MaskHandlingForRetrievalGetMasks.Instance);
                    var relationsFromMap = relationsFrom.ToLookup(t => t.Relation.FromCIID);

                    var ret = new List<(RelationSelectionFrom, MergedRelation)>();
                    foreach (var rs in relationSelections)
                        foreach (var ciid in rs.FromCIIDs) ret.AddRange(relationsFromMap[ciid].Select(t => (rs, t)));
                    return ret.ToLookup(t => t.Item1, t => t.Item2);
                });
            return loader;
        }

        private IDataLoader<RelationSelectionTo, IEnumerable<MergedRelation>> SetupRelationFetchingTo(IRelationModel relationModel, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            var loader = dataLoaderContextAccessor.Context.GetOrAddCollectionBatchLoader($"GetMergedRelationsTo_{layerSet}_{timeThreshold}",
                async (IEnumerable<RelationSelectionTo> relationSelections) =>
                {
                    var combinedRelationsTo = new HashSet<Guid>();
                    foreach (var rs in relationSelections)
                        combinedRelationsTo.UnionWith(rs.ToCIIDs);

                    // TODO: masking
                    var relationsTo = await relationModel.GetMergedRelations(RelationSelectionTo.Build(combinedRelationsTo), layerSet, trans, timeThreshold, MaskHandlingForRetrievalGetMasks.Instance);
                    var relationsToMap = relationsTo.ToLookup(t => t.Relation.ToCIID);

                    var ret = new List<(RelationSelectionTo, MergedRelation)>();
                    foreach (var rs in relationSelections)
                        foreach (var ciid in rs.ToCIIDs) ret.AddRange(relationsToMap[ciid].Select(t => (rs, t)));
                    return ret.ToLookup(t => t.Item1, t => t.Item2);
                });
            return loader;
        }

        private IDataLoader<RelationSelectionAll, IEnumerable<MergedRelation>> SetupRelationFetchingAll(IRelationModel relationModel, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            var loader = dataLoaderContextAccessor.Context.GetOrAddCollectionBatchLoader($"GetMergedRelationsAll_{layerSet}_{timeThreshold}",
                async (IEnumerable<RelationSelectionAll> relationSelections) =>
                {
                    // TODO: masking
                    var relations = await relationModel.GetMergedRelations(RelationSelectionAll.Instance, layerSet, trans, timeThreshold, MaskHandlingForRetrievalGetMasks.Instance);
                    return relations.ToLookup(r => RelationSelectionAll.Instance);
                });
            return loader;
        }
    }


    public interface IDataLoaderService
    {
        IDataLoaderResult<IEnumerable<Changeset>> SetupAndLoadChangesets(ISet<Guid> ids, IChangesetModel changesetModel, IModelContext trans);
        IDataLoaderResult<IEnumerable<EffectiveTrait>> SetupAndLoadEffectiveTraitLoader(MergedCI ci, ITraitSelection traitSelection, IEffectiveTraitModel traitModel, ITraitsProvider traitsProvider, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans);
        IDataLoaderResult<IEnumerable<MergedCI>> SetupAndLoadMergedCIs(ICIIDSelection ciidSelection, IAttributeSelection attributeSelection, bool includeEmptyCIs, ICIModel ciModel, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans);
        IDataLoaderResult<IDictionary<Guid, string>> SetupAndLoadCINames(ICIIDSelection ciidSelection, IAttributeModel attributeModel, ICIIDModel ciidModel, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans);
        IDataLoaderResult<IDictionary<string, LayerData>> SetupAndLoadAllLayers(ILayerDataModel layerDataModel, TimeThreshold timeThreshold, IModelContext trans);
        IDataLoaderResult<IEnumerable<MergedRelation>> SetupAndLoadRelation(IRelationSelection rs, IRelationModel relationModel, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans);
    }
}
