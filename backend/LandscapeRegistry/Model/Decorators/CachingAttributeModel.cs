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
    public class CachingAttributeModel : IAttributeModel
    {
        private readonly IAttributeModel model;
        private readonly IMemoryCache memoryCache;

        public CachingAttributeModel(IAttributeModel model, IMemoryCache memoryCache)
        {
            this.model = model;
            this.memoryCache = memoryCache;
        }

        public async Task<IEnumerable<CIAttribute>> FindAttributesByName(string like, bool includeRemoved, long layerID, NpgsqlTransaction trans, TimeThreshold atTime, Guid? ciid = null)
        {
            return await model.FindAttributesByName(like, includeRemoved, layerID, trans, atTime, ciid);
        }

        public async Task<IDictionary<Guid, MergedCIAttribute>> FindMergedAttributesByFullName(string name, IAttributeModel.IAttributeSelection selection, bool includeRemoved, LayerSet layers, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            return await model.FindMergedAttributesByFullName(name, selection, includeRemoved, layers, trans, atTime);
        }

        public async Task<CIAttribute> GetAttribute(string name, long layerID, Guid ciid, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            return await model.GetAttribute(name, layerID, ciid, trans, atTime);
        }

        public async Task<IEnumerable<CIAttribute>> GetAttributes(IAttributeModel.IAttributeSelection selection, bool includeRemoved, long layerID, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            return await model.GetAttributes(selection, includeRemoved, layerID, trans, atTime);
        }

        public async Task<IDictionary<string, MergedCIAttribute>> GetMergedAttributes(Guid ciid, bool includeRemoved, LayerSet layers, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            return await model.GetMergedAttributes(ciid, includeRemoved, layers, trans, atTime);
        }

        public async Task<IDictionary<Guid, IDictionary<string, MergedCIAttribute>>> GetMergedAttributes(IEnumerable<Guid> ciids, bool includeRemoved, LayerSet layers, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            return await model.GetMergedAttributes(ciids, includeRemoved, layers, trans, atTime);
        }

        public async Task<CIAttribute> InsertAttribute(string name, IAttributeValue value, long layerID, Guid ciid, IChangesetProxy changesetProxy, NpgsqlTransaction trans)
        {
            memoryCache.CancelCIChangeToken(ciid);
            return await model.InsertAttribute(name, value, layerID, ciid, changesetProxy, trans);
        }

        public async Task<CIAttribute> InsertCINameAttribute(string nameValue, long layerID, Guid ciid, IChangesetProxy changesetProxy, NpgsqlTransaction trans)
        {
            memoryCache.CancelCIChangeToken(ciid);
            return await model.InsertCINameAttribute(nameValue, layerID, ciid, changesetProxy, trans);
        }

        public async Task<CIAttribute> RemoveAttribute(string name, long layerID, Guid ciid, IChangesetProxy changesetProxy, NpgsqlTransaction trans)
        {
            memoryCache.CancelCIChangeToken(ciid);
            return await model.RemoveAttribute(name, layerID, ciid, changesetProxy, trans);
        }

        public async Task<bool> BulkReplaceAttributes<F>(IBulkCIAttributeData<F> data, IChangesetProxy changesetProxy, NpgsqlTransaction trans)
        {
            var success = await model.BulkReplaceAttributes(data, changesetProxy, trans);
            if (success)
                foreach (var f in data.Fragments) memoryCache.CancelCIChangeToken(data.GetCIID(f));
            return success;
        }

    }
}
