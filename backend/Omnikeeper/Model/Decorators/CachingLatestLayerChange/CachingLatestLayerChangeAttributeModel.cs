﻿using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Model.Decorators.CachingLatestLayerChange
{
    public class CachingLatestLayerChangeAttributeModel : IBaseAttributeModel
    {
        private readonly IBaseAttributeModel model;
        private readonly LatestLayerChangeCache cache;

        public CachingLatestLayerChangeAttributeModel(IBaseAttributeModel model, LatestLayerChangeCache cache)
        {
            this.model = model;
            this.cache = cache;
        }

        public async Task<IDictionary<Guid, IDictionary<string, CIAttribute>>[]> GetAttributes(ICIIDSelection selection, IAttributeSelection attributeSelection, string[] layerIDs, IModelContext trans, TimeThreshold atTime)
        {
            return await model.GetAttributes(selection, attributeSelection, layerIDs, trans, atTime);
        }

        public async Task<IEnumerable<CIAttribute>> GetAttributesOfChangeset(Guid changesetID, bool getRemoved, IModelContext trans)
        {
            return await model.GetAttributesOfChangeset(changesetID, getRemoved, trans);
        }

        public async Task<CIAttribute?> GetFullBinaryAttribute(string name, Guid ciid, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            return await model.GetFullBinaryAttribute(name, ciid, layerID, trans, atTime);
        }

        public async Task<ISet<Guid>> GetCIIDsWithAttributes(ICIIDSelection selection, string[] layerIDs, IModelContext trans, TimeThreshold atTime)
        {
            return await model.GetCIIDsWithAttributes(selection, layerIDs, trans, atTime);
        }

        public async Task<(CIAttribute attribute, bool changed)> InsertAttribute(string name, IAttributeValue value, Guid ciid, string layerID, IChangesetProxy changeset, DataOriginV1 origin, IModelContext trans)
        {
            var t = await model.InsertAttribute(name, value, ciid, layerID, changeset, origin, trans);
            if (t.changed)
                cache.UpdateCache(layerID, changeset.TimeThreshold.Time);
            return t;
        }

        public async Task<(CIAttribute attribute, bool changed)> RemoveAttribute(string name, Guid ciid, string layerID, IChangesetProxy changeset, DataOriginV1 origin, IModelContext trans)
        {
            var t = await model.RemoveAttribute(name, ciid, layerID, changeset, origin, trans);
            if (t.changed)
                cache.UpdateCache(layerID, changeset.TimeThreshold.Time);
            return t;
        }

        public async Task<IEnumerable<(Guid ciid, string fullName)>> BulkReplaceAttributes<F>(IBulkCIAttributeData<F> data, IChangesetProxy changeset, DataOriginV1 origin, IModelContext trans)
        {
            var t = await model.BulkReplaceAttributes(data, changeset, origin, trans);
            if (!t.IsEmpty())
                cache.UpdateCache(data.LayerID, changeset.TimeThreshold.Time);
            return t;
        }
    }
}