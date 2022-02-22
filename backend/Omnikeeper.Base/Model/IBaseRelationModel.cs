using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface IBaseRelationModel
    {
        Task<IEnumerable<Relation>[]> GetRelations(IRelationSelection rl, string[] layerIDs, IModelContext trans, TimeThreshold atTime);

        Task<IEnumerable<Relation>> GetRelationsOfChangeset(Guid changesetID, bool getRemoved, IModelContext trans);

        // mutations
        Task<(Relation relation, bool changed)> RemoveRelation(Guid fromCIID, Guid toCIID, string predicateID, string layerID, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans);
        Task<(Relation relation, bool changed)> InsertRelation(Guid fromCIID, Guid toCIID, string predicateID, bool mask, string layerID, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans);

        Task<(
            IList<(Guid fromCIID, Guid toCIID, string predicateID, Guid? existingRelationID, Guid newRelationID, bool mask)> inserts,
            IDictionary<string, Relation> outdatedRelations
            )> PrepareForBulkUpdate<F>(IBulkRelationData<F> data, IModelContext trans, TimeThreshold readTS);
        Task<(bool changed, Guid changesetID)> BulkUpdate(
            IList<(Guid fromCIID, Guid toCIID, string predicateID, Guid? existingRelationID, Guid newRelationID, bool mask)> inserts,
            IList<(Guid fromCIID, Guid toCIID, string predicateID, Guid existingRelationID, Guid newRelationID, bool mask)> removes,
            string layerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans);
    }

    public interface IBaseRelationRevisionistModel
    {
        Task<int> DeleteAllRelations(string layerID, IModelContext trans);
        Task<int> DeleteOutdatedRelationsOlderThan(string[] layerIDs, IModelContext trans, DateTimeOffset threshold, TimeThreshold atTime);
    }
}
