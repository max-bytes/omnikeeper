using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Inbound
{
    public interface IInboundAdapterManager
    {
        Task<bool> IsValidOnlinePluginInstance(string instanceName, IModelContext trans);
        Task<IOnlineInboundAdapter?> GetOnlinePluginInstance(string instanceName, IModelContext trans);
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
        private readonly IExternalIDMapPersister persister;
        private readonly IConfiguration appConfig;

        public InboundAdapterManager(IEnumerable<IOnlineInboundAdapterBuilder> onlinePluginBuilders, IExternalIDMapper externalIDMapper,
            IOIAContextModel ioaConfigModel, ILoggerFactory loggerFactory,
            IExternalIDMapPersister persister, IConfiguration appConfig)
        {
            this.onlinePluginsBuilders = onlinePluginBuilders.ToDictionary(p => p.Name);
            this.externalIDMapper = externalIDMapper;
            this.ioaConfigModel = ioaConfigModel;
            this.loggerFactory = loggerFactory;
            this.persister = persister;
            this.appConfig = appConfig;
        }

        public async Task<bool> IsValidOnlinePluginInstance(string instanceName, IModelContext trans)
        {
            if (instanceName == "") return false;
            try
            {
                var config = await ioaConfigModel.GetContextByName(instanceName, trans);
                return onlinePluginsBuilders.ContainsKey(config.Config.BuilderName);
            } catch (Exception)
            {
                return false;
            }
        }

        public async Task<IOnlineInboundAdapter?> GetOnlinePluginInstance(string instanceName, IModelContext trans)
        {
            try
            {
                var config = await ioaConfigModel.GetContextByName(instanceName, trans);
                if (onlinePluginsBuilders.TryGetValue(config.Config.BuilderName, out var builder))
                {
                    var idMapper = await externalIDMapper.CreateOrGetScoped(
                        config.Config.MapperScope,
                        () => builder.BuildIDMapper(persister.CreateScopedPersister(config.Config.MapperScope)),
                        trans);

                    return builder.Build(config.Config, appConfig, idMapper, loggerFactory);
                }
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
