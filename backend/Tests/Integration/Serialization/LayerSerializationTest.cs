using FluentAssertions;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Utils.Serialization;
using ProtoBuf.Meta;
using System;
using System.Drawing;

namespace Tests.Integration.Serialization
{
    class LayerSerializationTest
    {
        [Test]
        public void TestSerialization()
        {
            var dataSerializer = new ProtoBufDataSerializer();
            var a = Layer.Build("name", 123L, Color.Red, AnchorState.Deprecated, ComputeLayerBrainLink.Build("clbName"), OnlineInboundAdapterLink.Build("oiaName"));
            var b = dataSerializer.ToByteArray(a);
            dataSerializer.FromByteArray<Layer>(b).Should().BeEquivalentTo(a);
        }
    }
}
