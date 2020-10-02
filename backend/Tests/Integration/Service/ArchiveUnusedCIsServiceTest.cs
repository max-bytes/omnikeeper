using Landscape.Base.Inbound;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using LandscapeRegistry.Entity.AttributeValues;
using LandscapeRegistry.Model;
using LandscapeRegistry.Service;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Integration.Service
{
    class ArchiveUnusedCIsServiceTest
    {
        private NpgsqlConnection conn;

        [SetUp]
        public void Setup()
        {
            DBSetup.Setup();

            var dbcb = new DBConnectionBuilder();
            conn = dbcb.Build(DBSetup.dbName, false, true);

        }

        [TearDown]
        public void TearDown()
        {
            conn.Close();
        }

        [Test]
        public async Task TestArchiveUnusedCIs()
        {
            var attributeModel = new AttributeModel(new BaseAttributeModel(conn));
            var userModel = new UserInDatabaseModel(conn);
            var changesetModel = new ChangesetModel(userModel, conn);
            var model = new CIModel(attributeModel, conn);
            var layerModel = new LayerModel(conn);
            var user = await DBSetup.SetupUser(userModel);
            var e = new ExternalIDMapPostgresPersister();
            var p = new ScopedExternalIDMapPostgresPersister("tmp", e);

            var layer = await layerModel.CreateLayer("l1", null);

            Assert.AreEqual(0, await ArchiveUnusedCIsService.ArchiveUnusedCIs(e, conn, NullLogger.Instance));

            var ciid1 = await model.CreateCI(null);

            Assert.AreEqual(1, await ArchiveUnusedCIsService.ArchiveUnusedCIs(e, conn, NullLogger.Instance));
            Assert.AreEqual(0, await ArchiveUnusedCIsService.ArchiveUnusedCIs(e, conn, NullLogger.Instance));

            var ciid2 = await model.CreateCI(null);
            var changeset1 = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);
            await attributeModel.InsertAttribute("foo", AttributeScalarValueText.Build("bar"), ciid2, layer.ID, changeset1, null);

            Assert.AreEqual(0, await ArchiveUnusedCIsService.ArchiveUnusedCIs(e, conn, NullLogger.Instance));

            // create another empty ci
            var ciid3 = await model.CreateCI(null);
            Assert.AreEqual(1, await ArchiveUnusedCIsService.ArchiveUnusedCIs(e, conn, NullLogger.Instance));
            Assert.AreEqual(1, (await model.GetCIIDs(null)).Count());
            Assert.IsTrue((await model.GetCIIDs(null)).First() == ciid2);
            Assert.AreEqual(0, await ArchiveUnusedCIsService.ArchiveUnusedCIs(e, conn, NullLogger.Instance));

            // test archiving unused CIs with external id mapping present
            var ciid4 = await model.CreateCI(null);
            await p.Persist(new Dictionary<Guid, string>() { { ciid4, "tmp" } }, conn, null);
            Assert.AreEqual(0, await ArchiveUnusedCIsService.ArchiveUnusedCIs(e, conn, NullLogger.Instance));
            await p.Persist(new Dictionary<Guid, string>() { }, conn, null);
            Assert.AreEqual(1, await ArchiveUnusedCIsService.ArchiveUnusedCIs(e, conn, NullLogger.Instance));
        }

    }
}
