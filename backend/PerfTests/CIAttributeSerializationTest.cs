using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using FluentAssertions;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Base.Utils.Serialization;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace PerfTests
{
    [Explicit]
    public class CIAttributeSerializationTest
    {
        private readonly CIAttribute aScalarText;
        private readonly CIAttribute aScalarYAML;
        private readonly CIAttribute aScalarJSON;
        //private readonly CIAttribute aArrayText;
        private readonly byte[] bOldAttributeScalarText;
        private readonly byte[] bAttributeScalarText;
        private readonly byte[] bAttributeScalarYAML;
        private readonly byte[] bAttributeScalarJSON;
        //private readonly byte[] bAttributeArrayText;
        private readonly IDataSerializer protoBufDS = new ProtoBufDataSerializer();
        private readonly IDataSerializer binaryFormatterDS = new BinaryFormatterDataSerializer();

        public CIAttributeSerializationTest()
        {
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

            aScalarText = new CIAttribute(Guid.NewGuid(), "test", Guid.NewGuid(), new AttributeScalarValueText("foo"), AttributeState.New, Guid.NewGuid());
            aScalarYAML = new CIAttribute(Guid.NewGuid(), "test", Guid.NewGuid(), AttributeScalarValueYAML.BuildFromString(testYAML), AttributeState.New, Guid.NewGuid());
            aScalarJSON = new CIAttribute(Guid.NewGuid(), "test", Guid.NewGuid(), AttributeScalarValueYAML.BuildFromString(testJSON), AttributeState.New, Guid.NewGuid());
            //aArrayText = new CIAttribute(Guid.NewGuid(), "test", Guid.NewGuid(), AttributeArrayValueText.BuildFromString(new string[] { "foo", "bar" }), AttributeState.New, Guid.NewGuid(), new DataOriginV1(DataOriginType.Manual));

            bAttributeScalarText = protoBufDS.ToByteArray(aScalarText);
            bAttributeScalarYAML = protoBufDS.ToByteArray(aScalarYAML);
            bAttributeScalarJSON = protoBufDS.ToByteArray(aScalarJSON);
            //bAttributeArrayText = DistributedCacheExtensions.ToByteArray(aArrayText);
            bOldAttributeScalarText = binaryFormatterDS.ToByteArray(aScalarText);
        }

        [Benchmark]
        public byte[] SerializeScalarTextOld() => binaryFormatterDS.ToByteArray(aScalarText);
        [Benchmark]
        public CIAttribute DeserializeScalarTextOld() => binaryFormatterDS.FromByteArray<CIAttribute>(bOldAttributeScalarText)!;

        [Benchmark]
        public byte[] SerializeScalarText() => protoBufDS.ToByteArray(aScalarText);
        [Benchmark]
        public CIAttribute DeserializeScalarText() => protoBufDS.FromByteArray<CIAttribute>(bAttributeScalarText)!;

        [Benchmark]
        public byte[] SerializeScalarYAML() => protoBufDS.ToByteArray(aScalarYAML);
        [Benchmark]
        public CIAttribute DeserializeScalarYAML() => protoBufDS.FromByteArray<CIAttribute>(bAttributeScalarYAML)!;

        [Benchmark]
        public byte[] SerializeScalarJSON() => protoBufDS.ToByteArray(aScalarJSON);
        [Benchmark]
        public CIAttribute DeserializeScalarJSON() => protoBufDS.FromByteArray<CIAttribute>(bAttributeScalarJSON)!;

        [Test]
        public void Run()
        {
            var summary = BenchmarkRunner.Run<CIAttributeSerializationTest>();
        }
    }
}
