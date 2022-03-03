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
    class ArchiveUnusedCIsServiceTest : DIServicedTestBase
    {
        [Test]
        public async Task TestArchiveUnusedCIs()
        {
            var e = new ExternalIDMapPostgresPersister();
            var p = new ScopedExternalIDMapPostgresPersister("tmp", e);

            var trans = ModelContextBuilder.BuildImmediate();

            var (layer, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("l1", trans);

            Assert.AreEqual(0, await ArchiveUnusedCIsService.ArchiveUnusedCIs(e, ModelContextBuilder, NullLogger.Instance));

            var ciid1 = await GetService<ICIModel>().CreateCI(trans);

            Assert.AreEqual(1, await ArchiveUnusedCIsService.ArchiveUnusedCIs(e, ModelContextBuilder, NullLogger.Instance));
            Assert.AreEqual(0, await ArchiveUnusedCIsService.ArchiveUnusedCIs(e, ModelContextBuilder, NullLogger.Instance));

            var ciid2 = await GetService<ICIModel>().CreateCI(trans);
            var changeset1 = await CreateChangesetProxy();
            await GetService<IAttributeModel>().InsertAttribute("foo", new AttributeScalarValueText("bar"), ciid2, layer.ID, changeset1, new DataOriginV1(DataOriginType.Manual), trans, OtherLayersValueHandlingForceWrite.Instance);

            Assert.AreEqual(0, await ArchiveUnusedCIsService.ArchiveUnusedCIs(e, ModelContextBuilder, NullLogger.Instance));

            // create another empty ci
            var ciid3 = await GetService<ICIModel>().CreateCI(trans);
            Assert.AreEqual(1, await ArchiveUnusedCIsService.ArchiveUnusedCIs(e, ModelContextBuilder, NullLogger.Instance));
            Assert.AreEqual(1, (await GetService<ICIModel>().GetCIIDs(trans)).Count());
            Assert.IsTrue((await GetService<ICIModel>().GetCIIDs(trans)).First() == ciid2);
            Assert.AreEqual(0, await ArchiveUnusedCIsService.ArchiveUnusedCIs(e, ModelContextBuilder, NullLogger.Instance));

            // test archiving unused CIs with external id mapping present
            var ciid4 = await GetService<ICIModel>().CreateCI(trans);
            await p.Persist(new Dictionary<Guid, string>() { { ciid4, "tmp" } }, trans);
            Assert.AreEqual(0, await ArchiveUnusedCIsService.ArchiveUnusedCIs(e, ModelContextBuilder, NullLogger.Instance));
            await p.Persist(new Dictionary<Guid, string>() { }, trans);
            Assert.AreEqual(1, await ArchiveUnusedCIsService.ArchiveUnusedCIs(e, ModelContextBuilder, NullLogger.Instance));
        }

    }
}
