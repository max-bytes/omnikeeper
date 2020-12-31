using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Model.Decorators
{
    public class CachingBaseAttributeModel : IBaseAttributeModel
    {
        private readonly IBaseAttributeModel model;
        private readonly ILogger<CachingBaseAttributeModel> logger;

        public CachingBaseAttributeModel(IBaseAttributeModel model, ILogger<CachingBaseAttributeModel> logger)
        {
            this.model = model;
            this.logger = logger;
        }

        public async Task<IEnumerable<CIAttribute>> FindAttributesByName(string regex, ICIIDSelection selection, long layerID, IModelContext trans, TimeThreshold atTime)
        {
            // TODO: caching?
            logger.LogTrace("Cache Nope - FindAttributesByName");
            return await model.FindAttributesByName(regex, selection, layerID, trans, atTime);
        }

        public async Task<IEnumerable<CIAttribute>> FindAttributesByFullName(string name, ICIIDSelection selection, long layerID, IModelContext trans, TimeThreshold atTime)
        {
            // TODO: caching?
            logger.LogTrace("Cache Nope - FindAttributesByFullName");
            return await model.FindAttributesByFullName(name, selection, layerID, trans, atTime);
        }

        public async Task<IEnumerable<Guid>> FindCIIDsWithAttribute(string name, ICIIDSelection selection, long layerID, IModelContext trans, TimeThreshold atTime)
        {
            if (atTime.IsLatest)
            {
                var (ciids, hit) = await trans.GetOrCreateCachedValueAsync(CacheKeyService.CIIDsWithAttributeName(name, layerID), async () =>
                {
                    return await model.FindCIIDsWithAttribute(name, new AllCIIDsSelection(), layerID, trans, atTime);
                });

                if (hit)
                    logger.LogTrace("Cache Hit  - FindCIIDsWithAttribute");
                else
                    logger.LogTrace("Cache Miss - FindCIIDsWithAttribute");

                return ciids.Where(ciid => selection.Contains(ciid));
            }

            logger.LogTrace("Cache Nope - FindCIIDsWithAttribute");
            return await model.FindCIIDsWithAttribute(name, selection, layerID, trans, atTime);
        }

        public async Task<CIAttribute?> GetAttribute(string name, Guid ciid, long layerID, IModelContext trans, TimeThreshold atTime)
        {
            if (atTime.IsLatest)
            {
                // instead of doing single-attribute caching, we cache all attributes of this ci in a list and pick the fitting one based on name afterwards
                var (attributes, hit) = await trans.GetOrCreateCachedValueAsync(CacheKeyService.Attributes(ciid, layerID), async () =>
                {
                    return await model.GetAttributes(SpecificCIIDsSelection.Build(ciid), layerID, trans, atTime);
                });

                if (hit)
                    logger.LogTrace("Cache Hit  - GetAttribute");
                else
                    logger.LogTrace("Cache Miss - GetAttribute");

                attributes.FirstOrDefault(p => p.Name.Equals(name));
            }
            logger.LogTrace("Cache Nope - GetAttribute");
            return await model.GetAttribute(name, ciid, layerID, trans, atTime);
        }

        public async Task<CIAttribute?> GetFullBinaryAttribute(string name, Guid ciid, long layerID, IModelContext trans, TimeThreshold atTime)
        {
            // no caching for binary attributes
            logger.LogTrace("Cache Nope - GetFullBinaryAttribute");
            return await model.GetFullBinaryAttribute(name, ciid, layerID, trans, atTime);
        }

        public async Task<IEnumerable<CIAttribute>> GetAttributes(ICIIDSelection selection, long layerID, IModelContext trans, TimeThreshold atTime)
        {
            // NOTE: caching is not even faster in a lot of circumstances, so we don't actually cache (for now)
            // this might change in the future, which is why the code stays
            var cachingEnabled = false;

            if (cachingEnabled && atTime.IsLatest)
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
                                if (trans.TryGetCachedValue<IEnumerable<CIAttribute>>(CacheKeyService.Attributes(ciid, layerID), out var attributesOfCI))
                                {
                                    found.AddRange(attributesOfCI!);
                                }
                                else
                                    notFoundCIIDs.Add(ciid);
                            }

                            // get the non-cached items
                            if (notFoundCIIDs.Count > 0)
                            {
                                if (found.Count == 0)
                                    logger.LogTrace("Cache Nope - GetAttributes");
                                else
                                    logger.LogTrace("Cache Partial - GetAttributes");

                                var fetched = (await model.GetAttributes(SpecificCIIDsSelection.Build(notFoundCIIDs), layerID, trans, atTime));

                                // add them to the cache
                                foreach (var a in fetched.ToLookup(a => a.CIID))
                                    trans.SetCacheValue(CacheKeyService.Attributes(a.Key, layerID), a.ToList());

                                // NOTE: a CI containing NO attributes does not return any (obv), and so would also not get a cache entry (with an empty list as value)
                                // meaning it would NEVER get cached and retrieved all the time instead...
                                // to counter that, we check for empty CIs and insert an empty list for those
                                var emptyCIs = notFoundCIIDs.Except(fetched.Select(k => k.CIID).Distinct());
                                foreach (var ciid in emptyCIs)
                                    trans.SetCacheValue(CacheKeyService.Attributes(ciid, layerID), new List<CIAttribute>());

                                found.AddRange(fetched);
                            }
                            else
                            {
                                logger.LogTrace("Cache Hit - GetAttributes");
                            }
                            return found;
                        }
                    case AllCIIDsSelection acs:
                        // NOTE: caching seems slower for this path, so we avoid it entirely
                        logger.LogTrace("Cache Nope - GetAttributes");
                        return await model.GetAttributes(acs, layerID, trans, atTime);
                    default:
                        throw new Exception("Invalid CIIDSelection");
                }
            }
            else
            {
                logger.LogTrace("Cache Nope - GetAttributes");
                return await model.GetAttributes(selection, layerID, trans, atTime);
            }
        }

        public async Task<(CIAttribute attribute, bool changed)> InsertAttribute(string name, IAttributeValue value, Guid ciid, long layerID, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        {
            var t = await model.InsertAttribute(name, value, ciid, layerID, changesetProxy, origin, trans);
            if (t.changed)
            {
                trans.EvictFromCache(CacheKeyService.Attributes(ciid, layerID));
                trans.EvictFromCache(CacheKeyService.CIIDsWithAttributeName(name, layerID));
            }
            return t;
        }

        public async Task<(CIAttribute attribute, bool changed)> InsertCINameAttribute(string nameValue, Guid ciid, long layerID, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        {
            var t = await model.InsertCINameAttribute(nameValue, ciid, layerID, changesetProxy, origin, trans);
            if (t.changed)
            {
                trans.EvictFromCache(CacheKeyService.Attributes(ciid, layerID));
                trans.EvictFromCache(CacheKeyService.CIIDsWithAttributeName(ICIModel.NameAttribute, layerID));
            }
            return t;
        }

        public async Task<(CIAttribute attribute, bool changed)> RemoveAttribute(string name, Guid ciid, long layerID, IChangesetProxy changesetProxy, IModelContext trans)
        {
            var t = await model.RemoveAttribute(name, ciid, layerID, changesetProxy, trans);
            if (t.changed)
            {
                trans.EvictFromCache(CacheKeyService.Attributes(ciid, layerID));
                trans.EvictFromCache(CacheKeyService.CIIDsWithAttributeName(name, layerID));
            }
            return t;
        }

        public async Task<IEnumerable<(Guid ciid, string fullName, IAttributeValue value, AttributeState state)>> BulkReplaceAttributes<F>(IBulkCIAttributeData<F> data, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        {
            var inserted = await model.BulkReplaceAttributes(data, changesetProxy, origin, trans);
            foreach (var (ciid, fullName, _, _) in inserted)
            {
                trans.EvictFromCache(CacheKeyService.Attributes(ciid, data.LayerID)); // NOTE: inserted list is not distinct on ciids, but that's ok
                trans.EvictFromCache(CacheKeyService.CIIDsWithAttributeName(fullName, data.LayerID)); // NOTE: inserted list is not distinct on attribute names, but that's ok
            }
            return inserted;
        }
    }
}
