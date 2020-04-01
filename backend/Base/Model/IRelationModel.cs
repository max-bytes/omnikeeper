using LandscapeRegistry;
using LandscapeRegistry.Entity;
using LandscapeRegistry.Model;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Landscape.Base.Model
{
    public interface IRelationModel
    {
        Task<IEnumerable<Relation>> GetRelationsWithPredicateID(LayerSet layerset, bool includeRemoved, string predicate, NpgsqlTransaction trans, DateTimeOffset? timeThreshold = null);

        Task<bool> BulkReplaceRelations(BulkRelationData data, long changesetID, NpgsqlTransaction trans);

        Task<Relation> RemoveRelation(string fromCIID, string toCIID, string predicateID, long layerID, long changesetID, NpgsqlTransaction trans);
        Task<Relation> InsertRelation(string fromCIID, string toCIID, string predicateID, long layerID, long changesetID, NpgsqlTransaction trans);
    }
}
