using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Landscape.Base.Inbound
{
    public interface IInboundAdapterManager
    {
        IOnlineInboundAdapter GetOnlinePluginInstance(string instanceName);
    }

    public class InboundAdapterManager : IInboundAdapterManager
    {
        private readonly IDictionary<string, IOnlineInboundAdapterBuilder> onlinePluginsBuilders;
        private readonly IDictionary<string, (IOnlineInboundAdapterBuilder builder, IOnlineInboundAdapter.IConfig config)> staticConfiguredPlugins;
        private readonly IExternalIDMapper externalIDMapper;
        private readonly IExternalIDMapPersister persister;

        public InboundAdapterManager(IEnumerable<IOnlineInboundAdapterBuilder> onlinePluginBuilders, IExternalIDMapper externalIDMapper, IExternalIDMapPersister persister)
        {
            this.onlinePluginsBuilders = onlinePluginBuilders.ToDictionary(p => p.Name);
            this.externalIDMapper = externalIDMapper;
            this.persister = persister;
            staticConfiguredPlugins = new Dictionary<string, (IOnlineInboundAdapterBuilder builder, IOnlineInboundAdapter.IConfig config)>();
        }

        public void RegisterStaticOnlinePlugin(string builderName, IOnlineInboundAdapter.IConfig config, string instanceName)
        {
            var builder = onlinePluginsBuilders[builderName];
            staticConfiguredPlugins.Add(instanceName, (builder, config));
        }

        public IOnlineInboundAdapter GetOnlinePluginInstance(string instanceName)
        {
            // TODO: add dynamic plugins
            if (staticConfiguredPlugins.TryGetValue(instanceName, out var t))
            {
                var (builder, config) = t;
                return builder.Build(config, externalIDMapper, persister);
            }
            return null;
        }
    }
}
