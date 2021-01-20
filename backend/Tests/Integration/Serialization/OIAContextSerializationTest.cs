using FluentAssertions;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Utils.Serialization;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Tests.Integration.Serialization
{
    class OIAContextSerializationTest
    {
        [Serializable]
        class TestConfig : IOnlineInboundAdapter.IConfig, IEquatable<TestConfig>
        {
            public string BuilderName { get; set; } = "unset";
            public string MapperScope { get; set; } = "unset";

            public override int GetHashCode() => HashCode.Combine(BuilderName, MapperScope);
            public override bool Equals(object? obj) => Equals(obj as TestConfig);
            public bool Equals([AllowNull] TestConfig other)
            {
                return other != null && other.BuilderName == BuilderName && other.MapperScope == MapperScope;
            }
        }

        [Test]
        public void TestSerialization()
        {
            var dataSerializer = new ProtoBufDataSerializer();
            var a = new OIAContext("name", 234L, new TestConfig() { BuilderName = "BuilderName", MapperScope = "MapperScope"});
            var b = dataSerializer.ToByteArray(a);
            var c = dataSerializer.FromByteArray<OIAContext>(b);
            c.Should().BeEquivalentTo(a);
        }
    }
}
