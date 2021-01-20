using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Base.Utils.Serialization;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.Model;
using Omnikeeper.Model.Decorators;
using System;
using System.Collections.Generic;
using System.Linq;
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
                Setup(_ => _.GetAttributes(It.IsAny<ICIIDSelection>(), It.IsAny<long>(), It.IsAny<IModelContext>(), It.IsAny<TimeThreshold>())).ReturnsAsync(() =>
                {
                    return new List<CIAttribute>();
                });
            }
        }

        class MockedCIIDModel : Mock<ICIIDModel>
        {
            public MockedCIIDModel()
            {
                Setup(_ => _.GetCIIDs(It.IsAny<IModelContext>())).ReturnsAsync(() =>
                {
                    return new List<Guid>() { ciid1, ciid2 };
                });
                Setup(_ => _.CIIDExists(It.IsAny<Guid>(), It.IsAny<IModelContext>())).ReturnsAsync((Guid guid, IModelContext mc) =>
                {
                    return guid == ciid1 || guid == ciid2;
                });
            }
        }
        private readonly MockedCIIDModel mockedCIIDModel = new MockedCIIDModel();

        [Test]
        public async Task GetAttributesSingleCIIDSelection()
        {
            var mocked = new EmptyMockedBaseAttributeModel();
            var attributeModel = new CachingBaseAttributeModel(mocked.Object, mockedCIIDModel.Object, NullLogger<CachingBaseAttributeModel>.Instance);

            var memoryCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
            var trans = new ModelContextImmediateMode(memoryCache, null!, NullLogger<IModelContext>.Instance, new ProtoBufDataSerializer());

            var layerID = 1L;
            var timeThreshold = TimeThreshold.BuildLatest();
            await attributeModel.GetAttributes(SpecificCIIDsSelection.Build(ciid1), layerID, trans, timeThreshold);
            mocked.Verify(mock => mock.GetAttributes(It.Is<SpecificCIIDsSelection>(s => s.CIIDs.First() == ciid1), It.IsAny<long>(), It.IsAny<IModelContext>(), It.IsAny<TimeThreshold>()), Times.Once());

            await attributeModel.GetAttributes(SpecificCIIDsSelection.Build(ciid1), layerID, trans, timeThreshold);
            mocked.Verify(mock => mock.GetAttributes(It.Is<SpecificCIIDsSelection>(s => s.CIIDs.First() == ciid1), It.IsAny<long>(), It.IsAny<IModelContext>(), It.IsAny<TimeThreshold>()), Times.Once()); // still one

            await attributeModel.GetAttributes(SpecificCIIDsSelection.Build(ciid2), layerID, trans, timeThreshold);
            mocked.Verify(mock => mock.GetAttributes(It.Is<SpecificCIIDsSelection>(s => s.CIIDs.First() == ciid2), It.IsAny<long>(), It.IsAny<IModelContext>(), It.IsAny<TimeThreshold>()), Times.Once()); // different parameters

            await attributeModel.GetAttributes(SpecificCIIDsSelection.Build(ciid2), layerID, trans, TimeThreshold.BuildAtTime(DateTimeOffset.Now.AddDays(-1)));
            mocked.Verify(mock => mock.GetAttributes(It.Is<SpecificCIIDsSelection>(s => s.CIIDs.First() == ciid2), It.IsAny<long>(), It.IsAny<IModelContext>(), It.IsAny<TimeThreshold>()), Times.Exactly(2)); // not latest time
        }


        [Test]
        public async Task IsCacheProperlyFilledAndEvicted()
        {
            var mocked = new FilledMockedBaseAttributeModel();
            mocked.Setup(_ => _.RemoveAttribute(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<IChangesetProxy>(), It.IsAny<IModelContext>())).ReturnsAsync(() =>
            {
                return (null!, true);
            });
            var attributeModel = new CachingBaseAttributeModel(mocked.Object, mockedCIIDModel.Object, NullLogger<CachingBaseAttributeModel>.Instance);

            var memoryCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
            var trans = new ModelContextImmediateMode(memoryCache, null!, NullLogger<IModelContext>.Instance, new ProtoBufDataSerializer());

            var layerID = 1L;
            var timeThreshold = TimeThreshold.BuildLatest();
            await attributeModel.GetAttributes(SpecificCIIDsSelection.Build(ciid1, ciid2), layerID, trans, timeThreshold);

            var cachedAttributes1 = trans.GetCachedValue<IEnumerable<CIAttribute>>(CacheKeyService.Attributes(ciid1, layerID));
            cachedAttributes1.Should().BeEquivalentTo(
                new List<CIAttribute>()
                    {
                        new CIAttribute(new Guid("82b59560-3870-42b5-9c8f-5c646f9d0740"), "a1", ciid1, new AttributeScalarValueText("v1"), AttributeState.New, new Guid("6c1457d9-1807-453d-acab-68cd62726f1a"), new DataOriginV1(DataOriginType.Manual)),
                    }
                );
            var cachedAttributes2 = trans.GetCachedValue<IEnumerable<CIAttribute>>(CacheKeyService.Attributes(ciid2, layerID));
            cachedAttributes2.Should().BeEquivalentTo(
                new List<CIAttribute>()
                    {
                        new CIAttribute(new Guid("82b59560-3870-42b5-9c8f-5c646f9d0741"), "a2", ciid2, new AttributeScalarValueText("v2"), AttributeState.New, new Guid("6c1457d9-1807-453d-acab-68cd62726f1a"), new DataOriginV1(DataOriginType.Manual))
                    }
                );

            // remove one attribute
            await attributeModel.RemoveAttribute("a1", ciid1, layerID, null!, trans);

            // ensure this attribute is evicted from cache, the other on (in different ci) still exists in cache
            var cachedAttributes21 = trans.GetCachedValue<IEnumerable<CIAttribute>>(CacheKeyService.Attributes(ciid1, layerID));
            Assert.IsNull(cachedAttributes21);
            var cachedAttributes22 = trans.GetCachedValue<IEnumerable<CIAttribute>>(CacheKeyService.Attributes(ciid2, layerID));
            cachedAttributes22.Should().BeEquivalentTo(
                new List<CIAttribute>()
                    {
                        new CIAttribute(new Guid("82b59560-3870-42b5-9c8f-5c646f9d0741"), "a2", ciid2, new AttributeScalarValueText("v2"), AttributeState.New, new Guid("6c1457d9-1807-453d-acab-68cd62726f1a"), new DataOriginV1(DataOriginType.Manual))
                    }
                );

            // fetch all cis, expecting both CIs and their attributes in the cache again
            var cachedAttributes3 = await attributeModel.GetAttributes(new AllCIIDsSelection(), layerID, trans, timeThreshold);
            Assert.AreEqual(2, cachedAttributes3.Count());
            var cachedAttributes31 = trans.GetCachedValue<IEnumerable<CIAttribute>>(CacheKeyService.Attributes(ciid1, layerID));
            cachedAttributes31.Should().BeEquivalentTo(
                new List<CIAttribute>()
                    {
                        new CIAttribute(new Guid("82b59560-3870-42b5-9c8f-5c646f9d0740"), "a1", ciid1, new AttributeScalarValueText("v1"), AttributeState.New, new Guid("6c1457d9-1807-453d-acab-68cd62726f1a"), new DataOriginV1(DataOriginType.Manual)),
                    }
                );
            var cachedAttributes32 = trans.GetCachedValue<IEnumerable<CIAttribute>>(CacheKeyService.Attributes(ciid2, layerID));
            cachedAttributes32.Should().BeEquivalentTo(
                new List<CIAttribute>()
                    {
                        new CIAttribute(new Guid("82b59560-3870-42b5-9c8f-5c646f9d0741"), "a2", ciid2, new AttributeScalarValueText("v2"), AttributeState.New, new Guid("6c1457d9-1807-453d-acab-68cd62726f1a"), new DataOriginV1(DataOriginType.Manual))
                    }
                );

        }


        class FilledMockedBaseAttributeModel : Mock<IBaseAttributeModel>
        {
            public FilledMockedBaseAttributeModel()
            {
                Guid staticChangesetID = new Guid("6c1457d9-1807-453d-acab-68cd62726f1a");
                Setup(_ => _.GetAttributes(It.Is<SpecificCIIDsSelection>(s => Enumerable.SequenceEqual(new Guid[] { ciid1, ciid2 }, s.CIIDs)), It.IsAny<long>(), It.IsAny<IModelContext>(), It.IsAny<TimeThreshold>())).ReturnsAsync(() =>
                {
                    return new List<CIAttribute>()
                    {
                        new CIAttribute(new Guid("82b59560-3870-42b5-9c8f-5c646f9d0740"), "a1", ciid1, new AttributeScalarValueText("v1"), AttributeState.New, staticChangesetID, new DataOriginV1(DataOriginType.Manual)),
                        new CIAttribute(new Guid("82b59560-3870-42b5-9c8f-5c646f9d0741"), "a2", ciid2, new AttributeScalarValueText("v2"), AttributeState.New, staticChangesetID, new DataOriginV1(DataOriginType.Manual))
                    };
                });
                Setup(_ => _.GetAttributes(It.Is<SpecificCIIDsSelection>(s => Enumerable.SequenceEqual(new Guid[] { ciid1 }, s.CIIDs)), It.IsAny<long>(), It.IsAny<IModelContext>(), It.IsAny<TimeThreshold>())).ReturnsAsync(() =>
                {
                    return new List<CIAttribute>()
                    {
                        new CIAttribute(new Guid("82b59560-3870-42b5-9c8f-5c646f9d0740"), "a1", ciid1, new AttributeScalarValueText("v1"), AttributeState.New, staticChangesetID, new DataOriginV1(DataOriginType.Manual))
                    };
                });
                Setup(_ => _.GetAttributes(It.Is<SpecificCIIDsSelection>(s => Enumerable.SequenceEqual(new Guid[] { ciid2 }, s.CIIDs)), It.IsAny<long>(), It.IsAny<IModelContext>(), It.IsAny<TimeThreshold>())).ReturnsAsync(() =>
                {
                    return new List<CIAttribute>()
                    {
                        new CIAttribute(new Guid("82b59560-3870-42b5-9c8f-5c646f9d0741"), "a2", ciid2, new AttributeScalarValueText("v2"), AttributeState.New, staticChangesetID, new DataOriginV1(DataOriginType.Manual))
                    };
                });
            }
        }

        [Test]
        public async Task GetAttributesMultiCIIDSelection()
        {
            async Task TestBasic(Mock<IBaseAttributeModel> mocked)
            {
                var attributeModel = new CachingBaseAttributeModel(mocked.Object, mockedCIIDModel.Object, NullLogger<CachingBaseAttributeModel>.Instance);
                var memoryCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
                var trans = new ModelContextImmediateMode(memoryCache, null!, NullLogger<IModelContext>.Instance, new ProtoBufDataSerializer());

                var layerID = 1L;
                var timeThreshold = TimeThreshold.BuildLatest();
                await attributeModel.GetAttributes(SpecificCIIDsSelection.Build(new Guid[] { ciid1, ciid2 }), layerID, trans, timeThreshold);
                mocked.Verify(mock => mock.GetAttributes(It.Is<SpecificCIIDsSelection>(s => Enumerable.SequenceEqual(new Guid[] { ciid1, ciid2 }, s.CIIDs)), It.IsAny<long>(), It.IsAny<IModelContext>(), It.IsAny<TimeThreshold>()), Times.Once());
                await attributeModel.GetAttributes(SpecificCIIDsSelection.Build(new Guid[] { ciid1, ciid2 }), layerID, trans, timeThreshold);
                mocked.Verify(mock => mock.GetAttributes(It.Is<SpecificCIIDsSelection>(s => Enumerable.SequenceEqual(new Guid[] { ciid1, ciid2 }, s.CIIDs)), It.IsAny<long>(), It.IsAny<IModelContext>(), It.IsAny<TimeThreshold>()), Times.Once()); // still one
            }

            await TestBasic(new EmptyMockedBaseAttributeModel());
            await TestBasic(new FilledMockedBaseAttributeModel());
        }

        [Test]
        public async Task GetAttributesMultiCIIDSelectionPartial()
        {
            var mocked = new FilledMockedBaseAttributeModel();
            var memoryCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
            var trans = new ModelContextImmediateMode(memoryCache, null!, NullLogger<IModelContext>.Instance, new ProtoBufDataSerializer());
            var attributeModel = new CachingBaseAttributeModel(mocked.Object, mockedCIIDModel.Object, NullLogger<CachingBaseAttributeModel>.Instance);
            var layerID = 1L;
            var timeThreshold = TimeThreshold.BuildLatest();
            await attributeModel.GetAttributes(SpecificCIIDsSelection.Build(new Guid[] { ciid1 }), layerID, trans, timeThreshold);
            mocked.Verify(mock => mock.GetAttributes(It.Is<SpecificCIIDsSelection>(s => Enumerable.SequenceEqual(new Guid[] { ciid1 }, s.CIIDs)), It.IsAny<long>(), It.IsAny<IModelContext>(), It.IsAny<TimeThreshold>()), Times.Once());
            await attributeModel.GetAttributes(SpecificCIIDsSelection.Build(new Guid[] { ciid1, ciid2 }), layerID, trans, timeThreshold);
            mocked.Verify(mock => mock.GetAttributes(It.Is<SpecificCIIDsSelection>(s => Enumerable.SequenceEqual(new Guid[] { ciid1, ciid2 }, s.CIIDs)), It.IsAny<long>(), It.IsAny<IModelContext>(), It.IsAny<TimeThreshold>()), Times.Never());
            mocked.Verify(mock => mock.GetAttributes(It.Is<SpecificCIIDsSelection>(s => Enumerable.SequenceEqual(new Guid[] { ciid2 }, s.CIIDs)), It.IsAny<long>(), It.IsAny<IModelContext>(), It.IsAny<TimeThreshold>()), Times.Once());
        }
    }
}
