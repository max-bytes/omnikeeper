using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Controllers.Ingest;
using Omnikeeper.Model;
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
using Omnikeeper.Base.Utils.ModelContext;

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
            string projectPath = string.Join(Path.DirectorySeparatorChar.ToString(), pathItems.Take(pathItems.Length - pos - 1));
            return Path.Combine(projectPath, "files", filename);
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
            var attributeModel = new AttributeModel(new BaseAttributeModel());
            var layerModel = new LayerModel();
            var userModel = new UserInDatabaseModel();
            var ciModel = new CIModel(attributeModel);
            var predicateModel = new PredicateModel();
            var relationModel = new RelationModel(new BaseRelationModel(predicateModel));
            var modelContextBuilder = new ModelContextBuilder(null, conn, NullLogger<IModelContext>.Instance);
            var ingestDataService = new IngestDataService(attributeModel, ciModel, new ChangesetModel(userModel), relationModel, new CIMappingService());

            var mc = modelContextBuilder.BuildImmediate();

            Layer layer1 = await layerModel.CreateLayer("Inventory Scan", mc);
            
            // mock the current user service
            var mockCurrentUserService = new Mock<ICurrentUserService>();
            var user = new AuthenticatedUser(await userModel.UpsertUser(username, displayName, userGUID, UserType.Robot, mc), new List<Layer>() { layer1 });
            mockCurrentUserService.Setup(_ => _.GetCurrentUser(It.IsAny<IModelContext>())).ReturnsAsync(user);

            var mockAuthorizationService = new Mock<ILayerBasedAuthorizationService>();
            mockAuthorizationService.Setup(_ => _.CanUserWriteToLayer(user, layer1)).Returns(true);

            var insertLayer = layer1;
            var hosts = new string[] { "h1jmplx01.mhx.at", "h1lscapet01.mhx.local" };
            var layerSet = await layerModel.BuildLayerSet(mc);

            await predicateModel.InsertOrUpdate("has_network_interface", "has network interface", "is network interface of host", AnchorState.Active, PredicateModel.DefaultConstraits, mc);
            await predicateModel.InsertOrUpdate("has_mounted_device", "has mounted device", "is mounted at host", AnchorState.Active, PredicateModel.DefaultConstraits, mc);

            var controller = new AnsibleInventoryScanIngestController(ingestDataService, layerModel, mockCurrentUserService.Object, modelContextBuilder, mockAuthorizationService.Object, NullLogger<AnsibleInventoryScanIngestController>.Instance);

            var response = await PerformIngest(controller, hosts, insertLayer, layerSet);
            Assert.IsTrue(response is OkResult);

            var cis = await ciModel.GetMergedCIs(new AllCIIDsSelection(), layerSet, false, mc, TimeThreshold.BuildLatest());
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
            var cis2 = await ciModel.GetMergedCIs(new AllCIIDsSelection(), layerSet, false, mc, TimeThreshold.BuildLatest());
            Assert.AreEqual(34, cis2.Count());
        }

        private async Task<ActionResult> PerformIngest(AnsibleInventoryScanIngestController controller, string[] hosts, Layer insertLayer, LayerSet searchLayerSet)
        {
            var setupFacts = hosts.ToDictionary(fqdn => fqdn, fqdn =>
            {
                var f = LoadFile($"{fqdn}\\setup_facts.json");
                var jo = JObject.Parse(f);
                return jo;
            });

            var response = await controller.IngestAnsibleInventoryScan(insertLayer.ID, searchLayerSet.LayerIDs, new Omnikeeper.Base.Entity.DTO.Ingest.AnsibleInventoryScanDTO(
                setupFacts,
                new Dictionary<string, JObject>() { },
                new Dictionary<string, JObject>() { },
                new Dictionary<string, JObject>() { }
            ));
            return response;
        }
    }
}
