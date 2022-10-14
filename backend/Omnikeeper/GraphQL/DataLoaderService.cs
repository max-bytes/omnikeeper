using Autofac;
using GraphQL.DataLoader;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.GraphQL;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.TraitBased;
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
        private readonly IEffectiveTraitModel traitModel;
        private readonly ICIModel ciModel;
        private readonly IAttributeModel attributeModel;
        private readonly IEffectiveTraitModel effectiveTraitModel;
        private readonly ITraitsProvider traitsProvider;
        private readonly ILayerDataModel layerDataModel;
        private readonly IRelationModel relationModel;
        private readonly IChangesetModel changesetModel;
        private readonly IUserInDatabaseModel userInDatabaseModel;

        public DataLoaderService(IDataLoaderContextAccessor dataLoaderContextAccessor, IEffectiveTraitModel traitModel, ICIModel ciModel, IAttributeModel attributeModel, IEffectiveTraitModel effectiveTraitModel,
            ITraitsProvider traitsProvider, ILayerDataModel layerDataModel, IRelationModel relationModel, IChangesetModel changesetModel, IUserInDatabaseModel userInDatabaseModel)
        {
            this.dataLoaderContextAccessor = dataLoaderContextAccessor;
            this.traitModel = traitModel;
            this.ciModel = ciModel;
            this.attributeModel = attributeModel;
            this.effectiveTraitModel = effectiveTraitModel;
            this.traitsProvider = traitsProvider;
            this.layerDataModel = layerDataModel;
            this.relationModel = relationModel;
            this.changesetModel = changesetModel;
            this.userInDatabaseModel = userInDatabaseModel;
        }

        public IDataLoaderResult<IEnumerable<EffectiveTrait>> SetupAndLoadEffectiveTraits(MergedCI ci, ITraitSelection traitSelection, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            var loader = dataLoaderContextAccessor.Context.GetOrAddCollectionBatchLoader($"GetAllEffectiveTraits_{layerSet}_{timeThreshold}",
                async (IEnumerable<(MergedCI ci, ITraitSelection traitSelection)> selections) =>
            {
                var traits = (await traitsProvider.GetActiveTraits(trans, timeThreshold)).Values;
                var requestedTraits = TraitSelectionExtensions.UnionAll(selections.Select(t => t.traitSelection));
                var finalTraits = traits.Where(t => requestedTraits.Contains(t.ID));

                // this results in a (nested) dictionary, that contains a dictionary of distinct CIs PER requested trait
                var trait2CIDictionary = selections
                    .SelectMany(t => traits.Where(trait => t.traitSelection.Contains(trait.ID)).Select(trait => (t.ci, trait.ID)))
                    .GroupBy(t => t.ID)
                    .ToDictionary(t => t.Key, t => t.GroupBy(tt => tt.ci.ID).Select(tt => tt.First().ci).ToDictionary(tt => tt.ID));

                // this results in a (nested) dictionary, that contains a dictionary of distinct trait selections PER requested trait
                var trait2TraitSelectionDictionary = selections
                    .SelectMany(t => traits.Where(trait => t.traitSelection.Contains(trait.ID)).Select(trait => (t.traitSelection, trait.ID)))
                    .GroupBy(t => t.ID)
                    .ToDictionary(t => t.Key, t => t.GroupBy(tt => tt.traitSelection.GetHashCode()).Select(tt => tt.First().traitSelection));

                var tmp = new List<(MergedCI ci, ITraitSelection traitSelection, EffectiveTrait et)>();
                foreach (var trait in finalTraits)
                {
                    var cis = trait2CIDictionary[trait.ID];

                    var traitSelections = trait2TraitSelectionDictionary[trait.ID];
                    var etsPerTrait = await traitModel.GetEffectiveTraitsForTrait(trait, cis.Values, layerSet, trans, timeThreshold);

                    foreach (var traitSelection in traitSelections)
                    {
                        foreach (var kv in etsPerTrait)
                        {
                            var ci = cis[kv.Key];
                            tmp.Add((ci, traitSelection, kv.Value));
                        }
                    }
                }

                return tmp.ToLookup(kv => (kv.ci, kv.traitSelection), kv => kv.et, new MergedCIComparer());
            });
            return loader.LoadAsync((ci, traitSelection));
        }

        public IDataLoaderResult<IDictionary<Guid, EffectiveTrait>> SetupAndLoadEffectiveTraits(ICIIDSelection ciids, ITrait trait, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            var loader = dataLoaderContextAccessor.Context.GetOrAddBatchLoader<ICIIDSelection, IDictionary<Guid, EffectiveTrait>>($"GetEffectiveTraits_{layerSet}_{timeThreshold}_{trait.ID}",
                async (IEnumerable<ICIIDSelection> selections) =>
                {
                    var combinedCIIDSelection = CIIDSelectionExtensions.UnionAll(selections);

                    var attributeSelection = NamedAttributesSelection.Build(
                        trait.RequiredAttributes.Select(ra => ra.AttributeTemplate.Name).Union(
                        trait.OptionalAttributes.Select(oa => oa.AttributeTemplate.Name)).ToHashSet()
                    );

                    var combinedAttributes = await attributeModel.GetMergedAttributes(combinedCIIDSelection, attributeSelection, layerSet, trans, timeThreshold, GeneratedDataHandlingInclude.Instance);
                    var combinedCIs = ciModel.BuildMergedCIs(combinedAttributes, layerSet, timeThreshold);
                    var ets = await effectiveTraitModel.GetEffectiveTraitsForTrait(trait, combinedCIs, layerSet, trans, timeThreshold);

                    return selections.ToDictionary(selection => selection, selection => selection.FilterDictionary2Dictionary(ets));
                }
            );
            return loader.LoadAsync(ciids);
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

        public IDataLoaderResult<IDictionary<Guid, IDictionary<string, MergedCIAttribute>>> SetupAndLoadMergedAttributes(ICIIDSelection ciidSelection, IAttributeSelection attributeSelection, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            var dlContext = dataLoaderContextAccessor.Context;
            if (dlContext != null)
            {
                var loader = dlContext.GetOrAddBatchLoader<(ICIIDSelection ciidSelection, IAttributeSelection attributeSelection), IDictionary<Guid, IDictionary<string, MergedCIAttribute>>>($"GetMergedAttributes_{layerSet}_{timeThreshold}",
                        async (IEnumerable<(ICIIDSelection ciidSelection, IAttributeSelection attributeSelection)> selections) =>
                        {
                            var combinedCIIDSelection = CIIDSelectionExtensions.UnionAll(selections.Select(s => s.ciidSelection));
                            var combinedAttributeSelection = AttributeSelectionExtensions.UnionAll(selections.Select(s => s.attributeSelection));

                            var combinedAttributes = await attributeModel.GetMergedAttributes(combinedCIIDSelection, combinedAttributeSelection, layerSet, trans, timeThreshold, GeneratedDataHandlingInclude.Instance);

                            var ret = new Dictionary<(ICIIDSelection ciidSelection, IAttributeSelection attributeSelection), IDictionary<Guid, IDictionary<string, MergedCIAttribute>>>(); // NOTE: seems weird, cant lookup be created better?
                            foreach (var s in selections)
                            {
                                var selectedAttributes = s.ciidSelection.FilterDictionary2Dictionary(combinedAttributes);

                                // NOTE: we are NOT reducing the attributes again here, which means it's possible that this returns more attributes than requested according to attributeSelection

                                ret.Add(s, selectedAttributes);
                            }
                            return ret;
                        });
                return loader.LoadAsync((ciidSelection, attributeSelection));
            } else
            {
                return new SimpleDataLoader<IDictionary<Guid, IDictionary<string, MergedCIAttribute>>>(async token =>
                {
                    return await attributeModel.GetMergedAttributes(ciidSelection, attributeSelection, layerSet, trans, timeThreshold, GeneratedDataHandlingInclude.Instance);
                });
            }
        }

        public IDataLoaderResult<IEnumerable<MergedCI>> SetupAndLoadMergedCIs(ICIIDSelection ciidSelection, IAttributeSelection attributeSelection, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            var attributeLoader = SetupAndLoadMergedAttributes(ciidSelection, attributeSelection, layerSet, timeThreshold, trans);

            return attributeLoader.Then(attributes => (IEnumerable<MergedCI>)ciModel.BuildMergedCIs(attributes, layerSet, timeThreshold));
        }

        public IDataLoaderResult<IEnumerable<Changeset>> SetupAndLoadChangesets(ISet<Guid> ids, IModelContext trans)
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

        public IDataLoaderResult<UserInDatabase> SetupAndLoadUser(long userID, TimeThreshold timeThreshold, IModelContext trans)
        {
            var loader = dataLoaderContextAccessor.Context.GetOrAddBatchLoader<long, UserInDatabase>($"GetUser_{timeThreshold}",
                async (IEnumerable<long> userIDs) =>
                {
                    var combinedUserIDs = userIDs.ToHashSet();
                    var ret = (await userInDatabaseModel.GetUsers(combinedUserIDs, trans, timeThreshold)).ToDictionary(u => u.ID);
                    return ret;
                });
            return loader.LoadAsync(userID);
        }

        public IDataLoaderResult<IDictionary<Guid, string>> SetupAndLoadCINames(ICIIDSelection ciidSelection, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            var loader = dataLoaderContextAccessor.Context.GetOrAddBatchLoader<ICIIDSelection, IDictionary<Guid, string>>($"GetMergedCINames_{layerSet}_{timeThreshold}",
                async (IEnumerable<ICIIDSelection> ciidSelections) =>
                {
                    var combinedCIIDSelection = CIIDSelectionExtensions.UnionAll(ciidSelections);

                    var combinedNames = await attributeModel.GetMergedCINames(combinedCIIDSelection, layerSet, trans, timeThreshold);

                    var ret = new Dictionary<ICIIDSelection, IDictionary<Guid, string>>(ciidSelections.Count());
                    foreach (var ciidSelection in ciidSelections)
                    {
                        var ciids = await ciidSelection.GetCIIDsAsync(async () => await ciModel.GetCIIDs(trans));
                        var selectedNames = ciids.Where(combinedNames.ContainsKey).ToDictionary(ciid => ciid, ciid => combinedNames[ciid]);
                        ret.Add(ciidSelection, selectedNames);
                    }
                    return ret;
                });
            return loader.LoadAsync(ciidSelection);
        }

        public IDataLoaderResult<IDictionary<string, LayerData>> SetupAndLoadAllLayers(TimeThreshold timeThreshold, IModelContext trans)
        {
            var loader = dataLoaderContextAccessor.Context.GetOrAddLoader($"GetAllLayers_{timeThreshold}", () => layerDataModel.GetLayerData(trans, timeThreshold));
            return loader.LoadAsync();
        }

        public IDataLoaderResult<IEnumerable<MergedRelation>> SetupAndLoadRelation(IRelationSelection rs, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            return rs switch
            {
                RelationSelectionFrom f => SetupRelationFetchingFrom(f, layerSet, timeThreshold, trans).LoadAsync(f),
                RelationSelectionTo t => SetupRelationFetchingTo(t, layerSet, timeThreshold, trans).LoadAsync(t),
                RelationSelectionAll a => SetupRelationFetchingAll(layerSet, timeThreshold, trans).LoadAsync(a),
                RelationSelectionWithPredicate p => SetupRelationFetchingWithPredicate(layerSet, timeThreshold, trans).LoadAsync(p),
                _ => throw new Exception("Not support yet")
            };
        }

        private IDataLoader<RelationSelectionFrom, IEnumerable<MergedRelation>> SetupRelationFetchingFrom(RelationSelectionFrom f, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            // NOTE: we dont combine relationSelections with differing PredicateIDs
            var loader = dataLoaderContextAccessor.Context.GetOrAddCollectionBatchLoader($"GetMergedRelationsFrom_{layerSet}_{timeThreshold}{((f.PredicateIDs != null) ? "_" + string.Join(",", f.PredicateIDs) : "")}",
                async (IEnumerable<RelationSelectionFrom> relationSelections) =>
                {
                    var combinedRelationsFrom = new HashSet<Guid>();
                    foreach (var rs in relationSelections)
                        combinedRelationsFrom.UnionWith(rs.FromCIIDs);

                    // TODO: masking
                    var combinedSelection = (f.PredicateIDs == null) ? RelationSelectionFrom.BuildWithAllPredicateIDs(combinedRelationsFrom) : RelationSelectionFrom.Build(f.PredicateIDs, combinedRelationsFrom);
            var relationsFrom = await relationModel.GetMergedRelations(combinedSelection, layerSet, trans, timeThreshold, MaskHandlingForRetrievalGetMasks.Instance, GeneratedDataHandlingInclude.Instance);
                    var relationsFromMap = relationsFrom.ToLookup(t => t.Relation.FromCIID);

                    var ret = new List<(RelationSelectionFrom, MergedRelation)>();
                    foreach (var rs in relationSelections)
                        foreach (var ciid in rs.FromCIIDs) ret.AddRange(relationsFromMap[ciid].Select(t => (rs, t)));
                    return ret.ToLookup(t => t.Item1, t => t.Item2);
                });
            return loader;
        }

        private IDataLoader<RelationSelectionTo, IEnumerable<MergedRelation>> SetupRelationFetchingTo(RelationSelectionTo t, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            var loader = dataLoaderContextAccessor.Context.GetOrAddCollectionBatchLoader($"GetMergedRelationsTo_{layerSet}_{timeThreshold}{((t.PredicateIDs != null) ? "_" + string.Join(",", t.PredicateIDs) : "")}",
                async (IEnumerable<RelationSelectionTo> relationSelections) =>
                {
                    var combinedRelationsTo = new HashSet<Guid>();
                    foreach (var rs in relationSelections)
                        combinedRelationsTo.UnionWith(rs.ToCIIDs);

                    // TODO: masking
                    var combinedSelection = (t.PredicateIDs == null) ? RelationSelectionTo.BuildWithAllPredicateIDs(combinedRelationsTo) : RelationSelectionTo.Build(t.PredicateIDs, combinedRelationsTo);
                    var relationsTo = await relationModel.GetMergedRelations(combinedSelection, layerSet, trans, timeThreshold, MaskHandlingForRetrievalGetMasks.Instance, GeneratedDataHandlingInclude.Instance);
                    var relationsToMap = relationsTo.ToLookup(t => t.Relation.ToCIID);

                    var ret = new List<(RelationSelectionTo, MergedRelation)>();
                    foreach (var rs in relationSelections)
                        foreach (var ciid in rs.ToCIIDs) ret.AddRange(relationsToMap[ciid].Select(t => (rs, t)));
                    return ret.ToLookup(t => t.Item1, t => t.Item2);
                });
            return loader;
        }

        private IDataLoader<RelationSelectionAll, IEnumerable<MergedRelation>> SetupRelationFetchingAll(LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            var loader = dataLoaderContextAccessor.Context.GetOrAddCollectionBatchLoader($"GetMergedRelationsAll_{layerSet}_{timeThreshold}",
                async (IEnumerable<RelationSelectionAll> relationSelections) =>
                {
                    // TODO: masking
                    var relations = await relationModel.GetMergedRelations(RelationSelectionAll.Instance, layerSet, trans, timeThreshold, MaskHandlingForRetrievalGetMasks.Instance, GeneratedDataHandlingInclude.Instance);
                    return relations.ToLookup(r => RelationSelectionAll.Instance);
                });
            return loader;
        }


        private IDataLoader<RelationSelectionWithPredicate, IEnumerable<MergedRelation>> SetupRelationFetchingWithPredicate(LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            var loader = dataLoaderContextAccessor.Context.GetOrAddCollectionBatchLoader($"GetMergedRelationsWithPredicate_{layerSet}_{timeThreshold}",
                async (IEnumerable<RelationSelectionWithPredicate> relationSelections) =>
                {
                    var combinedRelationPredicateIDs = new HashSet<string>();
                    foreach (var rs in relationSelections)
                        combinedRelationPredicateIDs.UnionWith(rs.PredicateIDs);

                    // TODO: masking
                    var relationsWithPredicate = await relationModel.GetMergedRelations(RelationSelectionWithPredicate.Build(combinedRelationPredicateIDs), layerSet, trans, timeThreshold, MaskHandlingForRetrievalGetMasks.Instance, GeneratedDataHandlingInclude.Instance);
                    var relationsWithPredicateMap = relationsWithPredicate.ToLookup(t => t.Relation.PredicateID);

                    var ret = new List<(RelationSelectionWithPredicate, MergedRelation)>();
                    foreach (var rs in relationSelections)
                        foreach (var predicateID in rs.PredicateIDs) ret.AddRange(relationsWithPredicateMap[predicateID].Select(t => (rs, t)));
                    return ret.ToLookup(t => t.Item1, t => t.Item2);
                });
            return loader;
        }

        public IDataLoaderResult<IDictionary<Guid, Changeset>> SetupAndLoadLatestRelevantChangesetPerCI(ICIIDSelection ciidSelection, IAttributeSelection attributeSelection, IPredicateSelection predicateSelection, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            var loader = dataLoaderContextAccessor.Context.GetOrAddBatchLoader<ICIIDSelection, IDictionary<Guid, Changeset>>($"GetLatestRelevantChangesetPerCI_{layerSet}_{timeThreshold}_{attributeSelection}_{predicateSelection}",
                async (IEnumerable<ICIIDSelection> ciidSelections) =>
                {
                    var combinedCIIDSelection = CIIDSelectionExtensions.UnionAll(ciidSelections);

                    var combined = await changesetModel.GetLatestChangesetPerCI(combinedCIIDSelection, attributeSelection, predicateSelection, layerSet.LayerIDs, trans, timeThreshold);

                    var ret = new Dictionary<ICIIDSelection, IDictionary<Guid, Changeset>>(); // NOTE: seems weird, cant lookup be created better?
                    foreach (var s in ciidSelections)
                    {
                        var selected = s.FilterDictionary2Dictionary(combined);

                        ret.Add(s, selected);
                    }
                    return ret;
                });
            return loader.LoadAsync(ciidSelection);
        }
        public IDataLoaderResult<IDictionary<Guid, Changeset>> SetupAndLoadLatestRelevantChangesetPerTraitEntity(ICIIDSelection ciidSelection, bool includeRemovedTraitEntities, bool filterOutNonTraitEntityCIs, TraitEntityModel traitEntityModel, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            var loader = dataLoaderContextAccessor.Context.GetOrAddBatchLoader<ICIIDSelection, IDictionary<Guid, Changeset>>($"GetLatestRelevantChangesetOfTraitEntityPerCI_{traitEntityModel.TraitID}_{includeRemovedTraitEntities}_{filterOutNonTraitEntityCIs}_{layerSet}_{timeThreshold}",
                async (IEnumerable<ICIIDSelection> ciidSelections) =>
                {
                    var combinedCIIDSelection = CIIDSelectionExtensions.UnionAll(ciidSelections);

                    var combined = await traitEntityModel.GetLatestRelevantChangesetPerTraitEntity(combinedCIIDSelection, includeRemovedTraitEntities, filterOutNonTraitEntityCIs, layerSet, trans, timeThreshold);

                    var ret = new Dictionary<ICIIDSelection, IDictionary<Guid, Changeset>>(); // NOTE: seems weird, cant lookup be created better?
                    foreach (var s in ciidSelections)
                    {
                        var selected = s.FilterDictionary2Dictionary(combined);

                        ret.Add(s, selected);
                    }
                    return ret;
                });
            return loader.LoadAsync(ciidSelection);
        }

        
    }
}
