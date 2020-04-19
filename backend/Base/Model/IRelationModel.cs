using Landscape.Base.Entity;
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

        Task<IEnumerable<Relation>> GetRelationsWithPredicateID(LayerSet layerset, bool includeRemoved, string predicate, NpgsqlTransaction trans, DateTimeOffset? timeThreshold = null);
        Task<IEnumerable<Relation>> GetMergedRelations(Guid ciid, bool includeRemoved, LayerSet layerset, IncludeRelationDirections ird, NpgsqlTransaction trans, DateTimeOffset? timeThreshold = null);


        Task<bool> BulkReplaceRelations(BulkRelationData data, long changesetID, NpgsqlTransaction trans);

        Task<Relation> RemoveRelation(Guid fromCIID, Guid toCIID, string predicateID, long layerID, long changesetID, NpgsqlTransaction trans);
        Task<Relation> InsertRelation(Guid fromCIID, Guid toCIID, string predicateID, long layerID, long changesetID, NpgsqlTransaction trans);
    }
}
