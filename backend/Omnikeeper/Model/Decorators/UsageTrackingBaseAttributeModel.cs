using Autofac;
using Omnikeeper.Base.Entity;
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
    public class UsageTrackingBaseAttributeModel : IBaseAttributeModel
    {
        private readonly IBaseAttributeModel model;
        private readonly ScopedLifetimeAccessor scopedLifetimeAccessor;

        public UsageTrackingBaseAttributeModel(IBaseAttributeModel model, ScopedLifetimeAccessor scopedLifetimeAccessor)
        {
            this.model = model;
            this.scopedLifetimeAccessor = scopedLifetimeAccessor;
        }

        private void TrackAttributeUsage(string attributeName, IEnumerable<string> layerIDs, UsageStatsOperation operation)
        {
            var usageTracker = scopedLifetimeAccessor.GetLifetimeScope()?.Resolve<IScopedUsageTracker>();
            if (usageTracker != null)
                foreach (var layerID in layerIDs)
                    usageTracker.TrackUseAttribute(attributeName, layerID, operation);
        }
        private void TrackAttributeUsages(IEnumerable<string> attributeNames, IEnumerable<string> layerIDs, UsageStatsOperation operation)
        {
            var usageTracker = scopedLifetimeAccessor.GetLifetimeScope()?.Resolve<IScopedUsageTracker>();
            if (usageTracker != null)
                foreach (var name in attributeNames)
                    foreach(var layerID in layerIDs)
                        usageTracker.TrackUseAttribute(name, layerID, operation);
        }

        public async Task<IDictionary<Guid, IDictionary<string, CIAttribute>>[]> GetAttributes(ICIIDSelection selection, IAttributeSelection attributeSelection, string[] layerIDs, IModelContext trans, TimeThreshold atTime, IGeneratedDataHandling generatedDataHandling)
        {
            var usedAttributes = attributeSelection switch
            {
                AllAttributeSelection _ => (IEnumerable<string>)new string[] {"*"},
                NoAttributesSelection _ => Array.Empty<string>(),
                NamedAttributesSelection n => n.AttributeNames,
                //NamedAttributesWithValueFiltersSelection f => f.NamesAndFilters.Select(t => t.Key),
                _ => throw new NotImplementedException("")
            };
            TrackAttributeUsages(usedAttributes, layerIDs, UsageStatsOperation.Read);

            return await model.GetAttributes(selection, attributeSelection, layerIDs, trans, atTime, generatedDataHandling);
        }

        public async Task<IReadOnlyList<CIAttribute>> GetAttributesOfChangeset(Guid changesetID, bool getRemoved, IModelContext trans)
        {
            //TrackAttributeUsage("*"); // TODO: we should fetch the layer of this changeset here
            return await model.GetAttributesOfChangeset(changesetID, getRemoved, trans);
        }

        public async Task<CIAttribute?> GetFullBinaryAttribute(string name, Guid ciid, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            TrackAttributeUsage(name, new string[] { layerID }, UsageStatsOperation.Read);
            return await model.GetFullBinaryAttribute(name, ciid, layerID, trans, atTime);
        }

        public async Task<IReadOnlySet<Guid>> GetCIIDsWithAttributes(ICIIDSelection selection, string[] layerIDs, IModelContext trans, TimeThreshold atTime)
        {
            TrackAttributeUsage("*", layerIDs, UsageStatsOperation.Read);
            return await model.GetCIIDsWithAttributes(selection, layerIDs, trans, atTime);
        }

        public async Task<(bool changed, Guid changesetID)> BulkUpdate(IList<(Guid ciid, string fullName, IAttributeValue value, Guid? existingAttributeID, Guid newAttributeID)> inserts, IList<(Guid ciid, string name, IAttributeValue value, Guid attributeID, Guid newAttributeID)> removes, string layerID, IChangesetProxy changesetProxy, IModelContext trans)
        {
            TrackAttributeUsages(inserts.Select(i => i.fullName).Concat(removes.Select(i => i.name)).Distinct(), new string[] { layerID }, UsageStatsOperation.Write);
            return await model.BulkUpdate(inserts, removes, layerID, changesetProxy, trans);
        }
    }
}
