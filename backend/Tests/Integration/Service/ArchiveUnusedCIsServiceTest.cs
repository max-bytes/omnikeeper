using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Omnikeeper.Base.Entity.DataOrigin;
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
            var trans = ModelContextBuilder.BuildImmediate();

            var (layer, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("l1", trans);

            Assert.AreEqual(0, await ArchiveUnusedCIsService.ArchiveUnusedCIs(ModelContextBuilder, NullLogger.Instance));

            var ciid1 = await GetService<ICIModel>().CreateCI(trans);

            Assert.AreEqual(1, await ArchiveUnusedCIsService.ArchiveUnusedCIs(ModelContextBuilder, NullLogger.Instance));
            Assert.AreEqual(0, await ArchiveUnusedCIsService.ArchiveUnusedCIs(ModelContextBuilder, NullLogger.Instance));

            var ciid2 = await GetService<ICIModel>().CreateCI(trans);
            var changeset1 = await CreateChangesetProxy();
            await GetService<IAttributeModel>().InsertAttribute("foo", new AttributeScalarValueText("bar"), ciid2, layer.ID, changeset1, trans, OtherLayersValueHandlingForceWrite.Instance);

            Assert.AreEqual(0, await ArchiveUnusedCIsService.ArchiveUnusedCIs(ModelContextBuilder, NullLogger.Instance));

            // create another empty ci
            var ciid3 = await GetService<ICIModel>().CreateCI(trans);
            Assert.AreEqual(1, await ArchiveUnusedCIsService.ArchiveUnusedCIs(ModelContextBuilder, NullLogger.Instance));
            Assert.AreEqual(1, (await GetService<ICIModel>().GetCIIDs(trans)).Count());
            Assert.IsTrue((await GetService<ICIModel>().GetCIIDs(trans)).First() == ciid2);
            Assert.AreEqual(0, await ArchiveUnusedCIsService.ArchiveUnusedCIs(ModelContextBuilder, NullLogger.Instance));
        }

    }
}
