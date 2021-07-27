using Flurl;
using Flurl.Http;
using Keycloak.Protection.Net;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Utils
{
    public static class KeycloakUtils
    {
        public static async Task<(KeycloakProtectionClient, string realm, string pat)> CreateProtectionClient(IConfiguration configuration)
        {
            var keycloakURL = configuration.GetSection("Keycloak")["URL"];
            if (keycloakURL == null)
                throw new Exception("Keycloak.URL not set in configuration");
            var realm = configuration.GetSection("Keycloak")["Realm"];
            if (realm == null)
                throw new Exception("Keycloak.Realm not set in configuration");
            var clientID = configuration.GetSection("Keycloak")["ClientID"];
            if (clientID == null)
                throw new Exception("Keycloak.ClientID not set in configuration");
            var clientSecret = configuration.GetSection("Keycloak")["ClientSecret"];
            if (clientSecret == null)
                throw new Exception("Keycloak.ClientSecret not set in configuration");

            var client = new KeycloakProtectionClient(keycloakURL, () => GetAccessTokenAsync(keycloakURL, realm, clientID, clientSecret).GetAwaiter().GetResult());

            var pat = await client.GetPatAsync(realm, new Dictionary<string, string>
                {
                    { "grant_type", "client_credentials" },
                    { "client_secret", clientSecret },
                    { "client_id", clientID }
                });

            return (client, realm, pat);
        }

        private static async Task<string> GetAccessTokenAsync(string url, string realm, string clientID, string clientSecret)
        {
            var result = await url
                .AppendPathSegment($"/auth/realms/{realm}/protocol/openid-connect/token")
                .WithHeader("Accept", "application/json")
                .PostUrlEncodedAsync(new Dictionary<string, string>
                {
                    { "grant_type", "client_credentials" },
                    { "client_secret", clientSecret },
                    { "client_id", clientID }
                })
                .ReceiveJson();

            string accessToken = result.access_token.ToString();

            return accessToken;
        }
    }
}
