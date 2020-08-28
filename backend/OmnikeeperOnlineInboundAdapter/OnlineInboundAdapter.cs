using Flurl;
using Flurl.Http;
using Landscape.Base.Entity;
using Landscape.Base.Inbound;
using Landscape.Base.Model;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace OnlineInboundAdapterOmnikeeper
{
    public class OnlineInboundAdapter : IOnlineInboundAdapter
    {
        public class Builder : IOnlineInboundAdapterBuilder
        {
            public string Name => StaticName;
            public static string StaticName => "Omnikeeper";

            public IOnlineInboundAdapter Build(IOnlineInboundAdapter.IConfig config, IConfiguration appConfig, IExternalIDMapper externalIDMapper, IExternalIDMapPersister persister)
            {
                var cconfig = config as Config;
                var scopedExternalIDMapper = externalIDMapper.RegisterScoped(new ScopedExternalIDMapper(cconfig.mapperScope, persister));
                return new OnlineInboundAdapter(cconfig, scopedExternalIDMapper);
            }
        }

        public class Config : IOnlineInboundAdapter.IConfig
        {
            public readonly string apiURL;
            public readonly string authURL;
            public readonly string realm;
            public readonly string clientID;
            public readonly string clientSecret;
            public readonly string mapperScope;
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
                this.mapperScope = mapperScope;
                this.remoteLayerNames = remoteLayerNames;
                this.preferredIDMapUpdateRate = preferredIDMapUpdateRate;
            }
        }

        private readonly Config config;
        private readonly ILandscapeRegistryRESTAPIClient client;
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

            client = new LandscapeRegistryRESTAPIClient(config, httpClient);

            externalIDManager = new ExternalIDManager(client, config, scopedExternalIDMapper);
        }

        public IExternalIDManager GetExternalIDManager() => externalIDManager;

        public IOnlineInboundLayerAccessProxy GetLayerAccessProxy(Layer layer) => new LayerAccessProxy(config.remoteLayerNames, client, scopedExternalIDMapper, layer);
    }
}
