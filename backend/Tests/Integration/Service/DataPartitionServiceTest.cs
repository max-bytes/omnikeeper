using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using System.Threading;
using System.Threading.Tasks;

namespace Tests.Integration.Service
{
    class DataPartitionServiceTest : DIServicedTestBase
    {
        public DataPartitionServiceTest() : base(false, false)
        {
        }

        [Test]
        public async Task TestBasic()
        {
            await ExampleDataSetup.SetupCMDBExampleData(10, 2, 5000, 10, true, ServiceProvider, ModelContextBuilder);

            var dataPartitionService = ServiceProvider.GetRequiredService<IDataPartitionService>();
            var attributeModel = ServiceProvider.GetRequiredService<IAttributeModel>();
            var relationModel = ServiceProvider.GetRequiredService<IRelationModel>();

            var attributesBefore = await attributeModel.GetMergedAttributes(AllCIIDsSelection.Instance, AllAttributeSelection.Instance, new Omnikeeper.Base.Entity.LayerSet("1", "2"), ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest(), GeneratedDataHandlingInclude.Instance);
            var relationsBefore = await relationModel.GetMergedRelations(RelationSelectionAll.Instance, new Omnikeeper.Base.Entity.LayerSet("1", "2"), ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest(), MaskHandlingForRetrievalGetMasks.Instance, GeneratedDataHandlingInclude.Instance);

            Assert.IsTrue(await dataPartitionService.StartNewPartition());

            var attributesAfter = await attributeModel.GetMergedAttributes(AllCIIDsSelection.Instance, AllAttributeSelection.Instance, new Omnikeeper.Base.Entity.LayerSet("1", "2"), ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest(), GeneratedDataHandlingInclude.Instance);
            var relationsAfter = await relationModel.GetMergedRelations(RelationSelectionAll.Instance, new Omnikeeper.Base.Entity.LayerSet("1", "2"), ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest(), MaskHandlingForRetrievalGetMasks.Instance, GeneratedDataHandlingInclude.Instance);

            attributesAfter.Should().BeEquivalentTo(attributesBefore, options => options.WithStrictOrdering());
            relationsAfter.Should().BeEquivalentTo(relationsBefore, options => options.WithStrictOrdering());

            Thread.Sleep(1000); // need to sleep because partitioning service does not support performing a new partitioning in the same second

            Assert.IsTrue(await dataPartitionService.StartNewPartition());
        }

    }
}
