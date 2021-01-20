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
using Omnikeeper.Model.Decorators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tests.Integration.Model
{
    class CachingPartitionModelTest
    {
        private static readonly DateTimeOffset testPartitionIndex = new DateTimeOffset(2021, 1, 1, 12, 30, 45, TimeSpan.FromMinutes(60));

        class MockedPartitionModel : Mock<IPartitionModel>
        {
            public MockedPartitionModel()
            {
                Setup(_ => _.GetLatestPartitionIndex(It.IsAny<TimeThreshold>(), It.IsAny<IModelContext>())).ReturnsAsync(() =>
                {
                    return testPartitionIndex;
                });
            }
        }

        [Test]
        public async Task GetAttributesSingleCIIDSelection()
        {
            var mocked = new MockedPartitionModel();
            var partitionModel = new CachingPartitionModel(mocked.Object);

            var memoryCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
            var trans = new ModelContextImmediateMode(memoryCache, null!, NullLogger<IModelContext>.Instance, new ProtoBufDataSerializer());

            var timeThreshold = TimeThreshold.BuildLatest();
            Assert.AreEqual(testPartitionIndex, await partitionModel.GetLatestPartitionIndex(timeThreshold, trans));
            mocked.Verify(mock => mock.GetLatestPartitionIndex(It.IsIn(timeThreshold), It.IsIn(trans)), Times.Once());

            Assert.AreEqual(testPartitionIndex, await partitionModel.GetLatestPartitionIndex(timeThreshold, trans));
            mocked.Verify(mock => mock.GetLatestPartitionIndex(It.IsIn(timeThreshold), It.IsIn(trans)), Times.Once()); // still one
        }
    }
}
