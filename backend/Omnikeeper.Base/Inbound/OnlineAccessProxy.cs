using Microsoft.Extensions.Logging;
using Npgsql;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Inbound
{
    public class OnlineAccessProxy : IOnlineAccessProxy
    {
        private readonly ILayerModel layerModel;
        private readonly IInboundAdapterManager pluginManager;

        public OnlineAccessProxy(ILayerModel layerModel, IInboundAdapterManager pluginManager)
        {
            this.layerModel = layerModel;
            this.pluginManager = pluginManager;
        }

        public async Task<bool> IsOnlineInboundLayer(long layerID, IModelContext trans)
        {
            var layer = await layerModel.GetLayer(layerID, trans);
            if (layer == null) return false;
            return await pluginManager.IsValidOnlinePluginInstance(layer.OnlineInboundAdapterLink.AdapterName, trans);
        }

        private async IAsyncEnumerable<(ILayerAccessProxy proxy, Layer layer)> GetAccessProxies(LayerSet layerset, IModelContext trans)
        {
            foreach (var layer in await layerModel.GetLayers(layerset.LayerIDs, trans))
            {
                var plugin = await pluginManager.GetOnlinePluginInstance(layer.OnlineInboundAdapterLink.AdapterName, trans);
                if (plugin != null)
                {
                    yield return (plugin.CreateLayerAccessProxy(layer), layer);
                }
            }
        }

        public async IAsyncEnumerable<(CIAttribute attribute, long layerID)> GetAttributes(ICIIDSelection selection, LayerSet layerset, IModelContext trans, TimeThreshold atTime)
        {
            await foreach (var (proxy, layer) in GetAccessProxies(layerset, trans))
            {
                await foreach (var attribute in proxy.GetAttributes(selection, atTime).Select(a => (a, layer.ID)))
                    yield return attribute;
            }
        }

        public async IAsyncEnumerable<CIAttribute> GetAttributes(ICIIDSelection selection, long layerID, IModelContext trans, TimeThreshold atTime)
        {
            var layer = await layerModel.GetLayer(layerID, trans);
            var plugin = await pluginManager.GetOnlinePluginInstance(layer.OnlineInboundAdapterLink.AdapterName, trans);

            if (plugin == null)
                throw new Exception($"Could not load plugin instance {layer.OnlineInboundAdapterLink.AdapterName}");
            await foreach (var a in plugin.CreateLayerAccessProxy(layer).GetAttributes(selection, atTime))
                yield return a;
        }

        public async IAsyncEnumerable<CIAttribute> FindAttributesByName(string regex, ICIIDSelection selection, long layerID, IModelContext trans, TimeThreshold atTime)
        {
            var layer = await layerModel.GetLayer(layerID, trans);
            var plugin = await pluginManager.GetOnlinePluginInstance(layer.OnlineInboundAdapterLink.AdapterName, trans);

            if (plugin == null)
                throw new Exception($"Could not load plugin instance {layer.OnlineInboundAdapterLink.AdapterName}");
            await foreach (var a in plugin.CreateLayerAccessProxy(layer).FindAttributesByName(regex, selection, atTime))
                yield return a;
        }

        public async IAsyncEnumerable<CIAttribute> FindAttributesByFullName(string name, ICIIDSelection selection, long layerID, IModelContext trans, TimeThreshold atTime)
        {
            var layer = await layerModel.GetLayer(layerID, trans);
            var plugin = await pluginManager.GetOnlinePluginInstance(layer.OnlineInboundAdapterLink.AdapterName, trans);

            if (plugin == null)
                throw new Exception($"Could not load plugin instance {layer.OnlineInboundAdapterLink.AdapterName}");
            await foreach (var a in plugin.CreateLayerAccessProxy(layer).FindAttributesByFullName(name, selection, atTime))
                yield return a;
        }

        public async Task<Relation?> GetRelation(Guid fromCIID, Guid toCIID, string predicateID, long layerID, IModelContext trans, TimeThreshold atTime)
        {
            var layer = await layerModel.GetLayer(layerID, trans);
            var plugin = await pluginManager.GetOnlinePluginInstance(layer.OnlineInboundAdapterLink.AdapterName, trans);
            if (plugin == null)
                throw new Exception($"Could not load plugin instance {layer.OnlineInboundAdapterLink.AdapterName}");

            return await plugin.CreateLayerAccessProxy(layer).GetRelation(fromCIID, toCIID, predicateID, atTime);
        }

        public async IAsyncEnumerable<Relation> GetRelations(IRelationSelection rl, long layerID, IModelContext trans, TimeThreshold atTime)
        {
            var layer = await layerModel.GetLayer(layerID, trans);
            var plugin = await pluginManager.GetOnlinePluginInstance(layer.OnlineInboundAdapterLink.AdapterName, trans);

            if (plugin == null)
                throw new Exception($"Could not load plugin instance {layer.OnlineInboundAdapterLink.AdapterName}");
            await foreach (var relation in plugin.CreateLayerAccessProxy(layer).GetRelations(rl, atTime))
                yield return relation;
        }

        public async Task<CIAttribute?> GetAttribute(string name, long layerID, Guid ciid, IModelContext trans, TimeThreshold atTime)
        {
            var layer = await layerModel.GetLayer(layerID, trans);
            var plugin = await pluginManager.GetOnlinePluginInstance(layer.OnlineInboundAdapterLink.AdapterName, trans);

            if (plugin == null)
                throw new Exception($"Could not load plugin instance {layer.OnlineInboundAdapterLink.AdapterName}");
            return await plugin.CreateLayerAccessProxy(layer).GetAttribute(name, ciid, atTime);
        }
        public async Task<CIAttribute?> GetFullBinaryAttribute(string name, long layerID, Guid ciid, IModelContext trans, TimeThreshold atTime)
        {
            var layer = await layerModel.GetLayer(layerID, trans);
            var plugin = await pluginManager.GetOnlinePluginInstance(layer.OnlineInboundAdapterLink.AdapterName, trans);

            if (plugin == null)
                throw new Exception($"Could not load plugin instance {layer.OnlineInboundAdapterLink.AdapterName}");
            return await plugin.CreateLayerAccessProxy(layer).GetFullBinaryAttribute(name, ciid, atTime);
        }
    }
}
