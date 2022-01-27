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
            return await baseModel.BulkReplaceRelations(data, changesetProxy, origin, trans, maskHandling);
        }
    }
}
