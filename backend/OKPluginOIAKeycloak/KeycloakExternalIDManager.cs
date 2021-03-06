using Keycloak.Net;
using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OKPluginOIAKeycloak
{
    public class KeycloakExternalIDManager : ExternalIDManager<ExternalIDString>
    {
        private readonly KeycloakClient client;
        private readonly string realm;

        public KeycloakExternalIDManager(KeycloakClient client, string realm, KeycloakScopedExternalIDMapper mapper, TimeSpan preferredUpdateRate) : base(mapper, preferredUpdateRate)
        {
            this.client = client;
            this.realm = realm;
        }

        protected override async Task<IEnumerable<(ExternalIDString, ICIIdentificationMethod)>> GetExternalIDs()
        {
            var users = await client.GetUsersAsync(realm, true, null, null, null, null, 99999, null, null); // TODO, HACK: magic number, how to properly get all user IDs?

            return users.Select(u => (new ExternalIDString(u.Id), (ICIIdentificationMethod)CIIdentificationMethodNoop.Build()));
        }
    }
}
