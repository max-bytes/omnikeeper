using Landscape.Base.Entity;
using Landscape.Base.Entity.DTO;
using Landscape.Base.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace LandscapeRegistry.Entity.AttributeValues
{
    public abstract class AttributeValueYAML : IAttributeValue
    {
        public override string ToString() => $"AV-YAML: {Value2String()}";
        public AttributeValueType Type => AttributeValueType.YAML;
        public abstract string Value2String();
        public abstract bool IsArray { get; }
        public abstract AttributeValueDTO ToDTO();
        public abstract object ToGenericObject();
        public abstract bool Equals(IAttributeValue other);
        public abstract bool FullTextSearch(string searchString, CompareOptions compareOptions);

        public IEnumerable<ITemplateErrorAttribute> ApplyTextLengthConstraint(int? minimum, int? maximum)
        { // does not make sense for YAML
            yield return TemplateErrorAttributeWrongType.Build(AttributeValueType.Text, Type);
        }

        public IEnumerable<ITemplateErrorAttribute> MatchRegex(Regex regex)
        { // does not make sense for YAML
            yield return TemplateErrorAttributeWrongType.Build(AttributeValueType.Text, Type);
        }

    }

    public class AttributeValueYAMLScalar : AttributeValueYAML, IEquatable<AttributeValueYAMLScalar>
    {
        public YamlDocument Value { get; private set; }
        private string ValueStr { get; set; }
        public override string Value2String() => ValueStr;
        public override AttributeValueDTO ToDTO() => AttributeValueDTO.Build(ValueStr, Type);
        public override object ToGenericObject() => Value;
        public override bool IsArray => false;
        public override bool Equals([AllowNull] IAttributeValue other) => Equals(other as AttributeValueYAMLScalar);
        public bool Equals([AllowNull] AttributeValueYAMLScalar other) => other != null && false;// TODO: implement proper equals
        public override int GetHashCode() => Value.GetHashCode();
        public override bool FullTextSearch(string searchString, CompareOptions compareOptions)
        {
            throw new NotImplementedException("FullTextSearch not implemented yet for YAML");
        }

        internal static AttributeValueYAMLScalar Build(string value)
        {
            //var deserializer = new DeserializerBuilder().Build();
            //var yamlObject = deserializer.Deserialize<YamlDocument>(value);
            var stream = new YamlStream();
            stream.Load(new StringReader(value));
            var document = stream.Documents.FirstOrDefault();
            if (document == null) throw new Exception("Could not parse YAML");
            return new AttributeValueYAMLScalar
            {
                Value = document,
                ValueStr = value
            };
        }

    }

    public class AttributeValueYAMLArray : AttributeValueYAML, IEquatable<AttributeValueYAMLArray>
    {
        public YamlDocument[] Values { get; private set; }
        public override string Value2String() => string.Join(",", Values.Select(value => value.ToString().Replace(",", "\\,")));
        public override AttributeValueDTO ToDTO() => AttributeValueDTO.Build(Values.Select(v => v.ToString()).ToArray(), Type);
        public override object ToGenericObject() => Values;
        public override bool IsArray => true;
        public override bool Equals([AllowNull] IAttributeValue other) => Equals(other as AttributeValueYAMLArray);

        private class EqualityComparer : IEqualityComparer<YamlDocument>
        {
            public bool Equals(YamlDocument x, YamlDocument y) => false; // TODO: implement deep equality
            public int GetHashCode(YamlDocument obj) => obj.GetHashCode();
        }
        private static readonly EqualityComparer ec = new EqualityComparer();
        public bool Equals([AllowNull] AttributeValueYAMLArray other) 
            => other != null && Values.SequenceEqual(other.Values, ec);
        public override int GetHashCode() => Values.GetHashCode();
        public override bool FullTextSearch(string searchString, CompareOptions compareOptions)
        {
            throw new NotImplementedException("FullTextSearch not implemented yet for YAML");
        }


        public static AttributeValueYAMLArray Build(string[] values)
        {
            var yamlDocuments = values.Select(value => {
                var stream = new YamlStream();
                stream.Load(new StringReader(value));
                var document = stream.Documents.FirstOrDefault();
                if (document == null) throw new Exception("Could not parse YAML");
                return document;
            }).ToArray();
            return Build(yamlDocuments);
        }

        public static AttributeValueYAMLArray Build(YamlDocument[] values)
        {
            var n = new AttributeValueYAMLArray
            {
                Values = values
            };
            return n;
        }
    }
}
