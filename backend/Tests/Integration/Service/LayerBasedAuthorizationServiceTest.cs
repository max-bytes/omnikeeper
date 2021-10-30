using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Service;
using Omnikeeper.Utils;
using System;
using System.Collections.Generic;

namespace Tests.Integration.Service
{
    class LayerBasedAuthorizationServiceTest
    {
        [Test]
        public void TestBasics()
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>() { { "Authorization:debugAllowAll", "false"} })
                .Build();

            var lbas = new LayerBasedAuthorizationService(configuration, new AuthRolePermissionChecker());

            var userInDatabase1 = new UserInDatabase(1L, Guid.NewGuid(), "user1", "User1", UserType.Robot, DateTimeOffset.UtcNow);
            var user1 = new AuthenticatedUser(userInDatabase1, new AuthRole[] { 
                new AuthRole("ar1", new string[] { 
                    PermissionUtils.GetLayerReadPermission("layer1"),
                    PermissionUtils.GetLayerWritePermission("layer3"),
                }),
                new AuthRole("ar2", new string[] {
                    PermissionUtils.GetLayerReadPermission("layer2"),
                    PermissionUtils.GetLayerReadPermission("layer3"),
                    PermissionUtils.GetLayerWritePermission("layer1"),
                })
            });

            Assert.IsTrue(lbas.CanUserReadFromAllLayers(user1, new string[] { "layer1" }));
            Assert.IsTrue(lbas.CanUserReadFromAllLayers(user1, new string[] { "layer2" }));
            Assert.IsTrue(lbas.CanUserReadFromAllLayers(user1, new string[] { "layer3" }));
            Assert.IsFalse(lbas.CanUserReadFromAllLayers(user1, new string[] { "layer4" }));
            Assert.IsTrue(lbas.CanUserReadFromAllLayers(user1, new string[] { "layer1", "layer2" }));
            Assert.IsTrue(lbas.CanUserReadFromAllLayers(user1, new string[] { "layer1", "layer3" }));
            Assert.IsFalse(lbas.CanUserReadFromAllLayers(user1, new string[] { "layer1", "layer3", "layer4" }));

            Assert.IsTrue(lbas.CanUserWriteToAllLayers(user1, new string[] { "layer1" }));
            Assert.IsFalse(lbas.CanUserWriteToAllLayers(user1, new string[] { "layer2" }));
            Assert.IsTrue(lbas.CanUserWriteToAllLayers(user1, new string[] { "layer3" }));
            Assert.IsFalse(lbas.CanUserWriteToAllLayers(user1, new string[] { "layer4" }));
            Assert.IsFalse(lbas.CanUserWriteToAllLayers(user1, new string[] { "layer1", "layer2" }));
            Assert.IsTrue(lbas.CanUserWriteToAllLayers(user1, new string[] { "layer1", "layer3" }));
            Assert.IsFalse(lbas.CanUserWriteToAllLayers(user1, new string[] { "layer1", "layer3", "layer4" }));
        }
    }
}
