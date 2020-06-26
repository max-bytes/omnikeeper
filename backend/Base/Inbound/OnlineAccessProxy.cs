using Landscape.Base.Entity;
using Landscape.Base.Model;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Landscape.Base.Inbound
{
    public class OnlineAccessProxy : IOnlineAccessProxy
    {
        private readonly IDictionary<string, IOnlineInboundLayerPlugin> availablePlugins;
        private readonly ILayerModel layerModel;

        public OnlineAccessProxy(IEnumerable<IOnlineInboundLayerPlugin> availablePlugins, ILayerModel layerModel) {
            this.availablePlugins = availablePlugins.ToDictionary(p => p.Name);
            this.layerModel = layerModel;
        }

        private async IAsyncEnumerable<(IOnlineInboundLayerAccessProxy proxy, Layer layer)> GetAccessProxies(LayerSet layerset, NpgsqlTransaction trans)
        {
            foreach (var layer in await layerModel.GetLayers(layerset.LayerIDs, trans))
            {
                availablePlugins.TryGetValue(layer.OnlineInboundLayerPlugin.PluginName, out var plugin);
                if (plugin != null)
                {
                    yield return (plugin.GetLayerAccessProxy(), layer);
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
    }
}
