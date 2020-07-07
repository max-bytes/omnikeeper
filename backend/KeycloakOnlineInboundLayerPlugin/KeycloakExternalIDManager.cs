using Keycloak.Net;
using Landscape.Base.Inbound;
using Landscape.Base.Model;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OnlineInboundAdapterKeycloak
{
    //public class KeycloakExternalUser : IExternalItem<ExternalIDString>
    //{
    //    private readonly Keycloak.Net.Models.Users.User user;

    //    public KeycloakExternalUser(Keycloak.Net.Models.Users.User user)
    //    {
    //        this.user = user;
    //        ID = new ExternalIDString(user.Id);
    //    }
    //    public ExternalIDString ID { get; }
    //}

    public class KeycloakExternalIDManager : ExternalIDManager<ExternalIDString>
    {
        private readonly KeycloakClient client;
        private readonly string realm;

        public KeycloakExternalIDManager(KeycloakClient client, string realm, KeycloakScopedExternalIDMapper mapper, TimeSpan preferredUpdateRate) : base(mapper, preferredUpdateRate)
        {
            this.client = client;
            this.realm = realm;
        }

        protected override async Task<IEnumerable<ExternalIDString>> GetExternalIDs()
        {
            var users = await client.GetUsersAsync(realm, true, null, null, null, null, 99999, null, null); // TODO, HACK: magic number, how to properly get all user IDs?

            return users.Select(u => new ExternalIDString(u.Id));
        }
    }
}
