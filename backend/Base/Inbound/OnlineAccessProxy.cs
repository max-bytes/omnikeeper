using Landscape.Base.Entity;
using Landscape.Base.Model;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Landscape.Base.Model.IRelationModel;

namespace Landscape.Base.Inbound
{
    public class OnlineAccessProxy : IOnlineAccessProxy
    {
        private readonly ILayerModel layerModel;
        private readonly IInboundLayerPluginManager pluginManager;

        public OnlineAccessProxy(ILayerModel layerModel, IInboundLayerPluginManager pluginManager) {
            this.layerModel = layerModel;
            this.pluginManager = pluginManager;
        }

        private async IAsyncEnumerable<(IOnlineInboundLayerAccessProxy proxy, Layer layer)> GetAccessProxies(LayerSet layerset, NpgsqlTransaction trans)
        {
            foreach (var layer in await layerModel.GetLayers(layerset.LayerIDs, trans))
            {
                var plugin = pluginManager.GetOnlinePluginInstance(layer.OnlineInboundLayerPlugin.PluginName);
                if (plugin != null)
                {
                    yield return (plugin.GetLayerAccessProxy(layer), layer);
                }
            }
        }

        public async IAsyncEnumerable<(CIAttribute attribute, long layerID)> GetAttributes(ISet<Guid> ciids, LayerSet layerset, NpgsqlTransaction trans)
        {
            await foreach (var (proxy, layer) in GetAccessProxies(layerset, trans))
            {
                await foreach (var attribute in proxy.GetAttributes(ciids).Select(a => (a, layer.ID)))
                    yield return attribute;
            }
        }

        public async IAsyncEnumerable<(CIAttribute attribute, long layerID)> GetAttributesWithName(string name, LayerSet layerset, NpgsqlTransaction trans)
        {
            await foreach(var (proxy, layer) in GetAccessProxies(layerset, trans))
            {
                await foreach (var attribute in proxy.GetAttributesWithName(name).Select(a => (a, layer.ID)))
                    yield return attribute;
            }
        }

        public async IAsyncEnumerable<(Relation relation, long layerID)> GetRelations(Guid? ciid, LayerSet layerset, IncludeRelationDirections ird, NpgsqlTransaction trans)
        {
            await foreach (var (proxy, layer) in GetAccessProxies(layerset, trans))
            {
                await foreach (var relation in proxy.GetRelations(ciid, ird).Select(a => (a, layer.ID)))
                    yield return relation;
            }
        }

        public async IAsyncEnumerable<(Relation relation, long layerID)> GetRelationsWithPredicateID(string predicateID, LayerSet layerset, NpgsqlTransaction trans)
        {
            await foreach (var (proxy, layer) in GetAccessProxies(layerset, trans))
            {
                await foreach (var relation in proxy.GetRelationsWithPredicateID(predicateID).Select(a => (a, layer.ID)))
                    yield return relation;
            }
        }
    }
}
