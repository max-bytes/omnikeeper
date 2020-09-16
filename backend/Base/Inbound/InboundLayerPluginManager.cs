using Landscape.Base.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Collections.Generic;
using System.Linq;
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
        private readonly IExternalIDMapper externalIDMapper;
        private readonly IOIAConfigModel ioaConfigModel;
        private readonly ILoggerFactory loggerFactory;
        private readonly IExternalIDMapPersister persister;
        private readonly IConfiguration appConfig;

        public InboundAdapterManager(IEnumerable<IOnlineInboundAdapterBuilder> onlinePluginBuilders, IExternalIDMapper externalIDMapper,
            IOIAConfigModel ioaConfigModel, ILoggerFactory loggerFactory,
            IExternalIDMapPersister persister, IConfiguration appConfig)
        {
            this.onlinePluginsBuilders = onlinePluginBuilders.ToDictionary(p => p.Name);
            this.externalIDMapper = externalIDMapper;
            this.ioaConfigModel = ioaConfigModel;
            this.loggerFactory = loggerFactory;
            this.persister = persister;
            this.appConfig = appConfig;
        }

        public async Task<IOnlineInboundAdapter> GetOnlinePluginInstance(string instanceName, NpgsqlTransaction trans)
        {
            var config = await ioaConfigModel.GetConfigByName(instanceName, trans);
            if (config != null)
            {
                if (onlinePluginsBuilders.TryGetValue(config.Config.BuilderName, out var builder))
                    return builder.Build(config.Config, appConfig, externalIDMapper, persister, loggerFactory);
            }
            return null;
        }
    }
}
