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
            var p = new Predicate("id", "wordingFrom", "wordingTo", AnchorState.Deprecated, new PredicateConstraints(new string[] { "ptt1", "ptt2" }, new string[] { "ptf1", "ptf2" }));
            var a = new Relation(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), p, RelationState.Removed, Guid.NewGuid(), new DataOriginV1(DataOriginType.Generator));
            var b = dataSerializer.ToByteArray(a);
            dataSerializer.FromByteArray<Relation>(b).Should().BeEquivalentTo(a);
        }
    }
}
