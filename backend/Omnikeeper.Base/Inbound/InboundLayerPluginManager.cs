using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using Omnikeeper.Base.Model;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Inbound
{
    public interface IInboundAdapterManager
    {
        Task<bool> IsValidOnlinePluginInstance(string instanceName, NpgsqlTransaction trans);
        Task<IOnlineInboundAdapter> GetOnlinePluginInstance(string instanceName, NpgsqlTransaction trans);
    }

    /// <summary>
    /// scoped
    /// </summary>
    public class InboundAdapterManager : IInboundAdapterManager
    {
        private readonly IDictionary<string, IOnlineInboundAdapterBuilder> onlinePluginsBuilders;
        private readonly IExternalIDMapper externalIDMapper;
        private readonly IOIAContextModel ioaConfigModel;
        private readonly ILoggerFactory loggerFactory;
        private readonly NpgsqlConnection conn;
        private readonly IExternalIDMapPersister persister;
        private readonly IConfiguration appConfig;

        public InboundAdapterManager(IEnumerable<IOnlineInboundAdapterBuilder> onlinePluginBuilders, IExternalIDMapper externalIDMapper,
            IOIAContextModel ioaConfigModel, ILoggerFactory loggerFactory, NpgsqlConnection conn,
            IExternalIDMapPersister persister, IConfiguration appConfig)
        {
            this.onlinePluginsBuilders = onlinePluginBuilders.ToDictionary(p => p.Name);
            this.externalIDMapper = externalIDMapper;
            this.ioaConfigModel = ioaConfigModel;
            this.loggerFactory = loggerFactory;
            this.conn = conn;
            this.persister = persister;
            this.appConfig = appConfig;
        }

        public async Task<bool> IsValidOnlinePluginInstance(string instanceName, NpgsqlTransaction trans)
        {
            var config = await ioaConfigModel.GetContextByName(instanceName, trans);
            if (config != null)
                return onlinePluginsBuilders.ContainsKey(config.Config.BuilderName);
            return false;
        }

        public async Task<IOnlineInboundAdapter> GetOnlinePluginInstance(string instanceName, NpgsqlTransaction trans)
        {
            var config = await ioaConfigModel.GetContextByName(instanceName, trans);
            if (config != null)
            {
                if (onlinePluginsBuilders.TryGetValue(config.Config.BuilderName, out var builder))
                {
                    var idMapper = await externalIDMapper.CreateOrGetScoped(
                        config.Config.MapperScope,
                        () => builder.BuildIDMapper(persister.CreateScopedPersister(config.Config.MapperScope)),
                        conn, trans);

                    return builder.Build(config.Config, appConfig, idMapper, loggerFactory);
                }
            }
            return null;
        }
    }
}
