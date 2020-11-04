using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.Model.Decorators;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using Npgsql;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Integration.Model
{
    class CachingBaseAttributeModelTest
    {
        public static readonly Guid ciid1 = Guid.NewGuid();
        public static readonly Guid ciid2 = Guid.NewGuid();

        class EmptyMockedBaseAttributeModel : Mock<IBaseAttributeModel>
        {
            public EmptyMockedBaseAttributeModel()
            {
                Setup(_ => _.GetAttributes(It.IsAny<ICIIDSelection>(), It.IsAny<long>(), It.IsAny<NpgsqlTransaction>(), It.IsAny<TimeThreshold>())).ReturnsAsync(() =>
                {
                    return new List<CIAttribute>();
                });
            }
        }


        [Test]
        public async Task GetAttributesSingleCIIDSelection()
        {
            var mocked = new EmptyMockedBaseAttributeModel();
            var attributeModel = new CachingBaseAttributeModel(mocked.Object, new MemoryCache(Options.Create(new MemoryCacheOptions())));

            var layerID = 1L;
            var timeThreshold = TimeThreshold.BuildLatest();
            await attributeModel.GetAttributes(SpecificCIIDsSelection.Build(ciid1), layerID, null, timeThreshold);
            mocked.Verify(mock => mock.GetAttributes(It.Is<SpecificCIIDsSelection>(s => s.CIIDs.First() == ciid1), It.IsAny<long>(), It.IsAny<NpgsqlTransaction>(), It.IsAny<TimeThreshold>()), Times.Once());

            await attributeModel.GetAttributes(SpecificCIIDsSelection.Build(ciid1), layerID, null, timeThreshold);
            mocked.Verify(mock => mock.GetAttributes(It.Is<SpecificCIIDsSelection>(s => s.CIIDs.First() == ciid1), It.IsAny<long>(), It.IsAny<NpgsqlTransaction>(), It.IsAny<TimeThreshold>()), Times.Once()); // still one

            await attributeModel.GetAttributes(SpecificCIIDsSelection.Build(ciid2), layerID, null, timeThreshold);
            mocked.Verify(mock => mock.GetAttributes(It.Is<SpecificCIIDsSelection>(s => s.CIIDs.First() == ciid2), It.IsAny<long>(), It.IsAny<NpgsqlTransaction>(), It.IsAny<TimeThreshold>()), Times.Once()); // different parameters

            await attributeModel.GetAttributes(SpecificCIIDsSelection.Build(ciid2), layerID, null, TimeThreshold.BuildAtTime(DateTimeOffset.Now.AddDays(-1)));
            mocked.Verify(mock => mock.GetAttributes(It.Is<SpecificCIIDsSelection>(s => s.CIIDs.First() == ciid2), It.IsAny<long>(), It.IsAny<NpgsqlTransaction>(), It.IsAny<TimeThreshold>()), Times.Exactly(2)); // not latest time
        }


        class FilledMockedBaseAttributeModel : Mock<IBaseAttributeModel>
        {
            public FilledMockedBaseAttributeModel()
            {
                Guid staticChangesetID = Guid.NewGuid();
                Setup(_ => _.GetAttributes(It.Is<SpecificCIIDsSelection>(s => Enumerable.SequenceEqual(new Guid[] { ciid1, ciid2 }, s.CIIDs)), It.IsAny<long>(), It.IsAny<NpgsqlTransaction>(), It.IsAny<TimeThreshold>())).ReturnsAsync(() =>
                {
                    return new List<CIAttribute>()
                    {
                        CIAttribute.Build(Guid.NewGuid(), "a1", ciid1, AttributeScalarValueText.BuildFromString("v1"), AttributeState.New, staticChangesetID),
                        CIAttribute.Build(Guid.NewGuid(), "a2", ciid2, AttributeScalarValueText.BuildFromString("v2"), AttributeState.New, staticChangesetID)
                    };
                });
                Setup(_ => _.GetAttributes(It.Is<SpecificCIIDsSelection>(s => Enumerable.SequenceEqual(new Guid[] { ciid1 }, s.CIIDs)), It.IsAny<long>(), It.IsAny<NpgsqlTransaction>(), It.IsAny<TimeThreshold>())).ReturnsAsync(() =>
                {
                    return new List<CIAttribute>()
                    {
                        CIAttribute.Build(Guid.NewGuid(), "a1", ciid1, AttributeScalarValueText.BuildFromString("v1"), AttributeState.New, staticChangesetID)
                    };
                });
                Setup(_ => _.GetAttributes(It.Is<SpecificCIIDsSelection>(s => Enumerable.SequenceEqual(new Guid[] { ciid2 }, s.CIIDs)), It.IsAny<long>(), It.IsAny<NpgsqlTransaction>(), It.IsAny<TimeThreshold>())).ReturnsAsync(() =>
                {
                    return new List<CIAttribute>()
                    {
                        CIAttribute.Build(Guid.NewGuid(), "a2", ciid2, AttributeScalarValueText.BuildFromString("v2"), AttributeState.New, staticChangesetID)
                    };
                });
            }
        }

        [Test]
        public async Task GetAttributesMultiCIIDSelection()
        {
            async Task testBasic(Mock<IBaseAttributeModel> mocked) {
                var memoryCache = new MemoryCache(Options.Create(new MemoryCacheOptions()));
                var attributeModel = new CachingBaseAttributeModel(mocked.Object, memoryCache);

                var layerID = 1L;
                var timeThreshold = TimeThreshold.BuildLatest();
                await attributeModel.GetAttributes(SpecificCIIDsSelection.Build(new Guid[] { ciid1, ciid2 }), layerID, null, timeThreshold);
                mocked.Verify(mock => mock.GetAttributes(It.Is<SpecificCIIDsSelection>(s => Enumerable.SequenceEqual(new Guid[] { ciid1, ciid2 }, s.CIIDs)), It.IsAny<long>(), It.IsAny<NpgsqlTransaction>(), It.IsAny<TimeThreshold>()), Times.Once());
                await attributeModel.GetAttributes(SpecificCIIDsSelection.Build(new Guid[] { ciid1, ciid2 }), layerID, null, timeThreshold);
                mocked.Verify(mock => mock.GetAttributes(It.Is<SpecificCIIDsSelection>(s => Enumerable.SequenceEqual(new Guid[] { ciid1, ciid2 }, s.CIIDs)), It.IsAny<long>(), It.IsAny<NpgsqlTransaction>(), It.IsAny<TimeThreshold>()), Times.Once()); // still one
            }

            await testBasic(new EmptyMockedBaseAttributeModel());
            await testBasic(new FilledMockedBaseAttributeModel());
        }

        [Test]
        public async Task GetAttributesMultiCIIDSelectionPartial()
        {
            var mocked = new FilledMockedBaseAttributeModel();
            var memoryCache = new MemoryCache(Options.Create(new MemoryCacheOptions()));
            var attributeModel = new CachingBaseAttributeModel(mocked.Object, memoryCache);
            var layerID = 1L;
            var timeThreshold = TimeThreshold.BuildLatest();
            await attributeModel.GetAttributes(SpecificCIIDsSelection.Build(new Guid[] { ciid1
        }), layerID, null, timeThreshold);
                mocked.Verify(mock => mock.GetAttributes(It.Is<SpecificCIIDsSelection>(s => Enumerable.SequenceEqual(new Guid[] { ciid1 }, s.CIIDs)), It.IsAny<long>(), It.IsAny<NpgsqlTransaction>(), It.IsAny<TimeThreshold>()), Times.Once());
                await attributeModel.GetAttributes(SpecificCIIDsSelection.Build(new Guid[] { ciid1, ciid2 }), layerID, null, timeThreshold);
            mocked.Verify(mock => mock.GetAttributes(It.Is<SpecificCIIDsSelection>(s => Enumerable.SequenceEqual(new Guid[] { ciid1, ciid2 }, s.CIIDs)), It.IsAny<long>(), It.IsAny<NpgsqlTransaction>(), It.IsAny<TimeThreshold>()), Times.Never());
            mocked.Verify(mock => mock.GetAttributes(It.Is<SpecificCIIDsSelection>(s => Enumerable.SequenceEqual(new Guid[] { ciid2 }, s.CIIDs)), It.IsAny<long>(), It.IsAny<NpgsqlTransaction>(), It.IsAny<TimeThreshold>()), Times.Once());
        }
    }
}
