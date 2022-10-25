//using Omnikeeper.Base.Entity;
//using Omnikeeper.Base.Model;
//using Omnikeeper.Base.Model.TraitBased;
//using Omnikeeper.Base.Utils;
//using Omnikeeper.Base.Utils.ModelContext;
//using System;
//using System.Collections.Generic;
//using System.Collections.Immutable;
//using System.Diagnostics.CodeAnalysis;
//using System.Linq;
//using System.Threading.Tasks;

//namespace Omnikeeper.Base.Model.Incremental
//{
//    public class IncrementalCISelectionModel
//    {
//        private readonly IChangesetModel changesetModel;

//        public IncrementalCISelectionModel(IChangesetModel changesetModel)
//        {
//            this.changesetModel = changesetModel;
//        }

//        public async Task<UpdateSet> InitOrUpdate(IReadOnlyDictionary<string, IReadOnlyList<Changeset>?> unprocessedChangesets, LayerSet layerSet, IModelContext trans)
//        {
//            // collect relevant changesets, check if we can do incremental update
//            var updatedCISelections = new List<ICIIDSelection>();
//            var canUpdateIncrementally = true;
//            foreach (var layerID in layerSet)
//            {
//                if (unprocessedChangesets.TryGetValue(layerID, out var uc))
//                {
//                    if (uc == null)
//                    {
//                        canUpdateIncrementally = false;
//                        break;
//                    }
//                    foreach (var changeset in uc)
//                    {
//                        var changedCIIDs = await changesetModel.GetCIIDsAffectedByChangeset(changeset.ID, trans);
//                        var changedCIIDSelection = SpecificCIIDsSelection.Build(changedCIIDs);
//                        updatedCISelections.Add(changedCIIDSelection);
//                    }
//                }
//                else
//                {
//                    canUpdateIncrementally = false;
//                    break;
//                }
//            }

//            if (!canUpdateIncrementally)
//            {
//                return new UpdateSet(AllCIIDsSelection.Instance, true);
//            }
//            else
//            {
//                return new UpdateSet(CIIDSelectionExtensions.UnionAll(updatedCISelections), false);
//            }
//        }

//        public class UpdateSet
//        {
//            public readonly ICIIDSelection Selection;
//            public readonly bool ForcedFullUpdate;

//            public UpdateSet(ICIIDSelection selection, bool forcedFullUpdate)
//            {
//                Selection = selection;
//                ForcedFullUpdate = forcedFullUpdate;
//            }
//        }
//    }


//    public class IncrementalCIModel
//    {
//        private readonly ICIModel ciModel;
//        private readonly IAttributeModel attributeModel;
//        private readonly IncrementalStore store;

//        public IncrementalCIModel(ICIModel ciModel, IAttributeModel attributeModel, IncrementalStore store)
//        {
//            this.ciModel = ciModel;
//            this.attributeModel = attributeModel;
//            this.store = store;
//        }

//        public async Task<UpdateSet> InitOrUpdate(IncrementalCISelectionModel.UpdateSet ciSelection, LayerSet layerSet, IAttributeSelection attributeSelection, string context, IModelContext trans, TimeThreshold timeThreshold)
//        {
//            var key = $"IncrementalCIModel_{string.Join(",", layerSet)}_{context}";

//            if (!store.TryGet<IDictionary<Guid, MergedCI>>(key, out var so) || ciSelection.ForcedFullUpdate)
//            {
//                var initialCIs = await ciModel.GetMergedCIs(AllCIIDsSelection.Instance, layerSet, false, attributeSelection, trans, timeThreshold);
//                var newSo = new Dictionary<Guid, MergedCI>();
//                foreach (var ci in initialCIs)
//                {
//                    newSo.Add(ci.ID, ci);
//                }
//                store.Set<IDictionary<Guid, MergedCI>>(key, newSo);

//                return new UpdateSet(newSo, initialCIs, ImmutableList<Guid>.Empty, true);
//            }
//            else
//            {
//                var updatedCISelection = ciSelection.Selection;
//                if (updatedCISelection is NoCIIDsSelection)
//                    return new UpdateSet((IReadOnlyDictionary<Guid, MergedCI>)so, ImmutableList<MergedCI>.Empty, ImmutableList<Guid>.Empty, false);

