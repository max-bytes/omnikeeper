using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using YamlDotNet.RepresentationModel;

namespace Omnikeeper.Entity.AttributeValues
{
    public sealed record class AttributeScalarValueYAML(YamlDocument Value, string ValueStr) : IAttributeScalarValue<YamlDocument>
    {
        public override string ToString() => $"AV-YAML: {Value2String()}";

        public string Value2String() => ValueStr;
        public string[] ToRawDTOValues() => new string[] { ValueStr };
        public object ToGenericObject() => Value;
        public bool IsArray => false;
        public object ToGraphQLValue() => ValueStr;

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

    //public class AttributeScalarValueYAMLSerializer : ISubTypeSerializer<AttributeScalarValueYAML>, ISerializer<AttributeScalarValueYAML>
    //{
    //    SerializerFeatures ISerializer<AttributeScalarValueYAML>.Features => SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString;
    //    void ISerializer<AttributeScalarValueYAML>.Write(ref ProtoWriter.State state, AttributeScalarValueYAML value)
    //        => ((ISubTypeSerializer<AttributeScalarValueYAML>)this).WriteSubType(ref state, value);
    //    AttributeScalarValueYAML ISerializer<AttributeScalarValueYAML>.Read(ref ProtoReader.State state, AttributeScalarValueYAML value)
    //        => ((ISubTypeSerializer<AttributeScalarValueYAML>)this).ReadSubType(ref state, SubTypeState<AttributeScalarValueYAML>.Create(state.Context, value));

    //    public void WriteSubType(ref ProtoWriter.State state, AttributeScalarValueYAML value)
    //    {
    //        state.WriteFieldHeader(1, WireType.String);
    //        state.WriteString(value.ValueStr);
    //    }

    //    public AttributeScalarValueYAML ReadSubType(ref ProtoReader.State state, SubTypeState<AttributeScalarValueYAML> value)
    //    {
    //        int field;
    //        string valueStr = "";
    //        while ((field = state.ReadFieldHeader()) > 0)
    //        {
    //            switch (field)
    //            {
    //                case 1:
    //                    valueStr = state.ReadString();
    //                    break;
    //                default:
    //                    state.SkipField();
    //                    break;
    //            }
    //        }
    //        if (valueStr != "")
    //            return AttributeScalarValueYAML.BuildFromString(valueStr);
    //        else
    //            throw new Exception("Could not deserialize AttributeScalarValueYAML");
    //    }
    //}


    public sealed record class AttributeArrayValueYAML(string[] ValuesStr, YamlDocument[] Values) : IAttributeArrayValue
    {
        public AttributeValueType Type => AttributeValueType.YAML;

        public int Length => ValuesStr.Length;
        public bool IsArray => true;

        public static AttributeArrayValueYAML BuildFromString(string[] values)
        {
            var yamlDocuments = values.Select(value =>
            {
                var stream = new YamlStream();
                stream.Load(new StringReader(value));
                var document = stream.Documents.FirstOrDefault();
                if (document == null) throw new Exception("Could not parse YAML");
                return document;
            }).ToArray(); // TODO: make lazy, like JSON
            return new AttributeArrayValueYAML(values, yamlDocuments);
        }

        public bool Equals(AttributeArrayValueYAML? other) => other != null && Values.SequenceEqual(other.Values);
        public override int GetHashCode() => Values.GetHashCode();

        public string[] ToRawDTOValues() => ValuesStr;
        public object ToGenericObject() => Values;
        public object ToGraphQLValue() => ValuesStr;
        public string Value2String() => string.Join(",", ValuesStr.Select(value => value.Replace(",", "\\,")));
    }
}
