using Flurl;
using Flurl.Http;
using Keycloak.Net;
using Landscape.Base.Entity;
using Landscape.Base.Inbound;
using Landscape.Base.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KeycloakOnlineInboundLayerPlugin
{
    public class KeycloakOnlineInboundLayerPluginBuilder : IOnlineInboundLayerPluginBuilder
    {
        public string Name => "Keycloak";

        public IOnlineInboundLayerPlugin Build(IOnlineInboundLayerPlugin.IConfig config)
        {
            return new KeycloakOnlineInboundLayerPlugin(config as KeycloakOnlineInboundLayerPlugin.Config);
        }
    }

    public class KeycloakOnlineInboundLayerPlugin : IOnlineInboundLayerPlugin
    {
        public class Config : IOnlineInboundLayerPlugin.IConfig
        {
            public readonly string apiURL;
            public readonly string realm;
            public readonly string clientID;
            public readonly string clientSecret;
            public readonly ExternalIDMapper mapper;

            public Config(string apiURL, string realm, string clientID, string clientSecret, ExternalIDMapper mapper)
            {
                this.apiURL = apiURL;
                this.realm = realm;
                this.clientID = clientID;
                this.clientSecret = clientSecret;
                this.mapper = mapper;
            }
        }


        private readonly Config config;
        private readonly KeycloakClient client;

        public KeycloakOnlineInboundLayerPlugin(Config config)
        {
            this.config = config;
            string GetAccessToken() => GetAccessTokenAsync(config.apiURL, config.realm, config.clientID, config.clientSecret).GetAwaiter().GetResult();

            client = new KeycloakClient(config.apiURL, GetAccessToken);
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

        public IExternalIDManager GetExternalIDManager(ICIModel ciModel) => new KeycloakExternalIDManager(client, config.realm, config.mapper, ciModel);

        public IOnlineInboundLayerAccessProxy GetLayerAccessProxy(Layer layer) => new KeycloakLayerAccessProxy(client, config.realm, config.mapper, layer);
    }
}