//                var updatedAttributes = await attributeModel.GetMergedAttributes(updatedCISelection, attributeSelection, layerSet, trans, timeThreshold, GeneratedDataHandlingInclude.Instance);

//                var updatedCIs = ciModel.BuildMergedCIs(updatedAttributes, layerSet, timeThreshold); // TODO: implement and use yield-variant (maybe even replace?)

//                // update
//                foreach (var updatedCI in updatedCIs)
//                {
//                    // NOTE: no change detection necessary(?)
//                    so[updatedCI.ID] = updatedCI;
//                }

//                // remove empty
//                var allSelectedCIIDs = await updatedCISelection.GetCIIDsAsync(async () => await ciModel.GetCIIDs(trans));
//                var emptyCIIDs = allSelectedCIIDs.Except(updatedAttributes.Keys);
//                var emptiedCIs = new HashSet<Guid>();
//                foreach (var emptyCIID in emptyCIIDs)
//                {
//                    var removed = so.Remove(emptyCIID);
//                    if (removed)
//                        emptiedCIs.Add(emptyCIID);
//                }

//                return new UpdateSet((IReadOnlyDictionary<Guid, MergedCI>)so, updatedCIs, emptiedCIs, false);
//            }
//        }
//        public class UpdateSet : IncrementalUpdateSet<IReadOnlyDictionary<Guid, MergedCI>, IEnumerable<MergedCI>>
//        {
//            public UpdateSet(IReadOnlyDictionary<Guid, MergedCI> all, IEnumerable<MergedCI> updated, IEnumerable<Guid> removed, bool forcedFullUpdate)
//                : base(all, updated, removed, forcedFullUpdate) { }
//        }
//    }

//    public class IncrementalEffectiveTraitModel
//    {
//        private readonly IEffectiveTraitModel effectiveTraitModel;
//        private readonly IncrementalStore store;

//        public IncrementalEffectiveTraitModel(IEffectiveTraitModel effectiveTraitModel, IncrementalStore store)
//        {
//            this.effectiveTraitModel = effectiveTraitModel;
//            this.store = store;
//        }

//        public async Task<UpdateSet> InitOrUpdate(IncrementalCIModel.UpdateSet cis, ITrait trait, LayerSet layerSet, string context, IModelContext trans, TimeThreshold timeThreshold)
//        {
//            var key = $"IncrementalEffectiveTraitModel_{string.Join(",", layerSet)}_{trait.ID}_{context}";
//            if (!store.TryGet<IDictionary<Guid, EffectiveTrait>>(key, out var so) || cis.ForcedFullUpdate)
//            {
//                var tmp = await effectiveTraitModel.GetEffectiveTraitsForTrait(trait, cis.All.Values, layerSet, trans, timeThreshold);
//                store.Set(key, tmp);
//                return new UpdateSet((IReadOnlyDictionary<Guid, EffectiveTrait>)tmp, (IReadOnlyDictionary<Guid, EffectiveTrait>)tmp, ImmutableList<Guid>.Empty, true);
//            }
//            else
//            {
//                var removedETs = new HashSet<Guid>();
//                foreach (var emptiedCI in cis.Removed)
//                {
//                    if (so.Remove(emptiedCI))
//                        removedETs.Add(emptiedCI);
//                }

//                var updatedETs = await effectiveTraitModel.GetEffectiveTraitsForTrait(trait, cis.Updated, layerSet, trans, timeThreshold);
//                foreach (var uet in updatedETs)
//                {
//                    // TODO: proper change detection
//                    so[uet.Key] = uet.Value;
//                }

//                return new UpdateSet((IReadOnlyDictionary<Guid, EffectiveTrait>)so, (IReadOnlyDictionary<Guid, EffectiveTrait>)updatedETs, removedETs, false);
//            }
//        }
//        public class UpdateSet : IncrementalUpdateSet<IReadOnlyDictionary<Guid, EffectiveTrait>, IReadOnlyDictionary<Guid, EffectiveTrait>>
//        {
//            public UpdateSet(IReadOnlyDictionary<Guid, EffectiveTrait> all, IReadOnlyDictionary<Guid, EffectiveTrait> updated, IEnumerable<Guid> removed, bool forcedFullUpdate)
//                : base(all, updated, removed, forcedFullUpdate) { }
//        }
//    }

