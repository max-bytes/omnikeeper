using Npgsql;
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
        Task<IEnumerable<Relation>> GetRelations(IRelationSelection rl, long layerID, IModelContext trans, TimeThreshold atTime);
        Task<Relation?> GetRelation(Guid fromCIID, Guid toCIID, string predicateID, long layerID, IModelContext trans, TimeThreshold atTime);

        // mutations
        Task<(Relation relation, bool changed)> RemoveRelation(Guid fromCIID, Guid toCIID, string predicateID, long layerID, IChangesetProxy changesetProxy, IModelContext trans);
        Task<(Relation relation, bool changed)> InsertRelation(Guid fromCIID, Guid toCIID, string predicateID, long layerID, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans);
        Task<IEnumerable<(Guid fromCIID, Guid toCIID, string predicateID, RelationState state)>> BulkReplaceRelations<F>(IBulkRelationData<F> data, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans);
    }
}
