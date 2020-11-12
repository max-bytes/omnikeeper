using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Model.Decorators
{
    public class CachingBaseAttributeModel : IBaseAttributeModel
    {
        private readonly IBaseAttributeModel model;

        public CachingBaseAttributeModel(IBaseAttributeModel model)
        {
            this.model = model;
        }

        public async Task<IEnumerable<CIAttribute>> FindAttributesByName(string regex, ICIIDSelection selection, long layerID, IModelContext trans, TimeThreshold atTime)
        {
            // TODO: caching?
            return await model.FindAttributesByName(regex, selection, layerID, trans, atTime);
        }

        public async Task<IEnumerable<CIAttribute>> FindAttributesByFullName(string name, ICIIDSelection selection, long layerID, IModelContext trans, TimeThreshold atTime)
        {
            // TODO: caching?
            return await model.FindAttributesByFullName(name, selection, layerID, trans, atTime);
        }

        public async Task<CIAttribute?> GetAttribute(string name, Guid ciid, long layerID, IModelContext trans, TimeThreshold atTime)
        {
            if (atTime.IsLatest)
            {
                // instead of doing single-attribute caching, we cache all attributes of this ci in a list and pick the fitting one based on name afterwards
                var attributes = await trans.GetOrCreateCachedValueAsync(CacheKeyService.Attributes(ciid, layerID), async () =>
                {
                    return await model.GetAttributes(SpecificCIIDsSelection.Build(ciid), layerID, trans, atTime);
                }, CacheKeyService.AttributesChangeToken(ciid, layerID));

                attributes.FirstOrDefault(p => p.Name.Equals(name));
            }
            return await model.GetAttribute(name, ciid, layerID, trans, atTime);
        }

        public async Task<CIAttribute?> GetFullBinaryAttribute(string name, Guid ciid, long layerID, IModelContext trans, TimeThreshold atTime)
        {
            // no caching for binary attributes
            return await model.GetFullBinaryAttribute(name, ciid, layerID, trans, atTime);
        }

        public async Task<IEnumerable<CIAttribute>> GetAttributes(ICIIDSelection selection, long layerID, IModelContext trans, TimeThreshold atTime)
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
                                if (trans.TryGetCachedValue<IEnumerable<CIAttribute>>(CacheKeyService.Attributes(ciid, layerID), out var attributesOfCI))
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
                                    trans.SetCacheValue(CacheKeyService.Attributes(a.Key, layerID), a.ToList(), CacheKeyService.AttributesChangeToken(a.Key, layerID));

                                // NOTE: a CI containing NO attributes does not return any (obv), and so would also not get a cache entry (with an empty list as value)
                                // meaning it would NEVER get cached and retrieved all the time instead...
                                // to counter that, we check for empty CIs and insert an empty list for those
                                var emptyCIs = notFoundCIIDs.Except(fetched.Select(k => k.CIID).Distinct());
                                foreach (var ciid in emptyCIs)
                                    trans.SetCacheValue(CacheKeyService.Attributes(ciid, layerID), new List<CIAttribute>(), CacheKeyService.AttributesChangeToken(ciid, layerID));

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

        public async Task<(CIAttribute attribute, bool changed)> InsertAttribute(string name, IAttributeValue value, Guid ciid, long layerID, IChangesetProxy changesetProxy, IModelContext trans)
        {
            var t = await model.InsertAttribute(name, value, ciid, layerID, changesetProxy, trans);
            if (t.changed)
                trans.CancelToken(CacheKeyService.AttributesChangeToken(ciid, layerID));
            return t;
        }

        public async Task<(CIAttribute attribute, bool changed)> InsertCINameAttribute(string nameValue, Guid ciid, long layerID, IChangesetProxy changesetProxy, IModelContext trans)
        {
            var t = await model.InsertCINameAttribute(nameValue, ciid, layerID, changesetProxy, trans);
            if (t.changed)
                trans.CancelToken(CacheKeyService.AttributesChangeToken(ciid, layerID));
            return t;
        }

        public async Task<(CIAttribute attribute, bool changed)> RemoveAttribute(string name, Guid ciid, long layerID, IChangesetProxy changesetProxy, IModelContext trans)
        {
            var t = await model.RemoveAttribute(name, ciid, layerID, changesetProxy, trans);
            if (t.changed)
                trans.CancelToken(CacheKeyService.AttributesChangeToken(ciid, layerID));
            return t;
        }

        public async Task<IEnumerable<(Guid ciid, string fullName, IAttributeValue value, AttributeState state)>> BulkReplaceAttributes<F>(IBulkCIAttributeData<F> data, IChangesetProxy changesetProxy, IModelContext trans)
        {
            var inserted = await model.BulkReplaceAttributes(data, changesetProxy, trans);
            foreach (var (ciid, _, _, _) in inserted) trans.CancelToken(CacheKeyService.AttributesChangeToken(ciid, data.LayerID)); // NOTE: inserted list is not distinct on ciids, but that's ok
            return inserted;
        }
    }
}
