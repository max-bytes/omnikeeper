using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Microsoft.Extensions.Logging;
using Npgsql;
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
        private readonly ILogger<OnlineAccessProxy> logger;

        public OnlineAccessProxy(ILayerModel layerModel, IInboundAdapterManager pluginManager, ILogger<OnlineAccessProxy> logger)
        {
            this.layerModel = layerModel;
            this.pluginManager = pluginManager;
            this.logger = logger;
        }

        public async Task<bool> IsOnlineInboundLayer(long layerID, NpgsqlTransaction trans)
        {
            var layer = await layerModel.GetLayer(layerID, trans);
            if (layer == null) return false;
            var plugin = await pluginManager.GetOnlinePluginInstance(layer.OnlineInboundAdapterLink.AdapterName, trans);
            return plugin != null;
        }

        private async IAsyncEnumerable<(ILayerAccessProxy proxy, Layer layer)> GetAccessProxies(LayerSet layerset, NpgsqlTransaction trans)
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

        public async IAsyncEnumerable<(CIAttribute attribute, long layerID)> GetAttributes(ICIIDSelection selection, LayerSet layerset, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            await foreach (var (proxy, layer) in GetAccessProxies(layerset, trans))
            {
                await foreach (var attribute in proxy.GetAttributes(selection, atTime).Select(a => (a, layer.ID)))
                    yield return attribute;
            }
        }

        public async IAsyncEnumerable<CIAttribute> GetAttributes(ICIIDSelection selection, long layerID, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            var layer = await layerModel.GetLayer(layerID, trans);
            var plugin = await pluginManager.GetOnlinePluginInstance(layer.OnlineInboundAdapterLink.AdapterName, trans);
            await foreach (var a in plugin.CreateLayerAccessProxy(layer).GetAttributes(selection, atTime))
                yield return a;
        }

        public async IAsyncEnumerable<CIAttribute> FindAttributesByName(string regex, ICIIDSelection selection, long layerID, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            var layer = await layerModel.GetLayer(layerID, trans);
            var plugin = await pluginManager.GetOnlinePluginInstance(layer.OnlineInboundAdapterLink.AdapterName, trans);
            await foreach (var a in plugin.CreateLayerAccessProxy(layer).FindAttributesByName(regex, selection, atTime))
                yield return a;
        }

        public async IAsyncEnumerable<CIAttribute> FindAttributesByFullName(string name, ICIIDSelection selection, long layerID, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            var layer = await layerModel.GetLayer(layerID, trans);
            var plugin = await pluginManager.GetOnlinePluginInstance(layer.OnlineInboundAdapterLink.AdapterName, trans);
            await foreach (var a in plugin.CreateLayerAccessProxy(layer).FindAttributesByFullName(name, selection, atTime))
                yield return a;
        }

        public async Task<Relation> GetRelation(Guid fromCIID, Guid toCIID, string predicateID, long layerID, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            var layer = await layerModel.GetLayer(layerID, trans);
            var plugin = await pluginManager.GetOnlinePluginInstance(layer.OnlineInboundAdapterLink.AdapterName, trans);
            return await plugin.CreateLayerAccessProxy(layer).GetRelation(fromCIID, toCIID, predicateID, atTime);
        }

        public async IAsyncEnumerable<Relation> GetRelations(IRelationSelection rl, long layerID, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            var layer = await layerModel.GetLayer(layerID, trans);
            var plugin = await pluginManager.GetOnlinePluginInstance(layer.OnlineInboundAdapterLink.AdapterName, trans);
            await foreach (var relation in plugin.CreateLayerAccessProxy(layer).GetRelations(rl, atTime))
                yield return relation;
        }

        public async Task<CIAttribute> GetAttribute(string name, long layerID, Guid ciid, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            var layer = await layerModel.GetLayer(layerID, trans);
            var plugin = await pluginManager.GetOnlinePluginInstance(layer.OnlineInboundAdapterLink.AdapterName, trans);
            return await plugin.CreateLayerAccessProxy(layer).GetAttribute(name, ciid, atTime);
        }
        public async Task<CIAttribute> GetFullBinaryAttribute(string name, long layerID, Guid ciid, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            var layer = await layerModel.GetLayer(layerID, trans);
            var plugin = await pluginManager.GetOnlinePluginInstance(layer.OnlineInboundAdapterLink.AdapterName, trans);
            return await plugin.CreateLayerAccessProxy(layer).GetFullBinaryAttribute(name, ciid, atTime);
        }
    }
}
