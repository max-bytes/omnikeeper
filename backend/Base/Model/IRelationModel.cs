using Landscape.Base.Entity;
using Landscape.Base.Utils;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Landscape.Base.Model
{
    public interface IRelationModel
    {
        public enum IncludeRelationDirections
        {
            Forward, Backward, Both
        }

        Task<IEnumerable<Relation>> GetMergedRelationsWithPredicateID(LayerSet layerset, bool includeRemoved, string predicate, NpgsqlTransaction trans, TimeThreshold atTime);
        Task<IEnumerable<Relation>> GetMergedRelations(Guid? ciid, bool includeRemoved, LayerSet layerset, IncludeRelationDirections ird, NpgsqlTransaction trans, TimeThreshold atTime);


        Task<bool> BulkReplaceRelations<F>(IBulkRelationData<F> data, IChangesetProxy changesetProxy, NpgsqlTransaction trans);
        Task<Relation> RemoveRelation(Guid fromCIID, Guid toCIID, string predicateID, long layerID, IChangesetProxy changesetProxy, NpgsqlTransaction trans);
        Task<Relation> InsertRelation(Guid fromCIID, Guid toCIID, string predicateID, long layerID, IChangesetProxy changesetProxy, NpgsqlTransaction trans);
    }
}
