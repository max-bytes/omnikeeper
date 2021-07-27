using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Configuration;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Service
{
    public class KeycloakAuthorizationService : IKeycloakAuthorizationService
    {
        private readonly IConfiguration configuration;
        public KeycloakAuthorizationService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        /**
         * filter all allowed permission from the input list
         */
        public async Task<ISet<string>> CheckPermissions(AuthenticatedUser user, ISet<string> permissionsToCheck)
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

            var accessToken = user.AccessToken;

            try
            {
                // get ALL permissions for user
                var data = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:uma-ticket"),
                        new KeyValuePair<string, string>("response_mode", "permissions"),
                        new KeyValuePair<string, string>("response_include_resource_name", "true"),
                        new KeyValuePair<string, string>("audience", clientID)
                    };
                var allPermissions = await keycloakURL
                    .AppendPathSegment($"/auth/realms/{realm}/protocol/openid-connect/token")
                    .WithHeader("Authorization", $"Bearer {accessToken}")
                    .PostUrlEncodedAsync(data)
                    .ReceiveJsonList()
                    .ConfigureAwait(false);

                var ret = new HashSet<string>();
                foreach (var permission in allPermissions)
                {
                    if (((IDictionary<String, object>)permission).ContainsKey("scopes"))
                    {
                        var permissionName = permission.rsname;
                        var permissionScopes = permission.scopes;

                        foreach (var scope in permissionScopes)
                        {
                            var combined = $"{permissionName}#{scope}";
                            if (permissionsToCheck.Contains(combined))
                                ret.Add(combined);
                        }
                    }
                }

                return ret;
            }
            catch (Exception e)
            {
                // TODO: handle differently
                return new HashSet<string> { };
            }
        }



        public async Task<IEnumerable<T>> CheckPermissions<T>(AuthenticatedUser user, IEnumerable<T> objects, Func<T, string> permissionExtractor)
        {
            var d = objects.ToDictionary(tt => permissionExtractor(tt));
            var filtered = await CheckPermissions(user, d.Keys.ToHashSet());
            return d.Where(dd => filtered.Contains(dd.Key)).Select(dd => dd.Value);
        }


        /**
         * param name="permission" permission syntax: [resource#scope] e.g. layer_cmdb#write
         */
        public async Task<bool> HasPermission(AuthenticatedUser user, string permission)
        {
            return await HasPermissions(user, new HashSet<string>() { permission });
        }

        /**
         * param name="permission" permission syntax: [resource#scope] e.g. layer_cmdb#write
         */
        public async Task<bool> HasPermissions(AuthenticatedUser user, ISet<string> permissions)
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

            var accessToken = user.AccessToken;

            try
            {
                var data = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:uma-ticket"),
                        new KeyValuePair<string, string>("response_mode", "decision"),
                        new KeyValuePair<string, string>("audience", clientID) // TODO: needed?
                    };
                foreach (var permission in permissions)
                    data.Add(new KeyValuePair<string, string>("permission", permission));

                var y = await keycloakURL
                    .AppendPathSegment($"/auth/realms/{realm}/protocol/openid-connect/token")
                    .WithHeader("Authorization", $"Bearer {accessToken}")
                    .PostUrlEncodedAsync(data)
                    .ReceiveJson()
                    .ConfigureAwait(false);
                return y.result;
            }
            catch (Exception e)
            {
                // TODO: handle differently
                return false;
            }
        }
    }
}
