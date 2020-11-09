using Flurl;
using Flurl.Http;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace OKPluginOIAOmnikeeper
{
    public class OnlineInboundAdapter : IOnlineInboundAdapter
    {
        public class Builder : IOnlineInboundAdapterBuilder
        {
            public string Name => StaticName;
            public static string StaticName => "Omnikeeper";

            public IScopedExternalIDMapper BuildIDMapper(IScopedExternalIDMapPersister persister)
            {
                return new ScopedExternalIDMapper(persister);
            }
            public IOnlineInboundAdapter Build(IOnlineInboundAdapter.IConfig config, IConfiguration appConfig, IScopedExternalIDMapper scopedExternalIDMapper, ILoggerFactory loggerFactory)
            {
                var cconfig = config as Config;
                return new OnlineInboundAdapter(cconfig, scopedExternalIDMapper as ScopedExternalIDMapper);
            }
        }

        public class Config : IOnlineInboundAdapter.IConfig
        {
            public readonly string apiURL;
            public readonly string authURL;
            public readonly string realm;
            public readonly string clientID;
            public readonly string clientSecret;
            public string MapperScope { get; }
            public readonly string[] remoteLayerNames;
            public TimeSpan preferredIDMapUpdateRate;

            [Newtonsoft.Json.JsonIgnore]
            public string BuilderName { get; } = Builder.StaticName;

            public Config(string apiURL, string authURL, string realm, string clientID, string clientSecret, string[] remoteLayerNames, TimeSpan preferredIDMapUpdateRate, string mapperScope)
            {
                this.apiURL = apiURL;
                this.authURL = authURL;
                this.realm = realm;
                this.clientID = clientID;
                this.clientSecret = clientSecret;
                MapperScope = mapperScope;
                this.remoteLayerNames = remoteLayerNames;
                this.preferredIDMapUpdateRate = preferredIDMapUpdateRate;
            }
        }

        private readonly Config config;
        private readonly ILandscapeomnikeeperRESTAPIClient client;
        private readonly ExternalIDManager externalIDManager;
        private readonly ScopedExternalIDMapper scopedExternalIDMapper;

        public OnlineInboundAdapter(Config config, ScopedExternalIDMapper scopedExternalIDMapper)
        {
            this.config = config;
            this.scopedExternalIDMapper = scopedExternalIDMapper;

            var handler = new HttpClientHandler();
            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(config.apiURL)
            };

            client = new LandscapeomnikeeperRESTAPIClient(config, httpClient);

            externalIDManager = new ExternalIDManager(client, config, scopedExternalIDMapper);
        }

        public IExternalIDManager GetExternalIDManager() => externalIDManager;

        public ILayerAccessProxy CreateLayerAccessProxy(Layer layer) => new LayerAccessProxy(config.remoteLayerNames, client, scopedExternalIDMapper, layer);
    }
}
