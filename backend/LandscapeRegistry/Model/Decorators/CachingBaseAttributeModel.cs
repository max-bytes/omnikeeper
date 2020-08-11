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
    public class CachingBaseAttributeModel : IBaseAttributeModel
    {
        private readonly IBaseAttributeModel model;
        private readonly IMemoryCache memoryCache;

        public CachingBaseAttributeModel(IBaseAttributeModel model, IMemoryCache memoryCache)
        {
            this.model = model;
            this.memoryCache = memoryCache;
        }

        public async Task<IEnumerable<CIAttribute>> FindAttributesByName(string like, ICIIDSelection selection, long layerID, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            // TODO: caching?
            return await model.FindAttributesByName(like, selection, layerID, trans, atTime);
        }

        public async Task<IEnumerable<CIAttribute>> FindAttributesByFullName(string name, ICIIDSelection selection, long layerID, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            // TODO: caching?
            return await model.FindAttributesByFullName(name, selection, layerID, trans, atTime);
        }

        public async Task<CIAttribute> GetAttribute(string name, long layerID, Guid ciid, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            if (atTime.IsLatest)
            {
                // instead of doing single-attribute caching, we cache all attributes of this ci in a list and pick the fitting one based on name afterwards
                var attributes = await memoryCache.GetOrCreateAsync(CacheKeyService.Attributes(ciid, layerID), async (ce) =>
                {
                    var changeToken = memoryCache.GetAttributesCancellationChangeToken(ciid, layerID);
                    ce.AddExpirationToken(changeToken);
                    return await model.GetAttributes(new SingleCIIDSelection(ciid), layerID, trans, atTime);
                });

                attributes.FirstOrDefault(p => p.Name.Equals(name));
            }
            return await model.GetAttribute(name, layerID, ciid, trans, atTime);
        }

        public async Task<IEnumerable<CIAttribute>> GetAttributes(ICIIDSelection selection, long layerID, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            if (atTime.IsLatest)
            {
                switch (selection)
                {
                    case SingleCIIDSelection scs:
                    {
                        var attributes = await memoryCache.GetOrCreateAsync(CacheKeyService.Attributes(scs.CIID, layerID), async (ce) =>
                        {
                            var changeToken = memoryCache.GetAttributesCancellationChangeToken(scs.CIID, layerID);
                            ce.AddExpirationToken(changeToken);
                            return await model.GetAttributes(scs, layerID, trans, atTime);
                        });
                        return attributes;
                    }
                    case MultiCIIDsSelection mcs:
                    {
                        // check which item can be found in the cache
                        var found = new List<CIAttribute>();
                        var notFoundCIIDs = new List<Guid>();
                        foreach (var ciid in mcs.CIIDs)
                        {
                            if (memoryCache.TryGetValue<IEnumerable<CIAttribute>>(CacheKeyService.Attributes(ciid, layerID), out var attributesOfCI))
                            {
                                found.AddRange(attributesOfCI);
                            }
                            else
                                notFoundCIIDs.Add(ciid);

                            // get the non-cached items
                            if (notFoundCIIDs.Count > 0)
                            {
                                var fetched = await model.GetAttributes(MultiCIIDsSelection.Build(notFoundCIIDs), layerID, trans, atTime);
                                // add them to the cache
                                foreach (var a in fetched.ToLookup(a => a.CIID))
                                    memoryCache.Set(CacheKeyService.Attributes(a.Key, layerID), a.ToList(), memoryCache.GetAttributesCancellationChangeToken(a.Key, layerID));

                                found.AddRange(fetched);
                            }
                        }
                        return found;
                    }
                    case AllCIIDsSelection acs:
                        // TODO: caching(?)
                        return await model.GetAttributes(acs, layerID, trans, atTime);
                    default:
                        throw new Exception("Invalid CIIDSelection");
                }
            }
            else
                return await model.GetAttributes(selection, layerID, trans, atTime);
        }

        public async Task<CIAttribute> InsertAttribute(string name, IAttributeValue value, long layerID, Guid ciid, IChangesetProxy changesetProxy, NpgsqlTransaction trans)
        {
            memoryCache.CancelAttributesChangeToken(ciid, layerID);
            return await model.InsertAttribute(name, value, layerID, ciid, changesetProxy, trans);
        }

        public async Task<CIAttribute> InsertCINameAttribute(string nameValue, long layerID, Guid ciid, IChangesetProxy changesetProxy, NpgsqlTransaction trans)
        {
            memoryCache.CancelAttributesChangeToken(ciid, layerID);
            return await model.InsertCINameAttribute(nameValue, layerID, ciid, changesetProxy, trans);
        }

        public async Task<CIAttribute> RemoveAttribute(string name, long layerID, Guid ciid, IChangesetProxy changesetProxy, NpgsqlTransaction trans)
        {
            memoryCache.CancelAttributesChangeToken(ciid, layerID);
            return await model.RemoveAttribute(name, layerID, ciid, changesetProxy, trans);
        }

        public async Task<bool> BulkReplaceAttributes<F>(IBulkCIAttributeData<F> data, IChangesetProxy changesetProxy, NpgsqlTransaction trans)
        {
            var success = await model.BulkReplaceAttributes(data, changesetProxy, trans);
            if (success)
                foreach (var f in data.Fragments) memoryCache.CancelAttributesChangeToken(data.GetCIID(f), data.LayerID);
            return success;
        }
    }
}
