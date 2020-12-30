using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.Model;
using Omnikeeper.Service;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tests.Integration.Model;
using Omnikeeper.Base.Entity.DataOrigin;
using Microsoft.Extensions.DependencyInjection;
using Omnikeeper.Base.Service;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace Tests.Integration.Service
{
    class DataPartitionServiceTest : DIServicedTestBase
    {
        public DataPartitionServiceTest() : base(true)
        {
        }

        [Test]
        public async Task TestBasic()
        {
            await ExampleDataSetup.SetupCMDBExampleData(10, 2, 5000, 10, true, ServiceProvider, ModelContextBuilder);

            var dataPartitionService = ServiceProvider.GetRequiredService<IDataPartitionService>();
            var attributeModel = ServiceProvider.GetRequiredService<IAttributeModel>();
            var relationModel = ServiceProvider.GetRequiredService<IRelationModel>();

            var attributesBefore = await attributeModel.GetMergedAttributes(new AllCIIDsSelection(), new Omnikeeper.Base.Entity.LayerSet(1,2), ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            var relationsBefore = await relationModel.GetMergedRelations(new RelationSelectionAll(), new Omnikeeper.Base.Entity.LayerSet(1, 2), ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());

            Assert.IsTrue(await dataPartitionService.StartNewPartition());

            var attributesAfter = await attributeModel.GetMergedAttributes(new AllCIIDsSelection(), new Omnikeeper.Base.Entity.LayerSet(1, 2), ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            var relationsAfter = await relationModel.GetMergedRelations(new RelationSelectionAll(), new Omnikeeper.Base.Entity.LayerSet(1, 2), ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());

            attributesAfter.Should().BeEquivalentTo(attributesBefore);
            relationsAfter.Should().BeEquivalentTo(relationsBefore);

            Thread.Sleep(1000); // need to sleep because partitioning service does not support performing a new partitioning in the same second

            Assert.IsTrue(await dataPartitionService.StartNewPartition());
        }

    }
}
