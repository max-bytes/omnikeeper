using Flurl;
using Flurl.Http;
using Keycloak.Net;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OKPluginOIAKeycloak
{
    public class OnlineInboundAdapter : IOnlineInboundAdapter
    {
        public class Builder : IOnlineInboundAdapterBuilder
        {
            public string Name => StaticName;
            public static string StaticName => "Keycloak";

            public IScopedExternalIDMapper BuildIDMapper(IScopedExternalIDMapPersister persister)
            {
                return new KeycloakScopedExternalIDMapper(persister);
            }

            public IOnlineInboundAdapter Build(IOnlineInboundAdapter.IConfig config, IConfiguration appConfig, IScopedExternalIDMapper scopedExternalIDMapper, ILoggerFactory loggerFactory)
            {
                return new OnlineInboundAdapter((config as Config)!, (scopedExternalIDMapper as KeycloakScopedExternalIDMapper)!);
            }
        }

        public class Config : IOnlineInboundAdapter.IConfig
        {
            public readonly string apiURL;
            public readonly string realm;
            public readonly string clientID;
            public readonly string clientSecret;
            public string MapperScope { get; }
            public TimeSpan preferredIDMapUpdateRate;
            public string BuilderName { get; } = Builder.StaticName;

            public Config(string apiURL, string realm, string clientID, string clientSecret, TimeSpan preferredIDMapUpdateRate, string mapperScope)
            {
                MapperScope = mapperScope;
                this.apiURL = apiURL;
                this.realm = realm;
                this.clientID = clientID;
                this.clientSecret = clientSecret;
                this.preferredIDMapUpdateRate = preferredIDMapUpdateRate;
            }
        }


        public class BuilderInternal : IOnlineInboundAdapterBuilder
        {
            public string Name => StaticName;
            public static string StaticName => "Keycloak Internal";

            public IScopedExternalIDMapper BuildIDMapper(IScopedExternalIDMapPersister persister)
            {
                return new KeycloakScopedExternalIDMapper(persister);
            }

            public IOnlineInboundAdapter Build(IOnlineInboundAdapter.IConfig config, IConfiguration appConfig, IScopedExternalIDMapper scopedExternalIDMapper, ILoggerFactory loggerFactory)
            {
                var configInternal = (config as ConfigInternal)!;

                var keycloakConfig = appConfig.GetSection("Keycloak");
                var authURL = keycloakConfig["URL"];
                var realm = keycloakConfig["Realm"];
                var clientID = keycloakConfig["ClientID"];
                var clientSecret = keycloakConfig["ClientSecret"];
                var cconfig = new Config(authURL, realm, clientID, clientSecret, configInternal.preferredIDMapUpdateRate, configInternal.MapperScope);

                return new OnlineInboundAdapter(cconfig, (scopedExternalIDMapper as KeycloakScopedExternalIDMapper)!);
            }
        }

        public class ConfigInternal : IOnlineInboundAdapter.IConfig
        {
            public string MapperScope { get; }
            public TimeSpan preferredIDMapUpdateRate;
            public string BuilderName { get; } = BuilderInternal.StaticName;

            public ConfigInternal(TimeSpan preferredIDMapUpdateRate, string mapperScope)
            {
                MapperScope = mapperScope;
                this.preferredIDMapUpdateRate = preferredIDMapUpdateRate;
            }
        }


        private readonly Config config;
        private readonly KeycloakClient client;
        private readonly KeycloakExternalIDManager externalIDManager;
        private readonly KeycloakScopedExternalIDMapper scopedExternalIDMapper;

        public OnlineInboundAdapter(Config config, KeycloakScopedExternalIDMapper scopedExternalIDMapper)
        {
            this.config = config;
            this.scopedExternalIDMapper = scopedExternalIDMapper;
            string GetAccessToken() => GetAccessTokenAsync(config.apiURL, config.realm, config.clientID, config.clientSecret).GetAwaiter().GetResult();

            client = new KeycloakClient(config.apiURL, GetAccessToken);

            externalIDManager = new KeycloakExternalIDManager(client, config.realm, scopedExternalIDMapper, config.preferredIDMapUpdateRate);
        }

        private async Task<string> GetAccessTokenAsync(string url, string realm, string client_id, string client_secret)
        {
            var result = await url
                .AppendPathSegment($"/auth/realms/{realm}/protocol/openid-connect/token")
                .WithHeader("Accept", "application/json")
                .PostUrlEncodedAsync(new List<KeyValuePair<string, string>>
                {
                            new KeyValuePair<string, string>("grant_type", "client_credentials"),
                            new KeyValuePair<string, string>("client_secret", client_secret),
                            new KeyValuePair<string, string>("client_id", client_id)
                })
                .ReceiveJson();

            string accessToken = result.access_token.ToString();

            return accessToken;
        }

        public IExternalIDManager GetExternalIDManager() => externalIDManager;

        public ILayerAccessProxy CreateLayerAccessProxy(Layer layer) => new KeycloakLayerAccessProxy(client, config.realm, scopedExternalIDMapper, layer);
    }
}
