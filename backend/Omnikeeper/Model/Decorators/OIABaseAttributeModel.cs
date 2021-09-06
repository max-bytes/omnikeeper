using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Model.Decorators
{
    public class OIABaseAttributeModel : IBaseAttributeModel
    {
        private readonly IBaseAttributeModel model;
        private readonly IOnlineAccessProxy onlineAccessProxy;

        public OIABaseAttributeModel(IBaseAttributeModel model, IOnlineAccessProxy onlineAccessProxy)
        {
            this.model = model;
            this.onlineAccessProxy = onlineAccessProxy;
        }

        public async Task<IEnumerable<CIAttribute>> FindAttributesByName(string regex, ICIIDSelection selection, string layerID, bool returnRemoved, IModelContext trans, TimeThreshold atTime)
        {
            if (await onlineAccessProxy.IsOnlineInboundLayer(layerID, trans))
            {
                return onlineAccessProxy.FindAttributesByName(regex, selection, layerID, trans, atTime).ToEnumerable();
            }

            return await model.FindAttributesByName(regex, selection, layerID, returnRemoved, trans, atTime);
        }

        public async Task<IEnumerable<CIAttribute>> FindAttributesByFullName(string name, ICIIDSelection selection, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            if (await onlineAccessProxy.IsOnlineInboundLayer(layerID, trans))
            {
                return onlineAccessProxy.FindAttributesByFullName(name, selection, layerID, trans, atTime).ToEnumerable();
            }

            return await model.FindAttributesByFullName(name, selection, layerID, trans, atTime);
        }

        public async Task<IEnumerable<Guid>> FindCIIDsWithAttribute(string name, ICIIDSelection selection, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            // TODO: implement
            return await model.FindCIIDsWithAttribute(name, selection, layerID, trans, atTime);
        }

        public async Task<IDictionary<Guid, string>> GetCINames(ICIIDSelection selection, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            if (await onlineAccessProxy.IsOnlineInboundLayer(layerID, trans))
            {
                // TODO: implement properly, instead of falling back to FindAttributesByFullName()
                var attributes = await FindAttributesByFullName(ICIModel.NameAttribute, selection, layerID, trans, atTime);
                return attributes.ToDictionary(a => a.CIID, a => a.Value.Value2String());
            }

            return await model.GetCINames(selection, layerID, trans, atTime);
        }

        public async Task<CIAttribute?> GetAttribute(string name, Guid ciid, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            if (await onlineAccessProxy.IsOnlineInboundLayer(layerID, trans))
            {
                return await onlineAccessProxy.GetAttribute(name, layerID, ciid, trans, atTime);
            }

            return await model.GetAttribute(name, ciid, layerID, trans, atTime);
        }

        public async Task<CIAttribute?> GetFullBinaryAttribute(string name, Guid ciid, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            if (await onlineAccessProxy.IsOnlineInboundLayer(layerID, trans))
            {
                return await onlineAccessProxy.GetFullBinaryAttribute(name, layerID, ciid, trans, atTime);
            }

            return await model.GetFullBinaryAttribute(name, ciid, layerID, trans, atTime);
        }

        public async Task<IEnumerable<CIAttribute>> GetAttributes(ICIIDSelection selection, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            if (await onlineAccessProxy.IsOnlineInboundLayer(layerID, trans))
            {
                return onlineAccessProxy.GetAttributes(selection, layerID, trans, atTime).ToEnumerable();
            }

            return await model.GetAttributes(selection, layerID, trans, atTime);
        }

        public async Task<IEnumerable<Guid>> FindCIIDsWithAttributeNameAndValue(string name, IAttributeValue value, ICIIDSelection selection, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            if (await onlineAccessProxy.IsOnlineInboundLayer(layerID, trans))
            {
                // TODO: implement properly, instead of falling back to FindAttributesByFullName()
                var attributes = await FindAttributesByFullName(name, selection, layerID, trans, atTime);
                return attributes.Where(a => a.Value.Equals(value)).Select(a => a.CIID).ToHashSet();
            }
            return await model.FindCIIDsWithAttributeNameAndValue(name, value, selection, layerID, trans, atTime);
        }

        public async Task<(CIAttribute attribute, bool changed)> InsertAttribute(string name, IAttributeValue value, Guid ciid, string layerID, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        {
            if (await onlineAccessProxy.IsOnlineInboundLayer(layerID, trans)) throw new Exception("Cannot write to online inbound layer");

            return await model.InsertAttribute(name, value, ciid, layerID, changesetProxy, origin, trans);
        }

        public async Task<(CIAttribute attribute, bool changed)> RemoveAttribute(string name, Guid ciid, string layerID, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        {
            if (await onlineAccessProxy.IsOnlineInboundLayer(layerID, trans)) throw new Exception("Cannot write to online inbound layer");

            return await model.RemoveAttribute(name, ciid, layerID, changesetProxy, origin, trans);
        }

        public async Task<IEnumerable<(Guid ciid, string fullName)>> BulkReplaceAttributes<F>(IBulkCIAttributeData<F> data, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        {
            if (await onlineAccessProxy.IsOnlineInboundLayer(data.LayerID, trans)) throw new Exception("Cannot write to online inbound layer");

            return await model.BulkReplaceAttributes(data, changesetProxy, origin, trans);
        }

        public async Task<IEnumerable<CIAttribute>> GetAttributesOfChangeset(Guid changesetID, IModelContext trans)
        {
            // NOTE: OIAs do not support changesets, so an OIA can never return any
            return await model.GetAttributesOfChangeset(changesetID, trans);
        }
    }
}
