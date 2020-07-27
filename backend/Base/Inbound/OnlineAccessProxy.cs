using Landscape.Base.Entity;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Landscape.Base.Model.IRelationModel;

namespace Landscape.Base.Inbound
{
    public class OnlineAccessProxy : IOnlineAccessProxy
    {
        private readonly ILayerModel layerModel;
        private readonly IInboundAdapterManager pluginManager;
        private readonly ILogger<OnlineAccessProxy> logger;

        public OnlineAccessProxy(ILayerModel layerModel, IInboundAdapterManager pluginManager, ILogger<OnlineAccessProxy> logger) {
            this.layerModel = layerModel;
            this.pluginManager = pluginManager;
            this.logger = logger;
        }

        public async Task<bool> IsOnlineInboundLayer(long layerID, NpgsqlTransaction trans)
        {
            var layer = await layerModel.GetLayer(layerID, trans);
            var plugin = await pluginManager.GetOnlinePluginInstance(layer.OnlineInboundAdapterLink.AdapterName, trans);
            return plugin != null;
        }

        private async IAsyncEnumerable<(IOnlineInboundLayerAccessProxy proxy, Layer layer)> GetAccessProxies(LayerSet layerset, NpgsqlTransaction trans)
        {
            foreach (var layer in await layerModel.GetLayers(layerset.LayerIDs, trans))
            {
                var plugin = await pluginManager.GetOnlinePluginInstance(layer.OnlineInboundAdapterLink.AdapterName, trans);
                if (plugin != null)
                {
                    yield return (plugin.GetLayerAccessProxy(layer), layer);
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
            await foreach (var a in plugin.GetLayerAccessProxy(layer).GetAttributes(selection, atTime))
                yield return a;
        }

        public async IAsyncEnumerable<(CIAttribute attribute, long layerID)> GetAttributesWithName(string name, LayerSet layerset, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            await foreach(var (proxy, layer) in GetAccessProxies(layerset, trans))
            {
                IAsyncEnumerable<CIAttribute> attributes;
                try
                {
                    attributes = proxy.GetAttributesWithName(name, atTime);
                } catch (Exception e)
                {
                    logger.LogError(e, $"Error fetching attributes with name from access proxy {proxy.Name}");
                    yield break;
                }
                await foreach (var attribute in attributes.Select(a => (a, layer.ID)))
                    yield return attribute;
            }
        }

        public async IAsyncEnumerable<CIAttribute> FindAttributesByName(string regex, long layerID, NpgsqlTransaction trans, TimeThreshold atTime, Guid? ciid)
        {
            var layer = await layerModel.GetLayer(layerID, trans);
            var plugin = await pluginManager.GetOnlinePluginInstance(layer.OnlineInboundAdapterLink.AdapterName, trans);
            await foreach (var a in plugin.GetLayerAccessProxy(layer).FindAttributesByName(regex, atTime, ciid))
                yield return a;
        }

        public async IAsyncEnumerable<(Relation relation, long layerID)> GetRelations(Guid? ciid, LayerSet layerset, IncludeRelationDirections ird, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            await foreach (var (proxy, layer) in GetAccessProxies(layerset, trans))
            {
                await foreach (var relation in proxy.GetRelations(ciid, ird, atTime).Select(a => (a, layer.ID)))
                    yield return relation;
            }
        }

        public async IAsyncEnumerable<(Relation relation, long layerID)> GetRelationsWithPredicateID(string predicateID, LayerSet layerset, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            await foreach (var (proxy, layer) in GetAccessProxies(layerset, trans))
            {
                await foreach (var relation in proxy.GetRelationsWithPredicateID(predicateID, atTime).Select(a => (a, layer.ID)))
                    yield return relation;
            }
        }

        public async Task<CIAttribute> GetAttribute(string name, long layerID, Guid ciid, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            var layer = await layerModel.GetLayer(layerID, trans);
            var plugin = await pluginManager.GetOnlinePluginInstance(layer.OnlineInboundAdapterLink.AdapterName, trans);
            return await plugin.GetLayerAccessProxy(layer).GetAttribute(name, ciid, atTime);
        }
    }
}
