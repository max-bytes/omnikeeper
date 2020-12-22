using Npgsql;
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

        public async Task<IEnumerable<CIAttribute>> FindAttributesByName(string regex, ICIIDSelection selection, long layerID, IModelContext trans, TimeThreshold atTime)
        {
            if (await onlineAccessProxy.IsOnlineInboundLayer(layerID, trans))
            {
                return onlineAccessProxy.FindAttributesByName(regex, selection, layerID, trans, atTime).ToEnumerable();
            }

            return await model.FindAttributesByName(regex, selection, layerID, trans, atTime);
        }

        public async Task<IEnumerable<CIAttribute>> FindAttributesByFullName(string name, ICIIDSelection selection, long layerID, IModelContext trans, TimeThreshold atTime)
        {
            if (await onlineAccessProxy.IsOnlineInboundLayer(layerID, trans))
            {
                return onlineAccessProxy.FindAttributesByFullName(name, selection, layerID, trans, atTime).ToEnumerable();
            }

            return await model.FindAttributesByFullName(name, selection, layerID, trans, atTime);
        }

        public async Task<IEnumerable<Guid>> FindCIIDsWithAttribute(string name, ICIIDSelection selection, long layerID, IModelContext trans, TimeThreshold atTime)
        {
            // TODO: implement
            return await model.FindCIIDsWithAttribute(name, selection, layerID, trans, atTime);
        }

        public async Task<CIAttribute?> GetAttribute(string name, Guid ciid, long layerID, IModelContext trans, TimeThreshold atTime)
        {
            if (await onlineAccessProxy.IsOnlineInboundLayer(layerID, trans))
            {
                return await onlineAccessProxy.GetAttribute(name, layerID, ciid, trans, atTime);
            }

            return await model.GetAttribute(name, ciid, layerID, trans, atTime);
        }

        public async Task<CIAttribute?> GetFullBinaryAttribute(string name, Guid ciid, long layerID, IModelContext trans, TimeThreshold atTime)
        {
            if (await onlineAccessProxy.IsOnlineInboundLayer(layerID, trans))
            {
                return await onlineAccessProxy.GetFullBinaryAttribute(name, layerID, ciid, trans, atTime);
            }

            return await model.GetFullBinaryAttribute(name, ciid, layerID, trans, atTime);
        }

        public async Task<IEnumerable<CIAttribute>> GetAttributes(ICIIDSelection selection, long layerID, IModelContext trans, TimeThreshold atTime)
        {
            if (await onlineAccessProxy.IsOnlineInboundLayer(layerID, trans))
            {
                return onlineAccessProxy.GetAttributes(selection, layerID, trans, atTime).ToEnumerable();
            }

            return await model.GetAttributes(selection, layerID, trans, atTime);
        }

        public async Task<(CIAttribute attribute, bool changed)> InsertAttribute(string name, IAttributeValue value, Guid ciid, long layerID, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        {
            if (await onlineAccessProxy.IsOnlineInboundLayer(layerID, trans)) throw new Exception("Cannot write to online inbound layer");

            return await model.InsertAttribute(name, value, ciid, layerID, changesetProxy, origin, trans);
        }

        public async Task<(CIAttribute attribute, bool changed)> InsertCINameAttribute(string nameValue, Guid ciid, long layerID, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        {
            if (await onlineAccessProxy.IsOnlineInboundLayer(layerID, trans)) throw new Exception("Cannot write to online inbound layer");

            return await model.InsertCINameAttribute(nameValue, ciid, layerID, changesetProxy, origin, trans);
        }

        public async Task<(CIAttribute attribute, bool changed)> RemoveAttribute(string name, Guid ciid, long layerID, IChangesetProxy changesetProxy, IModelContext trans)
        {
            if (await onlineAccessProxy.IsOnlineInboundLayer(layerID, trans)) throw new Exception("Cannot write to online inbound layer");

            return await model.RemoveAttribute(name, ciid, layerID, changesetProxy, trans);
        }

        public async Task<IEnumerable<(Guid ciid, string fullName, IAttributeValue value, AttributeState state)>> BulkReplaceAttributes<F>(IBulkCIAttributeData<F> data, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        {
            if (await onlineAccessProxy.IsOnlineInboundLayer(data.LayerID, trans)) throw new Exception("Cannot write to online inbound layer");

            return await model.BulkReplaceAttributes(data, changesetProxy, origin, trans);
        }
    }
}
