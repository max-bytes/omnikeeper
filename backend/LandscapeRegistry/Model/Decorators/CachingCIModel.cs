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
            return await model.CreateCI(trans, id);
        }

        public async Task<Guid> CreateCI(NpgsqlTransaction trans)
        {
            return await model.CreateCI(trans);
        }

        public async Task<Guid> CreateCIWithType(string typeID, NpgsqlTransaction trans)
        {
            return await model.CreateCIWithType(typeID, trans);
        }

        public async Task<CI> GetCI(Guid ciid, long layerID, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            return await model.GetCI(ciid, layerID, trans, atTime);
        }

        public async Task<IEnumerable<Guid>> GetCIIDs(NpgsqlTransaction trans)
        {
            return await model.GetCIIDs(trans);
        }

        public async Task<IDictionary<Guid, string>> GetCINames(IEnumerable<Guid> ciids, LayerSet layerset, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            return await model.GetCINames(ciids, layerset, trans, atTime);
        }

        public async Task<IEnumerable<CI>> GetCIs(long layerID, bool includeEmptyCIs, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            return await model.GetCIs(layerID, includeEmptyCIs, trans, atTime);
        }

        public async Task<CIType> GetCITypeByID(string typeID, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            return await model.GetCITypeByID(typeID, trans, atTime);
        }

        public async Task<CIType> UpsertCIType(string typeID, AnchorState state, NpgsqlTransaction trans)
        {
            // TODO: evict CITypes, once implemented
            return await model.UpsertCIType(typeID, state, trans);
        }

        public async Task<IEnumerable<CIType>> GetCITypes(NpgsqlTransaction trans, TimeThreshold atTime)
        {
            return await model.GetCITypes(trans, atTime);
        }

        public async Task<IEnumerable<CompactCI>> GetCompactCIs(LayerSet visibleLayers, NpgsqlTransaction trans, TimeThreshold atTime, IEnumerable<Guid> CIIDs = null)
        {
            return await model.GetCompactCIs(visibleLayers, trans, atTime, CIIDs);
        }

        public async Task<MergedCI> GetMergedCI(Guid ciid, LayerSet layers, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            return await model.GetMergedCI(ciid, layers, trans, atTime);
        }

        public async Task<IEnumerable<MergedCI>> GetMergedCIs(LayerSet layers, bool includeEmptyCIs, NpgsqlTransaction trans, TimeThreshold atTime, IEnumerable<Guid> CIIDs = null)
        {
            return await model.GetMergedCIs(layers, includeEmptyCIs, trans, atTime, CIIDs);
        }

        public async Task<IEnumerable<MergedCI>> GetMergedCIsByType(LayerSet layers, NpgsqlTransaction trans, TimeThreshold atTime, string typeID)
        {
            return await model.GetMergedCIsByType(layers, trans, atTime, typeID);
        }

        public async Task<IEnumerable<MergedCI>> GetMergedCIsByType(LayerSet layers, NpgsqlTransaction trans, TimeThreshold atTime, IEnumerable<string> typeIDs)
        {
            return await model.GetMergedCIsByType(layers, trans, atTime, typeIDs);
        }

        public async Task<CIType> GetTypeOfCI(Guid ciid, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            return await model.GetTypeOfCI(ciid, trans, atTime);
        }
    }
}
