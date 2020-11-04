using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DTO;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using YamlDotNet.RepresentationModel;

namespace Omnikeeper.Entity.AttributeValues
{
    public class AttributeScalarValueYAML : IAttributeScalarValue<YamlDocument>, IEquatable<AttributeScalarValueYAML>
    {
        public YamlDocument Value { get; private set; }
        private string ValueStr { get; set; }

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
            return new AttributeScalarValueYAML
            {
                Value = document,
                ValueStr = value
            };
        }

        public static AttributeScalarValueYAML Build(YamlDocument document)
        {
            var yamlStream = new YamlStream(document);
            var buffer = new StringBuilder();
            using var writer = new StringWriter(buffer);
            yamlStream.Save(writer, false);
            return new AttributeScalarValueYAML
            {
                Value = document,
                ValueStr = writer.ToString()
            };
        }
        public static AttributeScalarValueYAML Build(YamlDocument document, string str)
        {
            return new AttributeScalarValueYAML
            {
                Value = document,
                ValueStr = str
            };
        }
    }


    public class AttributeArrayValueYAML : AttributeArrayValue<AttributeScalarValueYAML, YamlDocument>
    {
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
            var n = new AttributeArrayValueYAML
            {
                Values = values.Select((v, index) => AttributeScalarValueYAML.Build(v, valuesStr[index])).ToArray()
            };
            return n;
        }
    }
}
