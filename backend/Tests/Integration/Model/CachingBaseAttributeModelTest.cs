using Landscape.Base.Entity;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using LandscapeRegistry.Entity.AttributeValues;
using LandscapeRegistry.Model.Decorators;
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
            await attributeModel.GetAttributes(new SingleCIIDSelection(ciid1), layerID, null, timeThreshold);
            mocked.Verify(mock => mock.GetAttributes(It.Is<SingleCIIDSelection>(s => s.CIID == ciid1), It.IsAny<long>(), It.IsAny<NpgsqlTransaction>(), It.IsAny<TimeThreshold>()), Times.Once());

            await attributeModel.GetAttributes(new SingleCIIDSelection(ciid1), layerID, null, timeThreshold);
            mocked.Verify(mock => mock.GetAttributes(It.Is<SingleCIIDSelection>(s => s.CIID == ciid1), It.IsAny<long>(), It.IsAny<NpgsqlTransaction>(), It.IsAny<TimeThreshold>()), Times.Once()); // still one

            await attributeModel.GetAttributes(new SingleCIIDSelection(ciid2), layerID, null, timeThreshold);
            mocked.Verify(mock => mock.GetAttributes(It.Is<SingleCIIDSelection>(s => s.CIID == ciid2), It.IsAny<long>(), It.IsAny<NpgsqlTransaction>(), It.IsAny<TimeThreshold>()), Times.Once()); // different parameters

            await attributeModel.GetAttributes(new SingleCIIDSelection(ciid2), layerID, null, TimeThreshold.BuildAtTime(DateTimeOffset.Now.AddDays(-1)));
            mocked.Verify(mock => mock.GetAttributes(It.Is<SingleCIIDSelection>(s => s.CIID == ciid2), It.IsAny<long>(), It.IsAny<NpgsqlTransaction>(), It.IsAny<TimeThreshold>()), Times.Exactly(2)); // not latest time
        }


        class FilledMockedBaseAttributeModel : Mock<IBaseAttributeModel>
        {
            public FilledMockedBaseAttributeModel()
            {
                Setup(_ => _.GetAttributes(It.Is<MultiCIIDsSelection>(s => Enumerable.SequenceEqual(new Guid[] { ciid1, ciid2 }, s.CIIDs)), It.IsAny<long>(), It.IsAny<NpgsqlTransaction>(), It.IsAny<TimeThreshold>())).ReturnsAsync(() =>
                {
                    return new List<CIAttribute>()
                    {
                        CIAttribute.Build(Guid.NewGuid(), "a1", ciid1, AttributeScalarValueText.Build("v1"), AttributeState.New, 1L),
                        CIAttribute.Build(Guid.NewGuid(), "a2", ciid2, AttributeScalarValueText.Build("v2"), AttributeState.New, 1L)
                    };
                });
                Setup(_ => _.GetAttributes(It.Is<MultiCIIDsSelection>(s => Enumerable.SequenceEqual(new Guid[] { ciid1 }, s.CIIDs)), It.IsAny<long>(), It.IsAny<NpgsqlTransaction>(), It.IsAny<TimeThreshold>())).ReturnsAsync(() =>
                {
                    return new List<CIAttribute>()
                    {
                        CIAttribute.Build(Guid.NewGuid(), "a1", ciid1, AttributeScalarValueText.Build("v1"), AttributeState.New, 1L)
                    };
                });
                Setup(_ => _.GetAttributes(It.Is<MultiCIIDsSelection>(s => Enumerable.SequenceEqual(new Guid[] { ciid2 }, s.CIIDs)), It.IsAny<long>(), It.IsAny<NpgsqlTransaction>(), It.IsAny<TimeThreshold>())).ReturnsAsync(() =>
                {
                    return new List<CIAttribute>()
                    {
                        CIAttribute.Build(Guid.NewGuid(), "a2", ciid2, AttributeScalarValueText.Build("v2"), AttributeState.New, 1L)
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
                await attributeModel.GetAttributes(MultiCIIDsSelection.Build(new Guid[] { ciid1, ciid2 }), layerID, null, timeThreshold);
                mocked.Verify(mock => mock.GetAttributes(It.Is<MultiCIIDsSelection>(s => Enumerable.SequenceEqual(new Guid[] { ciid1, ciid2 }, s.CIIDs)), It.IsAny<long>(), It.IsAny<NpgsqlTransaction>(), It.IsAny<TimeThreshold>()), Times.Once());
                await attributeModel.GetAttributes(MultiCIIDsSelection.Build(new Guid[] { ciid1, ciid2 }), layerID, null, timeThreshold);
                mocked.Verify(mock => mock.GetAttributes(It.Is<MultiCIIDsSelection>(s => Enumerable.SequenceEqual(new Guid[] { ciid1, ciid2 }, s.CIIDs)), It.IsAny<long>(), It.IsAny<NpgsqlTransaction>(), It.IsAny<TimeThreshold>()), Times.Once()); // still one
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
            await attributeModel.GetAttributes(MultiCIIDsSelection.Build(new Guid[] { ciid1
        }), layerID, null, timeThreshold);
                mocked.Verify(mock => mock.GetAttributes(It.Is<MultiCIIDsSelection>(s => Enumerable.SequenceEqual(new Guid[] { ciid1 }, s.CIIDs)), It.IsAny<long>(), It.IsAny<NpgsqlTransaction>(), It.IsAny<TimeThreshold>()), Times.Once());
                await attributeModel.GetAttributes(MultiCIIDsSelection.Build(new Guid[] { ciid1, ciid2 }), layerID, null, timeThreshold);
            mocked.Verify(mock => mock.GetAttributes(It.Is<MultiCIIDsSelection>(s => Enumerable.SequenceEqual(new Guid[] { ciid1, ciid2 }, s.CIIDs)), It.IsAny<long>(), It.IsAny<NpgsqlTransaction>(), It.IsAny<TimeThreshold>()), Times.Never());
            mocked.Verify(mock => mock.GetAttributes(It.Is<MultiCIIDsSelection>(s => Enumerable.SequenceEqual(new Guid[] { ciid2 }, s.CIIDs)), It.IsAny<long>(), It.IsAny<NpgsqlTransaction>(), It.IsAny<TimeThreshold>()), Times.Once());
        }
    }
}
