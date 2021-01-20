using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.Model;
using Omnikeeper.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tests.Integration.Service
{
    class ArchiveUnusedCIsServiceTest : DBBackedTestBase
    {
        [Test]
        public async Task TestArchiveUnusedCIs()
        {
            var attributeModel = new AttributeModel(new BaseAttributeModel(new PartitionModel()));
            var userModel = new UserInDatabaseModel();
            var changesetModel = new ChangesetModel(userModel);
            var model = new CIModel(attributeModel, new CIIDModel());
            var layerModel = new LayerModel();
            var e = new ExternalIDMapPostgresPersister();
            var p = new ScopedExternalIDMapPostgresPersister("tmp", e);

            var trans = ModelContextBuilder.BuildImmediate();
            var user = await DBSetup.SetupUser(userModel, trans);

            var layer = await layerModel.CreateLayer("l1", trans);

            Assert.AreEqual(0, await ArchiveUnusedCIsService.ArchiveUnusedCIs(e, ModelContextBuilder, NullLogger.Instance));

            var ciid1 = await model.CreateCI(trans);

            Assert.AreEqual(1, await ArchiveUnusedCIsService.ArchiveUnusedCIs(e, ModelContextBuilder, NullLogger.Instance));
            Assert.AreEqual(0, await ArchiveUnusedCIsService.ArchiveUnusedCIs(e, ModelContextBuilder, NullLogger.Instance));

            var ciid2 = await model.CreateCI(trans);
            var changeset1 = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
            await attributeModel.InsertAttribute("foo", new AttributeScalarValueText("bar"), ciid2, layer.ID, changeset1, new DataOriginV1(DataOriginType.Manual), trans);

            Assert.AreEqual(0, await ArchiveUnusedCIsService.ArchiveUnusedCIs(e, ModelContextBuilder, NullLogger.Instance));

            // create another empty ci
            var ciid3 = await model.CreateCI(trans);
            Assert.AreEqual(1, await ArchiveUnusedCIsService.ArchiveUnusedCIs(e, ModelContextBuilder, NullLogger.Instance));
            Assert.AreEqual(1, (await model.GetCIIDs(trans)).Count());
            Assert.IsTrue((await model.GetCIIDs(trans)).First() == ciid2);
            Assert.AreEqual(0, await ArchiveUnusedCIsService.ArchiveUnusedCIs(e, ModelContextBuilder, NullLogger.Instance));

            // test archiving unused CIs with external id mapping present
            var ciid4 = await model.CreateCI(trans);
            await p.Persist(new Dictionary<Guid, string>() { { ciid4, "tmp" } }, trans);
            Assert.AreEqual(0, await ArchiveUnusedCIsService.ArchiveUnusedCIs(e, ModelContextBuilder, NullLogger.Instance));
            await p.Persist(new Dictionary<Guid, string>() { }, trans);
            Assert.AreEqual(1, await ArchiveUnusedCIsService.ArchiveUnusedCIs(e, ModelContextBuilder, NullLogger.Instance));
        }

    }
}
