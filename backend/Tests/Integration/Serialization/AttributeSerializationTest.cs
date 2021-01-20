using FluentAssertions;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Base.Utils.Serialization;
using Omnikeeper.Entity.AttributeValues;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tests.Integration.Serialization
{
    class AttributeSerializationTest
    {
        [Test]
        public void TestAttributeSerialization()
        {
            var dataSerializer = new ProtoBufDataSerializer();
            var aScalarText = new CIAttribute(Guid.NewGuid(), "test", Guid.NewGuid(), new AttributeScalarValueText("foo"), AttributeState.New, Guid.NewGuid(), new DataOriginV1(DataOriginType.Manual));
            var sScalarText = dataSerializer.ToByteArray(aScalarText);
            dataSerializer.FromByteArray<CIAttribute>(sScalarText).Should().BeEquivalentTo(aScalarText);

            var aArrayText = new CIAttribute(Guid.NewGuid(), "test", Guid.NewGuid(), AttributeArrayValueText.BuildFromString(new string[] { "foo", "bar" }), AttributeState.New, Guid.NewGuid(), new DataOriginV1(DataOriginType.Manual));
            var sArrayText = dataSerializer.ToByteArray(aArrayText);
            dataSerializer.FromByteArray<CIAttribute>(sArrayText).Should().BeEquivalentTo(aArrayText);
        }

        [Test]
        public void TestAttributeValueSerialization()
        {
            var dataSerializer = new ProtoBufDataSerializer();
            var testYAML = @"foo: 
  bar: 
    - true
    - false
  test: true";

            var testJSON = @"{
  ""foo"": [""bar"", ""test""],
  ""bla"": {
    ""blub"": true
  }
}";
            var bvp = BinaryScalarAttributeValueProxy.BuildFromHash(
                new byte[] { 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00, 0x02 },
                "test-mimetype", 5555);

            // TODO: test only value, not whole attribute

            var aScalarText = new CIAttribute(Guid.NewGuid(), "test", Guid.NewGuid(), new AttributeScalarValueText("foo", true), AttributeState.New, Guid.NewGuid(), new DataOriginV1(DataOriginType.Manual));
            var sScalarText = dataSerializer.ToByteArray(aScalarText);
            dataSerializer.FromByteArray<CIAttribute>(sScalarText).Should().BeEquivalentTo(aScalarText);

            var aScalarInteger = new CIAttribute(Guid.NewGuid(), "test", Guid.NewGuid(), new AttributeScalarValueInteger(-12), AttributeState.New, Guid.NewGuid(), new DataOriginV1(DataOriginType.Manual));
            var sScalarInteger = dataSerializer.ToByteArray(aScalarInteger);
            dataSerializer.FromByteArray<CIAttribute>(sScalarInteger).Should().BeEquivalentTo(aScalarInteger);

            var aScalarYAML = new CIAttribute(Guid.NewGuid(), "test", Guid.NewGuid(), AttributeScalarValueYAML.BuildFromString(testYAML), AttributeState.New, Guid.NewGuid(), new DataOriginV1(DataOriginType.Manual));
            var sScalarYAML = dataSerializer.ToByteArray(aScalarYAML);
            dataSerializer.FromByteArray<CIAttribute>(sScalarYAML).Should().BeEquivalentTo(aScalarYAML);

            var aScalarJSON = new CIAttribute(Guid.NewGuid(), "test", Guid.NewGuid(), AttributeScalarValueJSON.BuildFromString(testJSON), AttributeState.New, Guid.NewGuid(), new DataOriginV1(DataOriginType.Manual));
            var sScalarJSON = dataSerializer.ToByteArray(aScalarJSON);
            dataSerializer.FromByteArray<CIAttribute>(sScalarJSON).Should().BeEquivalentTo(aScalarJSON);

            var aScalarImage = new CIAttribute(Guid.NewGuid(), "test", Guid.NewGuid(), new AttributeScalarValueImage(bvp), AttributeState.New, Guid.NewGuid(), new DataOriginV1(DataOriginType.Manual));
            var sScalarImage = dataSerializer.ToByteArray(aScalarImage);
            dataSerializer.FromByteArray<CIAttribute>(sScalarImage).Should().BeEquivalentTo(aScalarImage);
        }
    }
}
