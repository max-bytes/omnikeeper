using Landscape.Base.Entity;
using Landscape.Base.Entity.DTO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapeRegistry.Entity.AttributeValues
{
    public abstract class AttributeValueJSON : IAttributeValue
    {
        public override string ToString() => $"AV-JSON: {Value2String()}";
        public AttributeValueType Type => AttributeValueType.JSON;
        public abstract string Value2String();
        public abstract bool IsArray { get; }
        public abstract AttributeValueDTO ToGeneric();
        public abstract bool Equals(IAttributeValue other);

        public IEnumerable<ITemplateErrorAttribute> ApplyTextLengthConstraint(int? minimum, int? maximum)
        { // does not make sense for JSON
            yield return TemplateErrorAttributeWrongType.Build(AttributeValueType.Text, Type);
        }
    }

    public class AttributeValueJSONScalar : AttributeValueJSON, IEquatable<AttributeValueJSONScalar>
    {
        public JObject Value { get; private set; }
        public override string Value2String() => Value.ToString();
        public override AttributeValueDTO ToGeneric() => AttributeValueDTO.Build(Value2String(), Type);
        public override bool IsArray => false;
        public override bool Equals([AllowNull] IAttributeValue other) => Equals(other as AttributeValueJSONScalar);
        public bool Equals([AllowNull] AttributeValueJSONScalar other) => other != null && Value.Equals(other.Value);
        public override int GetHashCode() => Value.GetHashCode();

        internal static AttributeValueJSONScalar Build(string value)
        {
            var v = JObject.Parse(value); // TODO: throws JsonReaderException, handle, but how?
            return Build(v);
        }

        public static AttributeValueJSONScalar Build(JObject value)
        {
            var n = new AttributeValueJSONScalar
            {
                Value = value
            };
            return n;
        }
    }

    public class AttributeValueJSONArray : AttributeValueJSON, IEquatable<AttributeValueJSONArray>
    {
        public JObject[] Values { get; private set; }
        public override string Value2String() => string.Join(",", Values.Select(value => value.ToString().Replace(",", "\\,")));
        public override AttributeValueDTO ToGeneric() => AttributeValueDTO.Build(Values.Select(v => v.ToString()).ToArray(), Type);
        public override bool IsArray => true;
        public override bool Equals([AllowNull] IAttributeValue other) => Equals(other as AttributeValueJSONArray);
        public bool Equals([AllowNull] AttributeValueJSONArray other) => other != null && Values.SequenceEqual(other.Values);
        public override int GetHashCode() => Values.GetHashCode();

        public static AttributeValueJSONArray Build(string[] values)
        {
            var jsonValues = values.Select(value => { return JObject.Parse(value); }).ToArray(); // TODO: throws JsonReaderException, handle, but how?
            return Build(jsonValues);
        }

        public static AttributeValueJSONArray Build(JObject[] values)
        {
            var n = new AttributeValueJSONArray
            {
                Values = values
            };
            return n;
        }
    }
}
