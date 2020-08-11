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
            return await model.CreateCI(trans, id);
        }

        public async Task<Guid> CreateCI(NpgsqlTransaction trans)
        {
            return await model.CreateCI(trans);
        }

        public async Task<bool> CIIDExists(Guid ciid, NpgsqlTransaction trans)
        {
            // TODO: caching
            return await model.CIIDExists(ciid, trans);
        }

        public async Task<CI> GetCI(Guid ciid, long layerID, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            return await model.GetCI(ciid, layerID, trans, atTime);
        }

        public async Task<IEnumerable<Guid>> GetCIIDs(NpgsqlTransaction trans)
        {
            return await model.GetCIIDs(trans);
        }

        public async Task<IEnumerable<Guid>> GetCIIDsOfNonEmptyCIs(LayerSet layerset, NpgsqlTransaction trans, TimeThreshold timeThreshold)
        {
            return await model.GetCIIDsOfNonEmptyCIs(layerset, trans, timeThreshold);
        }

        public async Task<IEnumerable<CI>> GetCIs(long layerID, ICIIDSelection selection, bool includeEmptyCIs, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            return await model.GetCIs(layerID, selection, includeEmptyCIs, trans, atTime);
        }

        public async Task<IEnumerable<CompactCI>> GetCompactCIs(LayerSet visibleLayers, ICIIDSelection selection, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            return await model.GetCompactCIs(visibleLayers, selection, trans, atTime);
        }

        public async Task<MergedCI> GetMergedCI(Guid ciid, LayerSet layers, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            return await model.GetMergedCI(ciid, layers, trans, atTime);
        }

        public async Task<IEnumerable<MergedCI>> GetMergedCIs(LayerSet layers, ICIIDSelection selection, bool includeEmptyCIs, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            return await model.GetMergedCIs(layers, selection, includeEmptyCIs, trans, atTime);
        }
    }
}
