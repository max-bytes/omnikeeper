using FluentAssertions;
using NUnit.Framework;
using Omnikeeper.Base.Entity.Config;
using Omnikeeper.Base.Utils.Serialization;
using System;

namespace Tests.Integration.Serialization
{
    class BaseConfigurationSerializationTest
    {
        [Test]
        public void TestSerialization()
        {
            var dataSerializer = new ProtoBufDataSerializer();
            var a = new BaseConfigurationV1(new TimeSpan(97202), "clbRunnerInterval", "markedForDeletionRunnerInterval", "externalIDManagerRunnerInterval", "archiveOldDataRunnerInterval", new long[] { 1L,2L }, 1L);
            var b = dataSerializer.ToByteArray(a);
            dataSerializer.FromByteArray<BaseConfigurationV1>(b).Should().BeEquivalentTo(a);
        }
    }
}
