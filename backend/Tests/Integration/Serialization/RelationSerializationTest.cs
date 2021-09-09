using FluentAssertions;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Utils.Serialization;
using System;

namespace Tests.Integration.Serialization
{
    class RelationSerializationTest
    {
        [Test]
        public void TestSerialization()
        {
            var dataSerializer = new ProtoBufDataSerializer();
            var a = new Relation(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "id", RelationState.Removed, Guid.NewGuid());
            var b = dataSerializer.ToByteArray(a);
            dataSerializer.FromByteArray<Relation>(b).Should().BeEquivalentTo(a, options => options.WithStrictOrdering());
        }
    }
}
