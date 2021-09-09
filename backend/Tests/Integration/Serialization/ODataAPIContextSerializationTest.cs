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
            var a = new ODataAPIContext("id", new ODataAPIContext.ConfigV3("123", new string[] { "923", "45" }));
            var b = dataSerializer.ToByteArray(a);
            dataSerializer.FromByteArray<ODataAPIContext>(b).Should().BeEquivalentTo(a, options => options.WithStrictOrdering());
        }
    }
}
