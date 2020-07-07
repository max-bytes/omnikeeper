﻿using Landscape.Base.Entity;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public async IAsyncEnumerable<(CIAttribute attribute, long layerID)> GetAttributes(ISet<Guid> ciids, LayerSet layerset, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            await foreach (var (proxy, layer) in GetAccessProxies(layerset, trans))
            {
                await foreach (var attribute in proxy.GetAttributes(ciids, atTime).Select(a => (a, layer.ID)))
                    yield return attribute;
            }
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

    }
}
