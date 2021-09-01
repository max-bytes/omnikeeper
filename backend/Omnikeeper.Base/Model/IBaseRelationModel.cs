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
        Task<IEnumerable<Relation>> GetRelations(IRelationSelection rl, string layerID, IModelContext trans, TimeThreshold atTime);
        Task<Relation?> GetRelation(Guid fromCIID, Guid toCIID, string predicateID, string layerID, IModelContext trans, TimeThreshold atTime);

        /**
        * NOTE: GetRelationsOfChangeset() can also return removed attributes
        */
        Task<IEnumerable<Relation>> GetRelationsOfChangeset(Guid changesetID, IModelContext trans);

        // mutations
        Task<(Relation relation, bool changed)> RemoveRelation(Guid fromCIID, Guid toCIID, string predicateID, string layerID, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans);
        Task<(Relation relation, bool changed)> InsertRelation(Guid fromCIID, Guid toCIID, string predicateID, string layerID, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans);
        Task<IEnumerable<(Guid fromCIID, Guid toCIID, string predicateID, RelationState state)>> BulkReplaceRelations<F>(IBulkRelationData<F> data, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans);
    }

    public interface IBaseRelationRevisionistModel
    {
        Task<int> DeleteAllRelations(string layerID, IModelContext trans);
        Task<int> DeleteOutdatedRelationsOlderThan(string layerID, IModelContext trans, DateTimeOffset threshold, TimeThreshold atTime);
    }
}
