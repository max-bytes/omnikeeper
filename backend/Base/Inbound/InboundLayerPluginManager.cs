using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Landscape.Base.Inbound
{
    public interface IInboundLayerPluginManager
    {
        IOnlineInboundLayerPlugin GetOnlinePluginInstance(string instanceName);
    }

    public class InboundLayerPluginManager : IInboundLayerPluginManager
    {
        private readonly IDictionary<string, IOnlineInboundLayerPluginBuilder> onlinePluginsBuilders;
        private readonly IDictionary<string, (IOnlineInboundLayerPluginBuilder builder, IOnlineInboundLayerPlugin.IConfig config)> staticConfiguredPlugins;

        public InboundLayerPluginManager(IEnumerable<IOnlineInboundLayerPluginBuilder> onlinePluginBuilders)
        {
            this.onlinePluginsBuilders = onlinePluginBuilders.ToDictionary(p => p.Name);
            staticConfiguredPlugins = new Dictionary<string, (IOnlineInboundLayerPluginBuilder builder, IOnlineInboundLayerPlugin.IConfig config)>();
        }

        public void RegisterStaticOnlinePlugin(string builderName, IOnlineInboundLayerPlugin.IConfig config, string instanceName)
        {
            var builder = onlinePluginsBuilders[builderName];
            staticConfiguredPlugins.Add(instanceName, (builder, config));
        }

        public IOnlineInboundLayerPlugin GetOnlinePluginInstance(string instanceName)
        {
            // TODO: add dynamic plugins
            if (staticConfiguredPlugins.TryGetValue(instanceName, out var t))
            {
                var (builder, config) = t;
                return builder.Build(config);
            }
            return null;
        }
    }
}
