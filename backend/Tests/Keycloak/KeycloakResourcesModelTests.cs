using Keycloak.Protection.Net;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Omnikeeper.Model.Keycloak;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tests.Keycloak
{
    class KeycloakResourcesModelTests
    {
        [Test]
        public async Task Test()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> {
                    { "Keycloak:URL", "https://localhost:9095/" },
                    { "Keycloak:Realm", "landscape" },
                    { "Keycloak:ClientID", "landscape-omnikeeper-api" },
                    { "Keycloak:ClientSecret", "b1d171a4-76bd-4a9a-b34d-956de13d9b32" },
                })
                .Build();
            var model = new KeycloakProtectionAPIModel(configuration);

            var resource = new ResourceRequest()
            {
                Name = "layer_test",
                Type = "ok:layer",
                ResourceScopes = new string[] { "read", "write", "foo" }
            };
            var createdID = await model.CreateResource(resource);
            Assert.IsNotNull(createdID);

            var resourceUpdate = new ResourceRequest()
            {
                Name = "layer_test2",
                Type = "ok:layer",
                ResourceScopes = new string[] { "read", "write" }
            };
            var successUpdate = await model.UpdateResource(createdID, resourceUpdate);
            Assert.IsTrue(successUpdate);


            var resourceIDs = await model.GetResources();
            Assert.IsTrue(resourceIDs.Count() >= 1);
            Assert.Contains(createdID, resourceIDs.ToList());

            var fetchResource = await model.GetResource(createdID);
            Assert.IsNotNull(fetchResource);
            Assert.AreEqual(resourceUpdate.Name, fetchResource.Name);
            Assert.AreEqual(resourceUpdate.ResourceScopes, fetchResource.ResourceScopes.Select(s => s.Name));

            var successDelete = await model.DeleteResource(createdID);
            Assert.IsTrue(successDelete);
        }
    }
}
