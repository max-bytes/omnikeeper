using Omnikeeper.Base.Entity;
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

        private IEnumerable<MergedRelation> MergeRelations(IEnumerable<Relation>[] relations, string[] layerIDs, IMaskHandlingForRetrieval maskHandling)
        {
            var compound = new Dictionary<string, MergedRelation>();
            for (var i = 0; i < layerIDs.Length; i++)
            {
                var layerID = layerIDs[i];
                var rel = relations[i];

                foreach (var r in rel)
                {
                    if (compound.TryGetValue(r.InformationHash, out var existingRelation))
                    {
                        existingRelation.LayerStackIDs.Add(layerID);
                    }
                    else
                    {
                        compound.Add(r.InformationHash, new MergedRelation(r, new List<string>() { layerID }));
                    }
                }
            }

            switch (maskHandling)
            {
                case MaskHandlingForRetrievalApplyMasks:
                    return compound.Values.Where(r => !r.Relation.Mask);
                case MaskHandlingForRetrievalGetMasks:
                    return compound.Values;
                default:
                    throw new Exception("Unknown mask handling");
            }
        }

        public async Task<IEnumerable<MergedRelation>> GetMergedRelations(IRelationSelection rl, LayerSet layerset, IModelContext trans, TimeThreshold atTime, IMaskHandlingForRetrieval maskHandling, IGeneratedDataHandling generatedDataHandling)
        {
            if (layerset.IsEmpty)
                return ImmutableList<MergedRelation>.Empty; // return empty, an empty layer list can never produce any relations

            var lr = await baseModel.GetRelations(rl, layerset.LayerIDs, trans, atTime, generatedDataHandling);

            return MergeRelations(lr, layerset.LayerIDs, maskHandling);
        }

        public async Task<IReadOnlyList<Relation>> GetRelationsOfChangeset(Guid changesetID, bool getRemoved, IModelContext trans)
        {
            return await baseModel.GetRelationsOfChangeset(changesetID, getRemoved, trans);
        }

        public async Task<bool> InsertRelation(Guid fromCIID, Guid toCIID, string predicateID, bool mask, string layerID, IChangesetProxy changesetProxy, IModelContext trans, IOtherLayersValueHandling otherLayersValueHandling)
        {
            var scope = new BulkRelationDataSpecificScope(layerID, new BulkRelationDataSpecificScope.Fragment[] {
                new BulkRelationDataSpecificScope.Fragment(fromCIID, toCIID, predicateID, mask)
            }, Array.Empty<(Guid from, Guid to, string predicateID)>());
            var maskHandling = MaskHandlingForRemovalApplyNoMask.Instance; // NOTE: we can keep this fixed here, because it does not affect inserts

            var r = await BulkReplaceRelations(scope, changesetProxy, trans, maskHandling, otherLayersValueHandling);
            return r > 0;
        }

        public async Task<bool> RemoveRelation(Guid fromCIID, Guid toCIID, string predicateID, string layerID, IChangesetProxy changesetProxy, IModelContext trans, IMaskHandlingForRemoval maskHandling)
        {
            var scope = new BulkRelationDataSpecificScope(layerID, Array.Empty<BulkRelationDataSpecificScope.Fragment>(),
                new List<(Guid from, Guid to, string predicateID)> { (fromCIID, toCIID, predicateID) });
            var otherLayersValueHandling = OtherLayersValueHandlingForceWrite.Instance; // NOTE: we can keep this fixed here, because it does not affect removals

            var r = await BulkReplaceRelations(scope, changesetProxy, trans, maskHandling, otherLayersValueHandling);
            return r > 0;
        }

        // NOTE: this bulk operation DOES check if the relations that are inserted are "unique":
        // it is not possible to insert the "same" relation (same from_ciid, to_ciid, predicate_id and layer) multiple times
        // if this operation detects a duplicate, an exception is thrown;
        // the caller is responsible for making sure there are no duplicates
        private async Task<(
            IList<(Guid fromCIID, Guid toCIID, string predicateID, Guid? existingRelationID, Guid newRelationID, bool mask)> inserts,
            IDictionary<string, Relation> outdatedRelations
            )> PrepareForBulkUpdate<F>(IBulkRelationData<F> data, IModelContext trans, TimeThreshold readTS)
        {

            var outdatedRelations = (await GetRelationsInScope(data, new LayerSet(data.LayerID), trans, readTS))
                .Select(r => r.Relation).ToDictionary(r => r.InformationHash);

            var actualInserts = new List<(Guid fromCIID, Guid toCIID, string predicateID, Guid? existingRelationID, Guid newRelationID, bool mask)>();
            var informationHashesToInsert = new HashSet<string>();
            foreach (var fragment in data.Fragments)
            {
                var fromCIID = data.GetFromCIID(fragment);
                var toCIID = data.GetToCIID(fragment);
                if (fromCIID == toCIID)
                    throw new Exception("From and To CIID must not be the same!");

                var predicateID = data.GetPredicateID(fragment);
                if (predicateID.IsEmpty())
                    throw new Exception("PredicateID must not be empty");
                IDValidations.ValidatePredicateIDThrow(predicateID);

                var mask = data.GetMask(fragment);

                var informationHash = Relation.CreateInformationHash(fromCIID, toCIID, predicateID);
                if (informationHashesToInsert.Contains(informationHash))
                {
                    throw new Exception($"Duplicate relation fragment detected! Bulk insertion does not support duplicate relations; relation predicate ID: {predicateID}, from CIID: {fromCIID}, to CIID: {toCIID}");
                }
                informationHashesToInsert.Add(informationHash);

                // remove the current relation from the list of relations to remove
                outdatedRelations.Remove(informationHash, out var currentRelation);

                // compare masks, if mask (and everything else) is equal, skip this relation
                if (currentRelation != null && currentRelation.Mask == mask)
                {
                    continue;
                }

                Guid newRelationID = Guid.NewGuid();
                actualInserts.Add((fromCIID, toCIID, predicateID, currentRelation?.ID, newRelationID, mask));
            }

            return (actualInserts, outdatedRelations);
        }

        private async Task<IEnumerable<MergedRelation>> GetRelationsInScope<F>(IBulkRelationData<F> data, LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold)
        {
            var maskHandlingForRetrieval = MaskHandlingForRetrievalGetMasks.Instance;

            async Task<IEnumerable<MergedRelation>> GetOutdatedRelationsFromCIAndPredicateScope(BulkRelationDataCIAndPredicateScope cp, LayerSet layerIDs, IModelContext trans, TimeThreshold timeThreshold, IMaskHandlingForRetrieval maskHandlingForRetrieval)
            {
                if (cp.Relevant.Count == 0)
                    return Array.Empty<MergedRelation>();
                var dLookup = cp.Relevant.ToLookup(dd => dd.thisCIID, dd => dd.predicateID);
                var relationSelection = (cp.Outgoing) ? 
                    RelationSelectionFrom.Build(cp.Relevant.Select(dd => dd.predicateID).ToHashSet(), cp.Relevant.Select(dd => dd.thisCIID).ToHashSet()) : 
                    RelationSelectionTo.Build(cp.Relevant.Select(dd => dd.predicateID).ToHashSet(), cp.Relevant.Select(dd => dd.thisCIID).ToHashSet());
                var allRelations = await GetMergedRelations(relationSelection, layerIDs, trans, timeThreshold, maskHandlingForRetrieval, GeneratedDataHandlingExclude.Instance);
                var outdatedRelations = allRelations.Where(r => dLookup[(cp.Outgoing) ? r.Relation.FromCIID : r.Relation.ToCIID].Contains(r.Relation.PredicateID));
                return outdatedRelations;
            }

            async Task<IEnumerable<MergedRelation>> GetOutdatedRelationsFromSpecificScope(BulkRelationDataSpecificScope ss, LayerSet layerIDs, IModelContext trans, TimeThreshold timeThreshold, IMaskHandlingForRetrieval maskHandlingForRetrieval)
            {
                var specificRelations =
                    ss.Fragments.Select(f => (ss.GetFromCIID(f), ss.GetToCIID(f), ss.GetPredicateID(f)))
                    .Union(ss.Removals);
                return await GetMergedRelations(RelationSelectionSpecific.Build(specificRelations), layerIDs, trans, timeThreshold, maskHandlingForRetrieval, GeneratedDataHandlingExclude.Instance);
            }

            async Task<IEnumerable<MergedRelation>> GetOutdatedRelationsFromCIScope(BulkRelationDataCIScope cs, LayerSet layerIDs, IModelContext trans, TimeThreshold timeThreshold, IMaskHandlingForRetrieval maskHandlingForRetrieval)
            {
                var from = await GetMergedRelations(RelationSelectionFrom.BuildWithAllPredicateIDs(cs.RelevantCIIDs), layerIDs, trans, timeThreshold, maskHandlingForRetrieval, GeneratedDataHandlingExclude.Instance);
                var to = await GetMergedRelations(RelationSelectionTo.BuildWithAllPredicateIDs(cs.RelevantCIIDs), layerIDs, trans, timeThreshold, maskHandlingForRetrieval, GeneratedDataHandlingExclude.Instance);

                // HACK, TODO: with this approach, we need to filter out duplicates
                // if we had a RelationSelectionFromOrTo, we wouldn't need to do this
                var dict = new Dictionary<Guid, MergedRelation>();
                foreach (var f in from)
                    dict[f.Relation.ID] = f;
                foreach (var t in to)
                    dict[t.Relation.ID] = t;
                return dict.Values;
            }

            return data switch
            {
                BulkRelationDataPredicateScope p => await GetMergedRelations(RelationSelectionWithPredicate.Build(p.PredicateID), layerSet, trans, timeThreshold, maskHandlingForRetrieval, GeneratedDataHandlingExclude.Instance),
                BulkRelationDataCIScope p => await GetOutdatedRelationsFromCIScope(p, layerSet, trans, timeThreshold, maskHandlingForRetrieval),
                BulkRelationDataLayerScope _ => await GetMergedRelations(RelationSelectionAll.Instance, layerSet, trans, timeThreshold, maskHandlingForRetrieval, GeneratedDataHandlingExclude.Instance),
                BulkRelationDataCIAndPredicateScope cp => await GetOutdatedRelationsFromCIAndPredicateScope(cp, layerSet, trans, timeThreshold, maskHandlingForRetrieval),
                BulkRelationDataSpecificScope ss => await GetOutdatedRelationsFromSpecificScope(ss, layerSet, trans, timeThreshold, maskHandlingForRetrieval),
                _ => throw new Exception("Unknown scope")
            };
        }


        public async Task<int> BulkReplaceRelations<F>(IBulkRelationData<F> data, IChangesetProxy changesetProxy, IModelContext trans, IMaskHandlingForRemoval maskHandling, IOtherLayersValueHandling otherLayersValueHandling)
        {
            var (actualInserts, outdatedRelations) = await PrepareForBulkUpdate(data, trans, changesetProxy.TimeThreshold);

            var informationHashesToInsert = data.Fragments.Select(f => Relation.CreateInformationHash(data.GetFromCIID(f), data.GetToCIID(f), data.GetPredicateID(f))).ToHashSet();

            // mask - based changes to inserts and removals
            // depending on mask-handling, calculate relations that are potentially "maskable" in below layers
            var maskableRelationsInBelowLayers = new Dictionary<string, (Guid fromCIID, Guid toCIID, string predicateID)>();
            switch (maskHandling)
            {
                case MaskHandlingForRemovalApplyMaskIfNecessary n:
                    maskableRelationsInBelowLayers = (await GetRelationsInScope(data, new LayerSet(n.ReadLayersBelowWriteLayer), trans, changesetProxy.TimeThreshold))
                        .Select(r => r.Relation)
                        .GroupBy(t => t.InformationHash)
                        .Where(g => !informationHashesToInsert.Contains(g.Key)) // if we are already inserting this relation, we definitely do not want to mask it
                        .ToDictionary(r => r.Key, r => (r.First().FromCIID, r.First().ToCIID, r.First().PredicateID));
                    break;
                case MaskHandlingForRemovalApplyNoMask _:
                    // no operation necessary
                    break;
                default:
                    throw new Exception("Invalid mask handling");
            }
            // reduce the actual removes by looking at maskable relations, replacing the removes with masks if necessary
            foreach (var kv in maskableRelationsInBelowLayers)
            {
                var ih = kv.Key;

                if (outdatedRelations.TryGetValue(ih, out var outdatedRelation))
                {
                    // the attribute exists in the write-layer AND is actually outdated AND needs to be masked -> mask it, instead of removing it
                    outdatedRelations.Remove(ih);
                    actualInserts.Add((outdatedRelation.FromCIID, outdatedRelation.ToCIID, outdatedRelation.PredicateID, existingRelationID: outdatedRelation.ID, newRelationID: Guid.NewGuid(), mask: true));
                }
                else
                {
                    // the attribute exists only in the layers below -> mask it
                    actualInserts.Add((kv.Value.fromCIID, kv.Value.toCIID, kv.Value.predicateID, null, Guid.NewGuid(), true));
                }
            }

            var removes = outdatedRelations.Values.Select(t => (t.FromCIID, t.ToCIID, t.PredicateID, existingRelationID: t.ID, newRelationID: Guid.NewGuid(), mask: t.Mask)).ToList();

            // other-layers-value handling
            switch (otherLayersValueHandling)
            {
                case OtherLayersValueHandlingTakeIntoAccount t:
                    // fetch relations in layerset excluding write layer; if existing relation is same as relation that we want to write -> instead of write -> no-op or even delete
                    var existingRelationsInOtherLayers = (await GetRelationsInScope(data, new LayerSet(t.ReadLayersWithoutWriteLayer), trans, changesetProxy.TimeThreshold))
                        .Select(r => r.Relation)
                        .ToDictionary(r => r.InformationHash);
                    for (var i = actualInserts.Count - 1; i >= 0; i--)
                    {
                        var insert = actualInserts[i];
                        if (existingRelationsInOtherLayers.TryGetValue(Relation.CreateInformationHash(insert.fromCIID, insert.toCIID, insert.predicateID), out var r))
                        {
                            if (r.Mask == insert.mask) // mask must match, otherwise its not really the same relation
                            {
                                actualInserts.RemoveAt(i);

                                // in case there is a relation there already, we actually remove it because the other layers provide the same relation
                                if (insert.existingRelationID.HasValue)
                                {
                                    removes.Add((insert.fromCIID, insert.toCIID, insert.predicateID, insert.existingRelationID.Value, Guid.NewGuid(), insert.mask));
                                }
                            }
                        }
                    }
                    break;
                case OtherLayersValueHandlingForceWrite _:
                    // no operation necessary
                    break;
                default:
                    throw new Exception("Invalid other-layers-value handling");
            }


            // perform actual updates in bulk
            await baseModel.BulkUpdate(actualInserts, removes, data.LayerID, changesetProxy, trans);

            return actualInserts.Count + removes.Count;

        }
    }
}
