using Keycloak.Net;
using Landscape.Base.Inbound;
using Landscape.Base.Model;
using System;

namespace KeycloakOnlineInboundLayerPlugin
{
    public class KeycloakOnlineInboundLayerPlugin : IOnlineInboundLayerPlugin
    {
        private readonly KeycloakClient client;
        private readonly string realm;
        private readonly ExternalIDMapper mapper;

        public KeycloakOnlineInboundLayerPlugin(string apiURL, Func<string> getToken, string realm, ExternalIDMapper mapper)
        {
            client = new KeycloakClient(apiURL, getToken);
            this.realm = realm;
            this.mapper = mapper;
        }

        public string Name => "Internal Keycloak";

        public IExternalIDManager GetExternalIDManager(ICIModel ciModel) => new KeycloakExternalIDManager(client, realm, mapper, ciModel);

        public IOnlineInboundLayerAccessProxy GetLayerAccessProxy() => new KeycloakLayerAccessProxy(client, realm, mapper);
    }
}
