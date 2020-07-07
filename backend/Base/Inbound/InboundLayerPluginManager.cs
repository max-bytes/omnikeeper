using Landscape.Base.Model;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Landscape.Base.Inbound
{
    public interface IInboundAdapterManager
    {
        Task<IOnlineInboundAdapter> GetOnlinePluginInstance(string instanceName, NpgsqlTransaction trans);
    }

    public class InboundAdapterManager : IInboundAdapterManager
    {
        private readonly IDictionary<string, IOnlineInboundAdapterBuilder> onlinePluginsBuilders;
        //private readonly IDictionary<string, IOnlineInboundAdapter.IConfig> staticConfiguredPlugins;
        private readonly IExternalIDMapper externalIDMapper;
        private readonly IOIAConfigModel ioaConfigModel;
        private readonly IExternalIDMapPersister persister;
        private readonly IConfiguration appConfig;

        public InboundAdapterManager(IEnumerable<IOnlineInboundAdapterBuilder> onlinePluginBuilders, IExternalIDMapper externalIDMapper,
            IOIAConfigModel ioaConfigModel,
            IExternalIDMapPersister persister, IConfiguration appConfig)
        {
            this.onlinePluginsBuilders = onlinePluginBuilders.ToDictionary(p => p.Name);
            this.externalIDMapper = externalIDMapper;
            this.ioaConfigModel = ioaConfigModel;
            this.persister = persister;
            this.appConfig = appConfig;
            //staticConfiguredPlugins = new Dictionary<string, IOnlineInboundAdapter.IConfig>();
        }

        //public void RegisterOnlineAdapter(IOnlineInboundAdapter.IConfig config, string instanceName)
        //{
        //    staticConfiguredPlugins.Add(instanceName, config);
        //}

        public async Task<IOnlineInboundAdapter> GetOnlinePluginInstance(string instanceName, NpgsqlTransaction trans)
        {
            // TODO: add dynamic plugins
            var config = await ioaConfigModel.GetConfigByName(instanceName, trans);
            if (config != null)
            {
                if (onlinePluginsBuilders.TryGetValue(config.Config.BuilderName, out var builder))
                    return builder.Build(config.Config, appConfig, externalIDMapper, persister);
            }
            return null;
        }
    }
}
