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
            var a = new BaseConfigurationV2(new TimeSpan(97202), "clbRunnerInterval", "markedForDeletionRunnerInterval", "externalIDManagerRunnerInterval", "archiveOldDataRunnerInterval");
            var b = dataSerializer.ToByteArray(a);
            dataSerializer.FromByteArray<BaseConfigurationV2>(b).Should().BeEquivalentTo(a, options => options.WithStrictOrdering());
        }
    }
}