//    public class IncrementalGenericTraitEntity<T> where T : TraitEntity, new()
//    {
//        private readonly IncrementalStore store;
//        protected readonly IEnumerable<TraitAttributeFieldInfo> attributeFieldInfos;
//        protected readonly IEnumerable<TraitRelationFieldInfo> relationFieldInfos;

//        public IncrementalGenericTraitEntity(IncrementalStore store)
//        {
//            this.store = store;
//            (_, attributeFieldInfos, relationFieldInfos) = GenericTraitEntityHelper.ExtractFieldInfos<T>();
//        }
//        public UpdateSet InitOrUpdate(IncrementalEffectiveTraitModel.UpdateSet ets, LayerSet layerSet, string context, IModelContext trans, TimeThreshold timeThreshold)
//        {
//            var key = $"IncrementalGenericTraitEntity_{typeof(T).Name}_{string.Join(",", layerSet)}_{context}";
//            if (!store.TryGet<IDictionary<Guid, T>>(key, out var so) || ets.ForcedFullUpdate)
//            {
//                var entities = ets.All.ToDictionary(kv => kv.Key, kv => GenericTraitEntityHelper.EffectiveTrait2Object<T>(kv.Value, attributeFieldInfos, relationFieldInfos));
//                store.Set<IDictionary<Guid, T>>(key, entities);
//                return new UpdateSet(entities, entities, ImmutableList<Guid>.Empty, true);
//            }
//            else
//            {
//                var removedEntities = new HashSet<Guid>();
//                foreach (var removedEntity in ets.Removed)
//                {
//                    if (so.Remove(removedEntity))
//                        removedEntities.Add(removedEntity);
//                }

//                var updatedEntities = ets.Updated.ToDictionary(kv => kv.Key, kv => GenericTraitEntityHelper.EffectiveTrait2Object<T>(kv.Value, attributeFieldInfos, relationFieldInfos));
//                foreach (var uet in updatedEntities)
//                {
//                    // TODO: proper change detection necessary? Should be done in IncrementalEffectiveTraitModel
//                    so[uet.Key] = uet.Value;
//                }

//                return new UpdateSet((IReadOnlyDictionary<Guid, T>)so, updatedEntities, removedEntities, false);
//            }
//        }
//        public class UpdateSet : IncrementalUpdateSet<IReadOnlyDictionary<Guid, T>, IReadOnlyDictionary<Guid, T>>
//        {
//            public UpdateSet(IReadOnlyDictionary<Guid, T> all, IReadOnlyDictionary<Guid, T> updated, IEnumerable<Guid> removed, bool forcedFullUpdate)
//                : base(all, updated, removed, forcedFullUpdate) { }
//        }
//    }

//    public class IncrementalUpdateSet<AT, UT>
//    {
//        public readonly AT All;
//        public readonly UT Updated;
//        public readonly IEnumerable<Guid> Removed;
//        public readonly bool ForcedFullUpdate;

//        public IncrementalUpdateSet(AT all, UT updated, IEnumerable<Guid> removed, bool forcedFullUpdate)
//        {
//            All = all;
//            Updated = updated;
//            Removed = removed;
//            ForcedFullUpdate = forcedFullUpdate;
//        }
//    }

//    public class IncrementalStore
//    {
//        private readonly IDictionary<string, object> store;

//        public IncrementalStore()
//        {
//            store = new Dictionary<string, object>();
//        }

//        public bool TryGet<O>(string key, [MaybeNullWhen(false)] out O o) where O : class
//        {
//            if (store.TryGetValue(key, out var tmp) && tmp is O tmp2)
//            {
//                o = tmp2;
//                return true;
//            }
//            else
//            {
//                o = null;
//                return false;
//            }
//        }

//        internal void Set<O>(string key, O so) where O : class
//        {
//            store[key] = so;
//        }
//    }
//}
