﻿
using Autofac;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.Service;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Model.Decorators
{
    public class UsageTrackingBaseAttributeModel : IBaseAttributeModel
    {
        private readonly IBaseAttributeModel model;
        private readonly ScopedLifetimeAccessor scopedLifetimeAccessor;

        public UsageTrackingBaseAttributeModel(IBaseAttributeModel model, ScopedLifetimeAccessor scopedLifetimeAccessor)
        {
            this.model = model;
            this.scopedLifetimeAccessor = scopedLifetimeAccessor;
        }

        private void TrackLayerUsage(string layerID)
        {
            var usageTracker = scopedLifetimeAccessor.GetLifetimeScope()?.Resolve<IScopedUsageTracker>();
            if (usageTracker != null)
                usageTracker.TrackUseLayer(layerID);
        }
        private void TrackLayerUsages(string[] layerIDs)
        {
            var usageTracker = scopedLifetimeAccessor.GetLifetimeScope()?.Resolve<IScopedUsageTracker>();
            if (usageTracker != null)
                foreach (var layerID in layerIDs) 
                    usageTracker.TrackUseLayer(layerID);
        }

        public async Task<IDictionary<Guid, IDictionary<string, CIAttribute>>[]> GetAttributes(ICIIDSelection selection, IAttributeSelection attributeSelection, string[] layerIDs, IModelContext trans, TimeThreshold atTime)
        {
            TrackLayerUsages(layerIDs);
            return await model.GetAttributes(selection, attributeSelection, layerIDs, trans, atTime);
        }

        public async Task<IEnumerable<CIAttribute>> GetAttributesOfChangeset(Guid changesetID, bool getRemoved, IModelContext trans)
        {
            return await model.GetAttributesOfChangeset(changesetID, getRemoved, trans);
        }

        public async Task<CIAttribute?> GetFullBinaryAttribute(string name, Guid ciid, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            TrackLayerUsage(layerID);
            return await model.GetFullBinaryAttribute(name, ciid, layerID, trans, atTime);
        }

        public async Task<ISet<Guid>> GetCIIDsWithAttributes(ICIIDSelection selection, string[] layerIDs, IModelContext trans, TimeThreshold atTime)
        {
            TrackLayerUsages(layerIDs);
            return await model.GetCIIDsWithAttributes(selection, layerIDs, trans, atTime);
        }

        public async Task<(CIAttribute attribute, bool changed)> InsertAttribute(string name, IAttributeValue value, Guid ciid, string layerID, IChangesetProxy changeset, DataOriginV1 origin, IModelContext trans)
        {
            TrackLayerUsage(layerID);
            return await model.InsertAttribute(name, value, ciid, layerID, changeset, origin, trans);
        }

        public async Task<(CIAttribute attribute, bool changed)> RemoveAttribute(string name, Guid ciid, string layerID, IChangesetProxy changeset, DataOriginV1 origin, IModelContext trans)
        {
            TrackLayerUsage(layerID);
            return await model.RemoveAttribute(name, ciid, layerID, changeset, origin, trans);
        }

        public async Task<IEnumerable<(Guid ciid, string fullName)>> BulkReplaceAttributes<F>(IBulkCIAttributeData<F> data, IChangesetProxy changeset, DataOriginV1 origin, IModelContext trans)
        {
            TrackLayerUsage(data.LayerID);
            return await model.BulkReplaceAttributes(data, changeset, origin, trans);
        }
    }
}