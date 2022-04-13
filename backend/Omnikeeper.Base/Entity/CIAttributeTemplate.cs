using JsonSubTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Omnikeeper.Base.Entity
{
    public class AttributeValueConstraintTypeDiscriminatorConverter : TypeDiscriminatorConverter<ICIAttributeValueConstraint> 
    {
        public AttributeValueConstraintTypeDiscriminatorConverter() : base("type", "$type")
        {
        }
    }

    [Newtonsoft.Json.JsonConverter(typeof(JsonSubtypes), "type")]
    [JsonSubtypes.KnownSubType(typeof(CIAttributeValueConstraintTextRegex), "textRegex")]
    [JsonSubtypes.KnownSubType(typeof(CIAttributeValueConstraintTextLength), "textLength")]
    [System.Text.Json.Serialization.JsonConverter(typeof(AttributeValueConstraintTypeDiscriminatorConverter))]
    public interface ICIAttributeValueConstraint
    {
        public string type { get; }
        public string ttype { get; }
        bool HasErrors(IAttributeValue value);

        public static readonly NewtonSoftJSONSerializer<ICIAttributeValueConstraint> NewtonsoftSerializer = new NewtonSoftJSONSerializer<ICIAttributeValueConstraint>(() =>
        {
            var s = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Objects
            };
            s.Converters.Add(new StringEnumConverter());
            return s;
        });

        public static readonly SystemTextJSONSerializer<ICIAttributeValueConstraint> SystemTextJSONSerializer = new SystemTextJSONSerializer<ICIAttributeValueConstraint>(() =>
        {
            return new System.Text.Json.JsonSerializerOptions()
            {
                Converters = {
                    new JsonStringEnumConverter()
                },
                IncludeFields = true
            };
        });
    }

    public class CIAttributeValueConstraintTextLength : ICIAttributeValueConstraint
    {
        public readonly int? Minimum;
        public readonly int? Maximum;

        public CIAttributeValueConstraintTextLength(int? minimum, int? maximum)
        {
            Minimum = minimum;
            Maximum = maximum;
        }

        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public string type => "textLength";

        [Newtonsoft.Json.JsonIgnore]
        [JsonPropertyName("$type")]
        public string ttype => SystemTextJSONSerializerMigrationHelper.GetTypeString(GetType());

        public static CIAttributeValueConstraintTextLength Build(int? min, int? max)
        {
            if (min > max) throw new Exception("Minimum value must not be larger than maximum value");
            return new CIAttributeValueConstraintTextLength(min, max);
        }

        public bool HasErrors(IAttributeValue value)
        {
            // HACK: this is a bit unclean, as we do CLR type-checking, but return an error based on the AttributeValueType value
            if (value is IAttributeValueText v)
            {
                return !v.ApplyTextLengthConstraint(Minimum, Maximum).IsEmpty();
            }
            else
            {
                return true;
            }
        }
    }

    public class CIAttributeValueConstraintArrayLength : ICIAttributeValueConstraint
    {
        public readonly int? Minimum;
        public readonly int? Maximum;

        public CIAttributeValueConstraintArrayLength(int? minimum, int? maximum)
        {
            Minimum = minimum;
            Maximum = maximum;
        }

        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public string type => "arrayLength";

        [Newtonsoft.Json.JsonIgnore]
        [JsonPropertyName("$type")]
        public string ttype => SystemTextJSONSerializerMigrationHelper.GetTypeString(GetType());

        public static CIAttributeValueConstraintArrayLength Build(int? min, int? max)
        {
            if (min > max) throw new Exception("Minimum value must not be larger than maximum value");
            return new CIAttributeValueConstraintArrayLength(min, max);
        }

        public bool HasErrors(IAttributeValue value)
        {
            if (value.IsArray)
            {
                var a = (value as IAttributeArrayValue);
                if (a == null)
                    return true;
                else if (Maximum.HasValue && a.Length > Maximum)
                    return true;
                else if (Minimum.HasValue && a.Length < Minimum)
                    return true;
                return false;
            }
            else
            {
                return true;
            }
        }
    }

    public class CIAttributeValueConstraintTextRegex : ICIAttributeValueConstraint
    {
        public readonly string RegexStr;
        public readonly RegexOptions RegexOptions;

        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public string type => "textRegex";

        [Newtonsoft.Json.JsonIgnore]
        [JsonPropertyName("$type")]
        public string ttype => SystemTextJSONSerializerMigrationHelper.GetTypeString(GetType());

        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [NonSerialized]
        private Regex? regex;

        public CIAttributeValueConstraintTextRegex(Regex r)
        {
            RegexStr = r.ToString(); // NOTE: weird, but ToString() returns the original pattern
            RegexOptions = r.Options;
            regex = r;
        }

        [Newtonsoft.Json.JsonConstructor]
        [System.Text.Json.Serialization.JsonConstructor]
        public CIAttributeValueConstraintTextRegex(string regexStr, RegexOptions regexOptions)
        {
            RegexStr = regexStr;
            RegexOptions = regexOptions;
            regex = null;
        }

        public bool HasErrors(IAttributeValue value)
        {
            // HACK: this is a bit unclean, as we do CLR type-checking, but return an error based on the AttributeValueType value
            if (value is IAttributeValueText v)
            {
                if (regex == null)
                    regex = new Regex(RegexStr, RegexOptions);
                return !v.MatchRegex(regex).IsEmpty();
            }
            else
            {
                return true;
            }
        }
    }

    public class CIAttributeTemplate
    {
        public readonly string Name;
        public readonly AttributeValueType? Type;
        public readonly bool? IsArray;
        public readonly IEnumerable<ICIAttributeValueConstraint> ValueConstraints;
        public readonly bool? IsID;

        public static CIAttributeTemplate BuildFromParams(string name, AttributeValueType? type, bool? isArray, bool? isID, params ICIAttributeValueConstraint[] valueConstraints)
        {
            return new CIAttributeTemplate(name, type, isArray, isID, valueConstraints);
        }

        public CIAttributeTemplate(string name, AttributeValueType? type, bool? isArray, bool? isID, IEnumerable<ICIAttributeValueConstraint> valueConstraints)
        {
            Name = name;
            Type = type;
            IsArray = isArray;
            ValueConstraints = valueConstraints;
            IsID = isID;
        }
    }
}
