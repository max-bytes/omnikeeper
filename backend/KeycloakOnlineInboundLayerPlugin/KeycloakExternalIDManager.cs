using Keycloak.Net;
using Landscape.Base.Inbound;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KeycloakOnlineInboundLayerPlugin
{
    public class KeycloakExternalUser : IExternalItem
    {
        private readonly Keycloak.Net.Models.Users.User user;

        public KeycloakExternalUser(Keycloak.Net.Models.Users.User user)
        {
            this.user = user;
        }
        public string ID => user.Id;
    }

    public class KeycloakExternalIDManager : ExternalIDManager
    {
        private readonly KeycloakClient client;
        private readonly string realm;

        public KeycloakExternalIDManager(KeycloakClient client, string realm, ExternalIDMapper mapper, ICIModel ciModel) : base(mapper, ciModel)
        {
            this.client = client;
            this.realm = realm;
        }

        protected override async Task<IEnumerable<IExternalItem>> GetExternalItems()
        {
            var users = await client.GetUsersAsync(realm, true, null, null, null, null, 99999, null, null); // TODO, HACK: magic number, how to properly get all user IDs?

            return users.Select(u => new KeycloakExternalUser(u));
        }
    }
}
