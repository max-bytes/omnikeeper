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

        public async Task<bool> BulkReplaceRelations<F>(IBulkRelationData<F> data, IChangesetProxy changesetProxy, NpgsqlTransaction trans)
        {
            var success = await model.BulkReplaceRelations(data, changesetProxy, trans);
            if (success)
                foreach (var f in data.Fragments)
                {
                    memoryCache.CancelRelationsChangeToken(data.GetFromCIID(f), data.GetToCIID(f), data.LayerID);
                }
            return success;
        }

        public async Task<IEnumerable<MergedRelation>> GetMergedRelations(IRelationSelection rl, bool includeRemoved, LayerSet layerset, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            return await model.GetMergedRelations(rl, includeRemoved, layerset, trans, atTime);
        }

        public async Task<Relation> GetRelation(Guid fromCIID, Guid toCIID, string predicateID, long layerID, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            return await model.GetRelation(fromCIID, toCIID, predicateID, layerID, trans, atTime);
        }

        public async Task<IEnumerable<Relation>> GetRelations(IRelationSelection rl, bool includeRemoved, long layerID, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            return await model.GetRelations(rl, includeRemoved, layerID, trans, atTime);
        }

        public async Task<Relation> InsertRelation(Guid fromCIID, Guid toCIID, string predicateID, long layerID, IChangesetProxy changesetProxy, NpgsqlTransaction trans)
        {
            memoryCache.CancelRelationsChangeToken(fromCIID, toCIID, layerID);
            return await model.InsertRelation(fromCIID, toCIID, predicateID, layerID, changesetProxy, trans);
        }

        public async Task<Relation> RemoveRelation(Guid fromCIID, Guid toCIID, string predicateID, long layerID, IChangesetProxy changesetProxy, NpgsqlTransaction trans)
        {
            memoryCache.CancelRelationsChangeToken(fromCIID, toCIID, layerID);
            return await model.RemoveRelation(fromCIID, toCIID, predicateID, layerID, changesetProxy, trans);
        }
    }
}
