using Flurl;
using Flurl.Http;
using Keycloak.Net;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;
using Keycloak.Protection.Net;
using System;
using Omnikeeper.Base.Utils;

namespace Omnikeeper.Model.Keycloak
{
    public class KeycloakProtectionAPIModel
    {
        private readonly IConfiguration configuration;
        public KeycloakProtectionAPIModel(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public async Task<IEnumerable<string>> GetResources()
        {
            var (client, realm, pat) = await KeycloakUtils.CreateProtectionClient(configuration);
            var result = await client.GetResourcesAsync(realm, pat);
            return result;
        }

        public async Task<Resource> GetResource(string resourceId)
        {
            var (client, realm, pat) = await KeycloakUtils.CreateProtectionClient(configuration);
            var result = await client.GetResourceAsync(realm, pat, resourceId);
            return result;
        }

        public async Task<string> CreateResource(ResourceRequest resource)
        {
            var (client, realm, pat) = await KeycloakUtils.CreateProtectionClient(configuration);

            var result = await client.CreateResourceAsync(realm, pat, resource);
            return result.Id;
        }

        // NOTE: complete replacement, no incremental updates
        public async Task<bool> UpdateResource(string resourceID, ResourceRequest resource)
        {
            var (client, realm, pat) = await KeycloakUtils.CreateProtectionClient(configuration);

            var result = await client.UpdateResourceAsync(realm, pat, resourceID, resource);
            return result;
        }

        public async Task<bool> DeleteResource(string resourceId)
        {
            var (client, realm, pat) = await KeycloakUtils.CreateProtectionClient(configuration);

            var result = await client.DeleteResourceAsync(realm, pat, resourceId);
            return result;
        }
    }
}
