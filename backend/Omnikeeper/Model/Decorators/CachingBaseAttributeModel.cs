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
        private readonly ICIIDModel ciidModel;
        private readonly ILogger<CachingBaseAttributeModel> logger;

        public CachingBaseAttributeModel(IBaseAttributeModel model, ICIIDModel ciidModel, ILogger<CachingBaseAttributeModel> logger)
        {
            this.model = model;
            this.ciidModel = ciidModel;
            this.logger = logger;
        }

        public async Task<IEnumerable<CIAttribute>> FindAttributesByName(string regex, ICIIDSelection selection, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            // TODO: caching?
            logger.LogTrace("Cache Nope - FindAttributesByName");
            return await model.FindAttributesByName(regex, selection, layerID, trans, atTime);
        }

        public async Task<IDictionary<Guid, string>> GetCINames(ICIIDSelection selection, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            if (atTime.IsLatest)
            {
                var (names, hit) = await trans.GetOrCreateCachedValueAsync(CacheKeyService.CINames(layerID), async () =>
                {
                    return await model.GetCINames(new AllCIIDsSelection(), layerID, trans, atTime);
                });

                switch (selection)
                {
                    case SpecificCIIDsSelection ss:
                        names = names.Where(kv => ss.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);
                        break;
                    case AllCIIDsSelection _:
                        break;
                    case AllCIIDsExceptSelection es:
                        names = names.Where(kv => es.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);
                        break;
                    case NoCIIDsSelection _:
                        names = new Dictionary<Guid, string?>();
                        break;
                    default:
                        throw new Exception("Unknown ciid selection encountered");
                }

                return names;
            }
            else
            {
                logger.LogTrace("Cache Nope - GetCINames");
                return await model.GetCINames(selection, layerID, trans, atTime);
            }
        }

        public async Task<IEnumerable<CIAttribute>> FindAttributesByFullName(string name, ICIIDSelection selection, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            // TODO: caching?
            logger.LogTrace("Cache Nope - FindAttributesByFullName");
            return await model.FindAttributesByFullName(name, selection, layerID, trans, atTime);
        }

        public async Task<IEnumerable<Guid>> FindCIIDsWithAttribute(string name, ICIIDSelection selection, string layerID, IModelContext trans, TimeThreshold atTime)
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

        public async Task<CIAttribute?> GetAttribute(string name, Guid ciid, string layerID, IModelContext trans, TimeThreshold atTime)
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

        public async Task<CIAttribute?> GetFullBinaryAttribute(string name, Guid ciid, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            // no caching for binary attributes
            logger.LogTrace("Cache Nope - GetFullBinaryAttribute");
            return await model.GetFullBinaryAttribute(name, ciid, layerID, trans, atTime);
        }

        // NOTE: caching is not even faster in a lot of circumstances, so we still consider if we should cache at all
        // this might change in the future, which is why the code stays like this for now
        // you can change this member mid-run to enable performance testing
        public bool CachingEnabledForGetAttributes = true;

        public async Task<IEnumerable<CIAttribute>> GetAttributes(ICIIDSelection selection, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            if (CachingEnabledForGetAttributes && atTime.IsLatest)
            {
                var neededCIIDs = await selection.GetCIIDsAsync(async () => await ciidModel.GetCIIDs(trans));

                // check which items can be found in the cache
                var found = new List<CIAttribute>();
                var notFoundCIIDs = new HashSet<Guid>();
                foreach (var ciid in neededCIIDs)
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
            else
            {
                logger.LogTrace("Cache Nope - GetAttributes");
                return await model.GetAttributes(selection, layerID, trans, atTime);
            }
        }

        public async Task<(CIAttribute attribute, bool changed)> InsertAttribute(string name, IAttributeValue value, Guid ciid, string layerID, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        {
            var t = await model.InsertAttribute(name, value, ciid, layerID, changesetProxy, origin, trans);
            if (t.changed)
            {
                trans.EvictFromCache(CacheKeyService.Attributes(ciid, layerID));
                trans.EvictFromCache(CacheKeyService.CIIDsWithAttributeName(name, layerID));
                if (name == ICIModel.NameAttribute) trans.EvictFromCache(CacheKeyService.CINames(layerID));
            }
            return t;
        }

        public async Task<(CIAttribute attribute, bool changed)> RemoveAttribute(string name, Guid ciid, string layerID, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        {
            var t = await model.RemoveAttribute(name, ciid, layerID, changesetProxy, origin, trans);
            if (t.changed)
            {
                trans.EvictFromCache(CacheKeyService.Attributes(ciid, layerID));
                trans.EvictFromCache(CacheKeyService.CIIDsWithAttributeName(name, layerID));
                if (name == ICIModel.NameAttribute) trans.EvictFromCache(CacheKeyService.CINames(layerID));
            }
            return t;
        }

        public async Task<IEnumerable<(Guid ciid, string fullName)>> BulkReplaceAttributes<F>(IBulkCIAttributeData<F> data, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        {
            var changed = await model.BulkReplaceAttributes(data, changesetProxy, origin, trans);
            var evictCINames = false;
            foreach (var (ciid, fullName) in changed)
            {
                trans.EvictFromCache(CacheKeyService.Attributes(ciid, data.LayerID)); // NOTE: inserted list is not distinct on ciids, but that's ok
                trans.EvictFromCache(CacheKeyService.CIIDsWithAttributeName(fullName, data.LayerID)); // NOTE: inserted list is not distinct on attribute names, but that's ok
                evictCINames = evictCINames || fullName == ICIModel.NameAttribute;
            }
            if (evictCINames) trans.EvictFromCache(CacheKeyService.CINames(data.LayerID));
            return changed;
        }

        public Task<IEnumerable<CIAttribute>> GetAttributesOfChangeset(Guid changesetID, IModelContext trans)
        {
            // TODO: caching
            return model.GetAttributesOfChangeset(changesetID, trans);
        }
    }
}
