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

        public async Task<IEnumerable<CIAttribute>> FindAttributesByName(string regex, ICIIDSelection selection, long layerID, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            // TODO: caching?
            return await model.FindAttributesByName(regex, selection, layerID, trans, atTime);
        }

        public async Task<IEnumerable<CIAttribute>> FindAttributesByFullName(string name, ICIIDSelection selection, long layerID, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            // TODO: caching?
            return await model.FindAttributesByFullName(name, selection, layerID, trans, atTime);
        }

        public async Task<CIAttribute> GetAttribute(string name, Guid ciid, long layerID, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            if (atTime.IsLatest)
            {
                // instead of doing single-attribute caching, we cache all attributes of this ci in a list and pick the fitting one based on name afterwards
                var attributes = await memoryCache.GetOrCreateAsync(CacheKeyService.Attributes(ciid, layerID), async (ce) =>
                {
                    var changeToken = memoryCache.GetAttributesCancellationChangeToken(ciid, layerID);
                    ce.AddExpirationToken(changeToken);
                    return await model.GetAttributes(SpecificCIIDsSelection.Build(ciid), layerID, trans, atTime);
                });

                attributes.FirstOrDefault(p => p.Name.Equals(name));
            }
            return await model.GetAttribute(name, ciid, layerID, trans, atTime);
        }

        public async Task<IEnumerable<CIAttribute>> GetAttributes(ICIIDSelection selection, long layerID, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            if (atTime.IsLatest)
            {
                switch (selection)
                {
                    case SpecificCIIDsSelection mcs:
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
                            }

                            // get the non-cached items
                            if (notFoundCIIDs.Count > 0)
                            {
                                var fetched = (await model.GetAttributes(SpecificCIIDsSelection.Build(notFoundCIIDs), layerID, trans, atTime));

                                // add them to the cache
                                foreach (var a in fetched.ToLookup(a => a.CIID))
                                    memoryCache.Set(CacheKeyService.Attributes(a.Key, layerID), a.ToList(), memoryCache.GetAttributesCancellationChangeToken(a.Key, layerID));

                                // NOTE: a CI containing NO attributes does not return any (obv), and so would also not get a cache entry (with an empty list as value)
                                // meaning it would NEVER get cached and retrieved all the time instead...
                                // to counter that, we check for empty CIs and insert an empty list for those
                                var emptyCIs = notFoundCIIDs.Except(fetched.Select(k => k.CIID).Distinct());
                                foreach (var ciid in emptyCIs)
                                    memoryCache.Set(CacheKeyService.Attributes(ciid, layerID), new List<CIAttribute>(), memoryCache.GetAttributesCancellationChangeToken(ciid, layerID));

                                found.AddRange(fetched);
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

        public async Task<(CIAttribute attribute, bool changed)> InsertAttribute(string name, IAttributeValue value, Guid ciid, long layerID, IChangesetProxy changesetProxy, NpgsqlTransaction trans)
        {
            var t = await model.InsertAttribute(name, value, ciid, layerID, changesetProxy, trans);
            if (t.changed)
                memoryCache.CancelAttributesChangeToken(ciid, layerID);
            return t;
        }

        public async Task<(CIAttribute attribute, bool changed)> InsertCINameAttribute(string nameValue, Guid ciid, long layerID, IChangesetProxy changesetProxy, NpgsqlTransaction trans)
        {
            var t = await model.InsertCINameAttribute(nameValue, ciid, layerID, changesetProxy, trans);
            if (t.changed)
                memoryCache.CancelAttributesChangeToken(ciid, layerID);
            return t;
        }

        public async Task<(CIAttribute attribute, bool changed)> RemoveAttribute(string name, Guid ciid, long layerID, IChangesetProxy changesetProxy, NpgsqlTransaction trans)
        {
            var t = await model.RemoveAttribute(name, ciid, layerID, changesetProxy, trans);
            if (t.changed)
                memoryCache.CancelAttributesChangeToken(ciid, layerID);
            return t;
        }

        public async Task<IEnumerable<(Guid ciid, string fullName, IAttributeValue value, AttributeState state)>> BulkReplaceAttributes<F>(IBulkCIAttributeData<F> data, IChangesetProxy changesetProxy, NpgsqlTransaction trans)
        {
            var inserted = await model.BulkReplaceAttributes(data, changesetProxy, trans);
            foreach (var (ciid, _, _, _) in inserted) memoryCache.CancelAttributesChangeToken(ciid, data.LayerID); // NOTE: inserted list is not distinct on ciids, but that's ok
            return inserted;
        }

        public async Task<int> ArchiveOutdatedAttributesOlderThan(DateTimeOffset threshold, long layerID, NpgsqlTransaction trans)
        {
            // NOTE: this method SHOULD NOT have any effect on caching, because we only cache the latest timestamp anyways
            return await model.ArchiveOutdatedAttributesOlderThan(threshold, layerID, trans);
        }
    }
}
