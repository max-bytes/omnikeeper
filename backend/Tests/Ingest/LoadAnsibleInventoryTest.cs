using Landscape.Base.Entity;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using LandscapeRegistry.Controllers.Ingest;
using LandscapeRegistry.Entity.AttributeValues;
using LandscapeRegistry.Model;
using LandscapeRegistry.Model.Decorators;
using LandscapeRegistry.Service;
using LandscapeRegistry.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.DotNet.InternalAbstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Newtonsoft.Json.Linq;
using Npgsql;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Tests.Integration;
using Tests.Integration.Model;
using Tests.Integration.Model.Mocks;

namespace Tests.Ingest
{
    [Explicit]
    class LoadAnsibleInventoryTest
    {
        private static string GetFilepath(string filename)
        {
            string startupPath = ApplicationEnvironment.ApplicationBasePath;
            var pathItems = startupPath.Split(Path.DirectorySeparatorChar);
            var pos = pathItems.Reverse().ToList().FindIndex(x => string.Equals("bin", x));
            string projectPath = String.Join(Path.DirectorySeparatorChar.ToString(), pathItems.Take(pathItems.Length - pos - 1));
            return Path.Combine(projectPath, "Ingest", "ansible-inventory-scan", filename);
        }

        private static string LoadFile(string filename)
        {
            var path = GetFilepath(filename);
            string strText = File.ReadAllText(path, System.Text.Encoding.UTF8);
            return strText;
        }

        [SetUp]
        public void Setup()
        {
            DBSetup.Setup();
        }

        [Test]
        public async Task Test()
        {
            var username = "testUser";
            var displayName = username;
            var userGUID = new Guid("7dc848b7-881d-4785-9f25-985e9b6f2715");

            var dbcb = new DBConnectionBuilder();
            using var conn = dbcb.Build(DBSetup.dbName, false, true);
            //using var conn = dbcb.Build("landscape_prototype", false, true);
            var attributeModel = new AttributeModel(new BaseAttributeModel(conn));
            var layerModel = new LayerModel(conn);
            var userModel = new UserInDatabaseModel(conn);
            var ciModel = new CIModel(attributeModel, conn);
            var predicateModel = new PredicateModel(conn);
            var relationModel = new RelationModel(new BaseRelationModel(predicateModel, conn));
            var ingestDataService = new IngestDataService(attributeModel, ciModel, new ChangesetModel(userModel, conn), relationModel, conn);

            Layer layer1 = await layerModel.CreateLayer("Inventory Scan", null);
            
            // mock the current user service
            var mockCurrentUserService = new Mock<ICurrentUserService>();
            var user = User.Build(await userModel.UpsertUser(username, displayName, userGUID, UserType.Robot, null), new List<Layer>() { layer1 });
            mockCurrentUserService.Setup(_ => _.GetCurrentUser(It.IsAny<NpgsqlTransaction>())).ReturnsAsync(user);

            var mockAuthorizationService = new Mock<IRegistryAuthorizationService>();
            mockAuthorizationService.Setup(_ => _.CanUserWriteToLayer(user, layer1)).Returns(true);

            var insertLayer = layer1;
            var hosts = new string[] { "h1jmplx01.mhx.at", "h1lscapet01.mhx.local" };
            var layerSet = await layerModel.BuildLayerSet(null);

            await predicateModel.InsertOrUpdate("has_network_interface", "has network interface", "is network interface of host", AnchorState.Active, PredicateModel.DefaultConstraits, null);
            await predicateModel.InsertOrUpdate("has_mounted_device", "has mounted device", "is mounted at host", AnchorState.Active, PredicateModel.DefaultConstraits, null);

            var controller = new AnsibleIngestController(ingestDataService, layerModel, mockCurrentUserService.Object, mockAuthorizationService.Object, NullLogger<AnsibleIngestController>.Instance);

            var response = await PerformIngest(controller, hosts, insertLayer, layerSet);
            Assert.IsTrue(response is OkResult);

            var cis = await ciModel.GetMergedCIs(layerSet, new AllCIIDsSelection(), false, null, TimeThreshold.BuildLatest());
            Assert.That(cis.Select(ci => ci.Name), Is.SupersetOf(hosts));
            Assert.IsTrue(cis.Any(ci => ci.Name == "h1jmplx01.mhx.at:/"));
            Assert.IsTrue(cis.Any(ci => ci.Name == "h1jmplx01.mhx.at:/boot"));
            Assert.IsTrue(cis.Any(ci => ci.Name == "Network Interface lo@h1jmplx01.mhx.at"));
            Assert.IsTrue(cis.Any(ci => ci.Name == "h1lscapet01.mhx.local:/"));
            Assert.IsTrue(cis.Any(ci => ci.Name == "h1lscapet01.mhx.local:/boot"));
            Assert.IsTrue(cis.Any(ci => ci.Name == "Network Interface eth0@h1lscapet01.mhx.local"));
            Assert.AreEqual(34, cis.Count());
            // TODO: more asserts

            // perform ingest again, ci count must stay equal
            var response2 = await PerformIngest(controller, hosts, insertLayer, layerSet);
            Assert.IsTrue(response2 is OkResult);
            var cis2 = await ciModel.GetMergedCIs(layerSet, new AllCIIDsSelection(), false, null, TimeThreshold.BuildLatest());
            Assert.AreEqual(34, cis2.Count());
        }

        private async Task<ActionResult> PerformIngest(AnsibleIngestController controller, string[] hosts, Layer insertLayer, LayerSet searchLayerSet)
        {
            var setupFacts = hosts.ToDictionary(fqdn => fqdn, fqdn =>
            {
                var f = LoadFile($"{fqdn}\\setup_facts.json");
                var jo = JObject.Parse(f);
                return jo;
            });

            var response = await controller.IngestAnsibleInventoryScan(insertLayer.ID, searchLayerSet.LayerIDs, new Landscape.Base.Entity.DTO.Ingest.AnsibleInventoryScanDTO()
            {
                SetupFacts = setupFacts,
                YumInstalled = new Dictionary<string, JObject>() { },
                YumRepos = new Dictionary<string, JObject>() { },
                YumUpdates = new Dictionary<string, JObject>() { },
            });
            return response;
        }
    }
}
