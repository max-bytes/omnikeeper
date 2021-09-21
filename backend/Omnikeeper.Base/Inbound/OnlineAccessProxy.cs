﻿using Omnikeeper.Base.Entity;
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

        public async Task<bool> IsOnlineInboundLayer(string layerID, IModelContext trans)
        {
            var layer = await layerModel.GetLayer(layerID, trans);
            if (layer == null) return false;
            var adapterName = layer.OnlineInboundAdapterLink.AdapterName;
            return await pluginManager.IsValidOnlinePluginInstance(adapterName, trans);
        }

        public async Task<bool> ContainsOnlineInboundLayer(LayerSet layerset, IModelContext trans)
        {
            var layers = await layerModel.GetLayers(layerset.LayerIDs, trans);
            foreach (var layer in layers)
            {
                var adapterName = layer.OnlineInboundAdapterLink.AdapterName;
                if (await pluginManager.IsValidOnlinePluginInstance(adapterName, trans))
                    return true;
            }
            return false;
        }

        private async IAsyncEnumerable<(ILayerAccessProxy proxy, Layer layer)> GetAccessProxies(string[] layerIDs, IModelContext trans)
        {
            foreach (var layer in await layerModel.GetLayers(layerIDs, trans))
            {
                var plugin = await pluginManager.GetOnlinePluginInstance(layer.OnlineInboundAdapterLink.AdapterName, trans);
                if (plugin != null)
                {
                    yield return (plugin.CreateLayerAccessProxy(layer), layer);
                }
            }
        }

        public async IAsyncEnumerable<(CIAttribute attribute, string layerID)> GetAttributes(ICIIDSelection selection, string[] layerIDs, IModelContext trans, TimeThreshold atTime, string? nameRegexFilter = null)
        {
            await foreach (var (proxy, layer) in GetAccessProxies(layerIDs, trans))
            {
                await foreach (var attribute in proxy.GetAttributes(selection, atTime, nameRegexFilter).Select(a => (a, layer.ID)))
                    yield return attribute;
            }
        }

        public async IAsyncEnumerable<CIAttribute> FindAttributesByFullName(string name, ICIIDSelection selection, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            var layer = await layerModel.GetLayer(layerID, trans);
            if (layer == null)
                throw new Exception($"Could not find layer with ID {layerID}");
            var plugin = await pluginManager.GetOnlinePluginInstance(layer.OnlineInboundAdapterLink.AdapterName, trans);

            if (plugin == null)
                throw new Exception($"Could not load plugin instance {layer.OnlineInboundAdapterLink.AdapterName}");
            await foreach (var a in plugin.CreateLayerAccessProxy(layer).FindAttributesByFullName(name, selection, atTime))
                yield return a;
        }

        public async Task<Relation?> GetRelation(Guid fromCIID, Guid toCIID, string predicateID, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            var layer = await layerModel.GetLayer(layerID, trans);
            if (layer == null)
                throw new Exception($"Could not find layer with ID {layerID}");
            var plugin = await pluginManager.GetOnlinePluginInstance(layer.OnlineInboundAdapterLink.AdapterName, trans);
            if (plugin == null)
                throw new Exception($"Could not load plugin instance {layer.OnlineInboundAdapterLink.AdapterName}");

            return await plugin.CreateLayerAccessProxy(layer).GetRelation(fromCIID, toCIID, predicateID, atTime);
        }

        public async IAsyncEnumerable<Relation> GetRelations(IRelationSelection rl, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            var layer = await layerModel.GetLayer(layerID, trans);
            if (layer == null)
                throw new Exception($"Could not find layer with ID {layerID}");
            var plugin = await pluginManager.GetOnlinePluginInstance(layer.OnlineInboundAdapterLink.AdapterName, trans);

            if (plugin == null)
                throw new Exception($"Could not load plugin instance {layer.OnlineInboundAdapterLink.AdapterName}");
            await foreach (var relation in plugin.CreateLayerAccessProxy(layer).GetRelations(rl, atTime))
                yield return relation;
        }

        public async Task<CIAttribute?> GetFullBinaryAttribute(string name, string layerID, Guid ciid, IModelContext trans, TimeThreshold atTime)
        {
            var layer = await layerModel.GetLayer(layerID, trans);
            if (layer == null)
                throw new Exception($"Could not find layer with ID {layerID}");
            var plugin = await pluginManager.GetOnlinePluginInstance(layer.OnlineInboundAdapterLink.AdapterName, trans);

            if (plugin == null)
                throw new Exception($"Could not load plugin instance {layer.OnlineInboundAdapterLink.AdapterName}");
            return await plugin.CreateLayerAccessProxy(layer).GetFullBinaryAttribute(name, ciid, atTime);
        }
    }
}
