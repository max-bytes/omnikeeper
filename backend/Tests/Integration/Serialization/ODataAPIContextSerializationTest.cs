using FluentAssertions;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Utils.Serialization;
using System;

namespace Tests.Integration.Serialization
{
    class ODataAPIContextSerializationTest
    {
        [Test]
        public void TestSerialization()
        {
            var dataSerializer = new ProtoBufDataSerializer();
            var a = new ODataAPIContext("id", new ODataAPIContext.ConfigV3(123L, new long[] { 923L, 45L }));
            var b = dataSerializer.ToByteArray(a);
            dataSerializer.FromByteArray<ODataAPIContext>(b).Should().BeEquivalentTo(a);
        }
    }
}
