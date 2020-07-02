using Flurl;
using Flurl.Http;
using Keycloak.Net;
using Landscape.Base.Entity;
using Landscape.Base.Inbound;
using Landscape.Base.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OnlineInboundAdapterKeycloak
{
    public class OnlineInboundAdapter : IOnlineInboundAdapter
    {
        public class Builder : IOnlineInboundAdapterBuilder
        {
            public string Name => "Keycloak";

            public IOnlineInboundAdapter Build(IOnlineInboundAdapter.IConfig config, IExternalIDMapper externalIDMapper, IExternalIDMapPersister persister)
            {
                var cconfig = config as Config;
                var scopedExternalIDMapper = externalIDMapper.RegisterScoped(new KeycloakScopedExternalIDMapper(cconfig.mapperScope, persister));
                return new OnlineInboundAdapter(cconfig, scopedExternalIDMapper);
            }
        }

        public class Config : IOnlineInboundAdapter.IConfig
        {
            public readonly string apiURL;
            public readonly string realm;
            public readonly string clientID;
            public readonly string clientSecret;
            public readonly string mapperScope;
            public TimeSpan preferredIDMapUpdateRate;

            public Config(string apiURL, string realm, string clientID, string clientSecret, TimeSpan preferredIDMapUpdateRate, string mapperScope)
            {
                this.apiURL = apiURL;
                this.realm = realm;
                this.clientID = clientID;
                this.clientSecret = clientSecret;
                this.mapperScope = mapperScope;
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

        public IOnlineInboundLayerAccessProxy GetLayerAccessProxy(Layer layer) => new KeycloakLayerAccessProxy(client, config.realm, scopedExternalIDMapper, layer);
    }
}
