using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Model.Decorators.CachingEffectiveTraits
{
    public class TraitCacheInvalidationBaseAttributeModel : IBaseAttributeModel
    {
        private readonly IBaseAttributeModel model;
        private readonly IBaseConfigurationModel baseConfigurationModel;
        private readonly EffectiveTraitCache cache;

        public TraitCacheInvalidationBaseAttributeModel(IBaseAttributeModel model, IBaseConfigurationModel baseConfigurationModel, EffectiveTraitCache cache)
        {
            this.model = model;
            this.baseConfigurationModel = baseConfigurationModel;
            this.cache = cache;
        }

        public async Task<(CIAttribute attribute, bool changed)> InsertAttribute(string name, IAttributeValue value, Guid ciid, string layerID, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        {
            var t = await model.InsertAttribute(name, value, ciid, layerID, changesetProxy, origin, trans);
            if (t.changed)
            {
                if (await baseConfigurationModel.IsLayerPartOfBaseConfiguration(layerID, trans))
                    cache.PurgeAll();
                else
                    cache.AddCIID(ciid, layerID);
            }
            return t;
        }

        public async Task<(CIAttribute attribute, bool changed)> RemoveAttribute(string name, Guid ciid, string layerID, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        {
            var t = await model.RemoveAttribute(name, ciid, layerID, changesetProxy, origin, trans);
            if (t.changed)
            {
                if (await baseConfigurationModel.IsLayerPartOfBaseConfiguration(layerID, trans))
                    cache.PurgeAll();
                else
                    cache.AddCIID(ciid, layerID);
            }
            return t;
        }

        public async Task<IEnumerable<(Guid ciid, string fullName)>> BulkReplaceAttributes<F>(IBulkCIAttributeData<F> data, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        {
            var updatedCIIDs = await model.BulkReplaceAttributes(data, changesetProxy, origin, trans);
            if (!updatedCIIDs.IsEmpty())
            {
                if (await baseConfigurationModel.IsLayerPartOfBaseConfiguration(data.LayerID, trans))
                    cache.PurgeAll();
                else
                    cache.AddCIIDs(updatedCIIDs.Select(t => t.ciid), data.LayerID);
            }
            return updatedCIIDs;
        }

        public Task<IEnumerable<CIAttribute>> GetAttributesOfChangeset(Guid changesetID, IModelContext trans)
        {
            return model.GetAttributesOfChangeset(changesetID, trans);
        }

        //public async Task<IDictionary<Guid, IDictionary<string, CIAttribute>>> FindAttributesByName(string regex, ICIIDSelection selection, string layerID, bool returnRemoved, IModelContext trans, TimeThreshold atTime)
        //{
        //    return await model.FindAttributesByName(regex, selection, layerID, returnRemoved, trans, atTime);
        //}

        public async Task<IDictionary<Guid, string>> GetCINames(ICIIDSelection selection, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            return await model.GetCINames(selection, layerID, trans, atTime);
        }

        public async Task<IDictionary<Guid, CIAttribute>> FindAttributesByFullName(string name, ICIIDSelection selection, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            return await model.FindAttributesByFullName(name, selection, layerID, trans, atTime);
        }

        public async Task<IEnumerable<Guid>> FindCIIDsWithAttribute(string name, ICIIDSelection selection, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            return await model.FindCIIDsWithAttribute(name, selection, layerID, trans, atTime);
        }

        public async Task<CIAttribute?> GetAttribute(string name, Guid ciid, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            return await model.GetAttribute(name, ciid, layerID, trans, atTime);
        }

        public async Task<CIAttribute?> GetFullBinaryAttribute(string name, Guid ciid, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            return await model.GetFullBinaryAttribute(name, ciid, layerID, trans, atTime);
        }

        public async Task<IDictionary<Guid, IDictionary<string, CIAttribute>>[]> GetAttributes(ICIIDSelection selection, string[] layerIDs, bool returnRemoved, IModelContext trans, TimeThreshold atTime, string? nameRegexFilter = null)
        {
            return await model.GetAttributes(selection, layerIDs, returnRemoved, trans, atTime, nameRegexFilter);
        }

        public async Task<IEnumerable<Guid>> FindCIIDsWithAttributeNameAndValue(string name, IAttributeValue value, ICIIDSelection selection, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            return await model.FindCIIDsWithAttributeNameAndValue(name, value, selection, layerID, trans, atTime);
        }
    }
}
