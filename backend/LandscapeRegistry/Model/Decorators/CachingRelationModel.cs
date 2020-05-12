using Landscape.Base.Entity;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using LandscapeRegistry.Service;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LandscapeRegistry.Model.Decorators
{
    public class CachingRelationModel : IRelationModel
    {
        private readonly IRelationModel model;
        private readonly IMemoryCache memoryCache;

        public CachingRelationModel(IRelationModel model, IMemoryCache memoryCache)
        {
            this.model = model;
            this.memoryCache = memoryCache;
        }

        public async Task<bool> BulkReplaceRelations<F>(IBulkRelationData<F> data, Changeset changeset, NpgsqlTransaction trans)
        {
            var success = await model.BulkReplaceRelations(data, changeset, trans);
            if (success)
                foreach (var f in data.Fragments)
                {
                    memoryCache.CancelCIChangeToken(data.GetFromCIID(f));
                    memoryCache.CancelCIChangeToken(data.GetToCIID(f));
                }
            return success;
        }

        public async Task<IEnumerable<Relation>> GetMergedRelations(Guid? ciid, bool includeRemoved, LayerSet layerset, IRelationModel.IncludeRelationDirections ird, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            return await model.GetMergedRelations(ciid, includeRemoved, layerset, ird, trans, atTime);
        }

        public async Task<IEnumerable<Relation>> GetMergedRelationsWithPredicateID(LayerSet layerset, bool includeRemoved, string predicate, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            return await model.GetMergedRelationsWithPredicateID(layerset, includeRemoved, predicate, trans, atTime);
        }

        public async Task<Relation> InsertRelation(Guid fromCIID, Guid toCIID, string predicateID, long layerID, Changeset changeset, NpgsqlTransaction trans)
        {
            memoryCache.CancelCIChangeToken(fromCIID);
            memoryCache.CancelCIChangeToken(toCIID);
            return await model.InsertRelation(fromCIID, toCIID, predicateID, layerID, changeset, trans);
        }

        public async Task<Relation> RemoveRelation(Guid fromCIID, Guid toCIID, string predicateID, long layerID, Changeset changeset, NpgsqlTransaction trans)
        {
            memoryCache.CancelCIChangeToken(fromCIID);
            memoryCache.CancelCIChangeToken(toCIID);
            return await model.RemoveRelation(fromCIID, toCIID, predicateID, layerID, changeset, trans);
        }
    }
}
