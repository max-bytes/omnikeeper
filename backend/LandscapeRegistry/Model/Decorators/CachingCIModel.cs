using GraphQL.Language.AST;
using Landscape.Base.Entity;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using LandscapeRegistry.Entity.AttributeValues;
using LandscapeRegistry.Service;
using Microsoft.Extensions.Caching.Memory;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapeRegistry.Model.Decorators
{
    public class CachingCIModel : ICIModel
    {
        private readonly ICIModel model;
        private readonly IMemoryCache memoryCache;

        public CachingCIModel(ICIModel model, IMemoryCache memoryCache)
        {
            this.model = model;
            this.memoryCache = memoryCache;
        }

        public async Task<Guid> CreateCI(NpgsqlTransaction trans, Guid id)
        {
            // we assume there is no cache entry for a ci that gets created
            return await model.CreateCI(trans, id);
        }

        public async Task<Guid> CreateCI(NpgsqlTransaction trans)
        {
            // we assume there is no cache entry for a ci that gets created
            return await model.CreateCI(trans);
        }

        public async Task<Guid> CreateCIWithType(string typeID, NpgsqlTransaction trans)
        {
            // we assume there is no cache entry for a ci that gets created
            return await model.CreateCIWithType(typeID, trans);
        }

        public async Task<CI> GetCI(Guid ciid, long layerID, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            if (atTime.IsLatest)
            {
                return await memoryCache.GetOrCreateAsync(CacheKeyService.CIOnLayer(ciid, layerID), async (ce) =>
                {
                    var changeToken = memoryCache.GetCICancellationChangeToken(ciid);
                    ce.AddExpirationToken(changeToken);
                    return await model.GetCI(ciid, layerID, trans, atTime);
                });
            }
            else return await model.GetCI(ciid, layerID, trans, atTime);
        }

        public async Task<IEnumerable<Guid>> GetCIIDs(NpgsqlTransaction trans)
        {
            // cannot be cached well... or can it?
            return await model.GetCIIDs(trans);
        }

        public async Task<IEnumerable<CI>> GetCIs(long layerID, bool includeEmptyCIs, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            // cannot be cached well... or can it?
            return await model.GetCIs(layerID, includeEmptyCIs, trans, atTime);
        }

        public async Task<CIType> UpsertCIType(string typeID, AnchorState state, NpgsqlTransaction trans)
        {
            // TODO: this would actually need to remove ALL CIs in cache, but we just ignore it for now because CITypes are supposed to be removed anyway
            return await model.UpsertCIType(typeID, state, trans);
        }

        public async Task<IEnumerable<CIType>> GetCITypes(NpgsqlTransaction trans, TimeThreshold atTime)
        {
            // we don't cache it because we hope to get rid of it soon anyway
            return await model.GetCITypes(trans, atTime);
        }

        public async Task<IEnumerable<CompactCI>> GetCompactCIs(LayerSet visibleLayers, NpgsqlTransaction trans, TimeThreshold atTime, IEnumerable<Guid> CIIDs = null)
        {
            // cannot be cached well... or can it?
            return await model.GetCompactCIs(visibleLayers, trans, atTime, CIIDs);
        }

        public async Task<MergedCI> GetMergedCI(Guid ciid, LayerSet layers, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            if (atTime.IsLatest)
            {
                return await memoryCache.GetOrCreateAsync(CacheKeyService.MergedCI(ciid, layers), async (ce) =>
                {
                    var changeToken = memoryCache.GetCICancellationChangeToken(ciid);
                    ce.AddExpirationToken(changeToken);
                    return await model.GetMergedCI(ciid, layers, trans, atTime);
                });
            }
            else return await model.GetMergedCI(ciid, layers, trans, atTime);
        }

        public async Task<IEnumerable<MergedCI>> GetMergedCIs(LayerSet layers, bool includeEmptyCIs, NpgsqlTransaction trans, TimeThreshold atTime, IEnumerable<Guid> CIIDs)
        {
            // check which item can be found in the cache
            var found = new List<MergedCI>();
            var notFound = new List<Guid>();
            foreach(var ciid in CIIDs)
            {
                if (memoryCache.TryGetValue<MergedCI>(CacheKeyService.MergedCI(ciid, layers), out var ci))
                    found.Add(ci);
                else 
                    notFound.Add(ciid);
            }
            // get the non-cached items
            var fetched = await model.GetMergedCIs(layers, includeEmptyCIs, trans, atTime, notFound);
            // add them to the cache
            foreach (var ci in fetched) memoryCache.Set(CacheKeyService.MergedCI(ci.ID, layers), ci, memoryCache.GetCICancellationChangeToken(ci.ID));

            return found.Concat(fetched);
        }
    }
}
