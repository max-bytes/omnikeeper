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

        private IEnumerable<MergedRelation> MergeRelations(IEnumerable<Relation>[] relations, string[] layerIDs, IMaskHandlingForRetrieval maskHandling)
        {
            var compound = new Dictionary<string, MergedRelation>();
            for (var i = 0; i < layerIDs.Length; i++)
            {
                var layerID = layerIDs[i];
                var rel = relations[i];

                foreach(var r in rel)
                {
                    if (compound.TryGetValue(r.InformationHash, out var existingRelation))
                    {
                        existingRelation.LayerStackIDs.Add(layerID);
                    } else
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

        public async Task<IEnumerable<MergedRelation>> GetMergedRelations(IRelationSelection rl, LayerSet layerset, IModelContext trans, TimeThreshold atTime, IMaskHandlingForRetrieval maskHandling)
        {
            if (layerset.IsEmpty)
                return ImmutableList<MergedRelation>.Empty; // return empty, an empty layer list can never produce any relations

            var lr = await baseModel.GetRelations(rl, layerset.LayerIDs, trans, atTime);


            return MergeRelations(lr, layerset.LayerIDs, maskHandling);
        }

        public async Task<IEnumerable<Relation>> GetRelationsOfChangeset(Guid changesetID, bool getRemoved, IModelContext trans)
        {
            return await baseModel.GetRelationsOfChangeset(changesetID, getRemoved, trans);
        }

        public async Task<(Relation relation, bool changed)> RemoveRelation(Guid fromCIID, Guid toCIID, string predicateID, string layerID, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans, IMaskHandlingForRemoval maskHandling)
        {
            return await baseModel.RemoveRelation(fromCIID, toCIID, predicateID, layerID, changesetProxy, origin, trans);
        }

        public async Task<(Relation relation, bool changed)> InsertRelation(Guid fromCIID, Guid toCIID, string predicateID, bool mask, string layerID, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        {
            return await baseModel.InsertRelation(fromCIID, toCIID, predicateID, mask, layerID, changesetProxy, origin, trans);
        }

        public async Task<IEnumerable<(Guid fromCIID, Guid toCIID, string predicateID)>> BulkReplaceRelations<F>(IBulkRelationData<F> data, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans, IMaskHandlingForRemoval maskHandling)
        {
            var (actualInserts, outdatedRelations) = await baseModel.PrepareForBulkUpdate(data, trans, changesetProxy.TimeThreshold);

            var informationHashesToInsert = data.Fragments.Select(f => Relation.CreateInformationHash(data.GetFromCIID(f), data.GetToCIID(f), data.GetPredicateID(f))).ToHashSet();

            // mask - based changes to inserts and removals
            // depending on mask-handling, calculate relations that are potentially "maskable" in below layers
            var maskableRelationsInBelowLayers = new Dictionary<string, (Guid fromCIID, Guid toCIID, string predicateID)>();
            switch (maskHandling)
            {
                case MaskHandlingForRemovalApplyMaskIfNecessary n:
                    maskableRelationsInBelowLayers = (data switch
                    {
                        BulkRelationDataPredicateScope p => (await baseModel.GetRelations(RelationSelectionWithPredicate.Build(p.PredicateID), n.ReadLayersBelowWriteLayer, trans, changesetProxy.TimeThreshold)),
                        BulkRelationDataLayerScope l => (await baseModel.GetRelations(RelationSelectionAll.Instance, n.ReadLayersBelowWriteLayer, trans, changesetProxy.TimeThreshold)),
                        BulkRelationDataCIAndPredicateScope cp => await cp.GetOutdatedRelationsFromCIAndPredicateScope(baseModel, n.ReadLayersBelowWriteLayer, trans, changesetProxy.TimeThreshold),
                        _ => throw new Exception("Unknown scope")
                    }).SelectMany(r => r)
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
                    actualInserts.Add((outdatedRelation.FromCIID, outdatedRelation.ToCIID, outdatedRelation.PredicateID, outdatedRelation.ID, Guid.NewGuid(), true));
                }
                else
                {
                    // the attribute exists only in the layers below -> mask it
                    actualInserts.Add((kv.Value.fromCIID, kv.Value.toCIID, kv.Value.predicateID, null, Guid.NewGuid(), true));
                }
            }


            // perform actual updates in bulk
            var removes = outdatedRelations.Values.Select(t => (t.FromCIID, t.ToCIID, t.PredicateID, t.ID, Guid.NewGuid(), t.Mask)).ToList();
            await baseModel.BulkUpdate(actualInserts, removes, data.LayerID, origin, changesetProxy, trans);

            // TODO: data (almost) is never used -> replace with a simpler return structure?
            return actualInserts.Select(r => (r.fromCIID, r.toCIID, r.predicateID))
                .Concat(outdatedRelations.Values.Select(r => (r.FromCIID, r.ToCIID, r.PredicateID)));

        }
    }
}
