﻿using ProtoBuf;
using ProtoBuf.Serializers;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace Omnikeeper.Entity.AttributeValues
{
    [ProtoContract(Serializer = typeof(AttributeScalarValueYAMLSerializer))]
    public class AttributeScalarValueYAML : IAttributeScalarValue<YamlDocument>, IEquatable<AttributeScalarValueYAML>
    {
        private AttributeScalarValueYAML(YamlDocument value, string valueStr)
        {
            this.value = value;
            this.valueStr = valueStr;
        }

        private readonly YamlDocument value;
        public YamlDocument Value => value;
        private readonly string valueStr;
        public string ValueStr => valueStr;

        public override string ToString() => $"AV-YAML: {Value2String()}";

        public string Value2String() => ValueStr;
        public string[] ToRawDTOValues() => new string[] { ValueStr };
        public object ToGenericObject() => Value;
        public bool IsArray => false;

        public AttributeValueType Type => AttributeValueType.YAML;

        public bool Equals([AllowNull] IAttributeValue other) => Equals(other as AttributeScalarValueYAML);
        public bool Equals([AllowNull] AttributeScalarValueYAML other) => other != null && false;// TODO: implement proper equals
        public override int GetHashCode() => Value.GetHashCode();

        public static AttributeScalarValueYAML BuildFromString(string value)
        {
            var stream = new YamlStream();
            stream.Load(new StringReader(value));
            var document = stream.Documents.FirstOrDefault();
            if (document == null) throw new Exception("Could not parse YAML");
            return new AttributeScalarValueYAML(document, value);
        }

        public static AttributeScalarValueYAML Build(YamlDocument document)
        {
            var yamlStream = new YamlStream(document);
            var buffer = new StringBuilder();
            using var writer = new StringWriter(buffer);
            yamlStream.Save(writer, false);
            return new AttributeScalarValueYAML(document, writer.ToString());
        }
        public static AttributeScalarValueYAML Build(YamlDocument document, string str)
        {
            return new AttributeScalarValueYAML(document, str);
        }
    }

    public class AttributeScalarValueYAMLSerializer : ISubTypeSerializer<AttributeScalarValueYAML>, ISerializer<AttributeScalarValueYAML>
    {
        SerializerFeatures ISerializer<AttributeScalarValueYAML>.Features => SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString;
        void ISerializer<AttributeScalarValueYAML>.Write(ref ProtoWriter.State state, AttributeScalarValueYAML value) 
            => ((ISubTypeSerializer<AttributeScalarValueYAML>)this).WriteSubType(ref state, value);
        AttributeScalarValueYAML ISerializer<AttributeScalarValueYAML>.Read(ref ProtoReader.State state, AttributeScalarValueYAML value) 
            => ((ISubTypeSerializer<AttributeScalarValueYAML>)this).ReadSubType(ref state, SubTypeState<AttributeScalarValueYAML>.Create(state.Context, value));

        public void WriteSubType(ref ProtoWriter.State state, AttributeScalarValueYAML value)
        {
            state.WriteFieldHeader(1, WireType.String);
            state.WriteString(value.ValueStr);
        }

        public AttributeScalarValueYAML ReadSubType(ref ProtoReader.State state, SubTypeState<AttributeScalarValueYAML> value)
        {
            int field;
            string valueStr = "";
            while ((field = state.ReadFieldHeader()) > 0)
            {
                switch (field)
                {
                    case 1:
                        valueStr = state.ReadString();
                        break;
                    default:
                        state.SkipField();
                        break;
                }
            }
            if (valueStr != "")
                return AttributeScalarValueYAML.BuildFromString(valueStr);
            else
                throw new Exception("Could not deserialize AttributeScalarValueYAML");
        }
    }


    [ProtoContract(SkipConstructor = true)]
    public class AttributeArrayValueYAML : AttributeArrayValue<AttributeScalarValueYAML, YamlDocument>
    {
        public AttributeArrayValueYAML(AttributeScalarValueYAML[] values) : base(values)
        {
        }

        public override AttributeValueType Type => AttributeValueType.YAML;

        public static AttributeArrayValueYAML BuildFromString(string[] values)
        {
            var yamlDocuments = values.Select(value =>
            {
                var stream = new YamlStream();
                stream.Load(new StringReader(value));
                var document = stream.Documents.FirstOrDefault();
                if (document == null) throw new Exception("Could not parse YAML");
                return document;
            }).ToArray();
            return Build(yamlDocuments, values);
        }

        public static AttributeArrayValueYAML Build(YamlDocument[] values, string[] valuesStr)
        {
            if (values.Length != valuesStr.Length) throw new Exception("Values and valuesStr must be equal length");
            var n = new AttributeArrayValueYAML(
                values.Select((v, index) => AttributeScalarValueYAML.Build(v, valuesStr[index])).ToArray()
            );
            return n;
        }
    }
}
