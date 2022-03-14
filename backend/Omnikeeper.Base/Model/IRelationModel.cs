using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface IRelationModel
    {
        Task<IEnumerable<MergedRelation>> GetMergedRelations(IRelationSelection rl, LayerSet layerset, IModelContext trans, TimeThreshold atTime, IMaskHandlingForRetrieval maskHandling, IGeneratedDataHandling generatedDataHandling);
        
        Task<IEnumerable<Relation>> GetRelationsOfChangeset(Guid changesetID, bool getRemoved, IModelContext trans);

        // mutations
        Task<bool> InsertRelation(Guid fromCIID, Guid toCIID, string predicateID, bool mask, string layerID, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans, IOtherLayersValueHandling otherLayersValueHandling);
        Task<bool> RemoveRelation(Guid fromCIID, Guid toCIID, string predicateID, string layerID, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans, IMaskHandlingForRemoval maskHandling);

        Task<int> BulkReplaceRelations<F>(IBulkRelationData<F> data, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans, IMaskHandlingForRemoval maskHandling, IOtherLayersValueHandling otherLayersValueHandling);
    }
}
