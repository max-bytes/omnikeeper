using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Omnikeeper.Base.Entity
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
    [JsonDerivedType(typeof(CIAttributeValueConstraintTextLength), typeDiscriminator: "Omnikeeper.Base.Entity.CIAttributeValueConstraintTextLength, Omnikeeper.Base")]
    [JsonDerivedType(typeof(CIAttributeValueConstraintArrayLength), typeDiscriminator: "Omnikeeper.Base.Entity.CIAttributeValueConstraintArrayLength, Omnikeeper.Base")]
    [JsonDerivedType(typeof(CIAttributeValueConstraintTextRegex), typeDiscriminator: "Omnikeeper.Base.Entity.CIAttributeValueConstraintTextRegex, Omnikeeper.Base")]
    public interface ICIAttributeValueConstraint
    {
        bool HasErrors(IAttributeValue value);

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

    public class CIAttributeValueConstraintTextLength : ICIAttributeValueConstraint, IEquatable<CIAttributeValueConstraintTextLength>
    {
        public readonly int? Minimum;
        public readonly int? Maximum;

        public CIAttributeValueConstraintTextLength(int? minimum, int? maximum)
        {
            Minimum = minimum;
            Maximum = maximum;
        }

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

        public bool Equals(CIAttributeValueConstraintTextLength? other) => other != null && Minimum == other.Minimum && Maximum == other.Maximum;
        public override bool Equals(object? other) => Equals(other as CIAttributeValueConstraintTextLength);
        public override int GetHashCode() => HashCode.Combine(Minimum, Maximum);
    }

    public class CIAttributeValueConstraintArrayLength : ICIAttributeValueConstraint, IEquatable<CIAttributeValueConstraintArrayLength>
    {
        public readonly int? Minimum;
        public readonly int? Maximum;

        public CIAttributeValueConstraintArrayLength(int? minimum, int? maximum)
        {
            Minimum = minimum;
            Maximum = maximum;
        }

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

        public bool Equals(CIAttributeValueConstraintArrayLength? other) => other != null && Minimum == other.Minimum && Maximum == other.Maximum;
        public override bool Equals(object? other) => Equals(other as CIAttributeValueConstraintArrayLength);
        public override int GetHashCode() => HashCode.Combine(Minimum, Maximum);
    }

    public class CIAttributeValueConstraintTextRegex : ICIAttributeValueConstraint, IEquatable<CIAttributeValueConstraintTextRegex>
    {
        public readonly string RegexStr;
        public readonly RegexOptions RegexOptions;

        [JsonIgnore]
        [NonSerialized]
        private Regex? regex;

        public CIAttributeValueConstraintTextRegex(Regex r)
        {
            RegexStr = r.ToString(); // NOTE: weird, but ToString() returns the original pattern
            RegexOptions = r.Options;
            regex = r;
        }

        [JsonConstructor]
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

        public bool Equals(CIAttributeValueConstraintTextRegex? other) => other != null && RegexStr == other.RegexStr && RegexOptions == other.RegexOptions;
        public override bool Equals(object? other) => Equals(other as CIAttributeValueConstraintTextRegex);
        public override int GetHashCode() => HashCode.Combine(RegexStr, RegexOptions);
    }

    public class CIAttributeTemplate : IEquatable<CIAttributeTemplate>
    {
        public readonly string Name;
        public readonly AttributeValueType? Type;
        public readonly bool? IsArray;
        public readonly ICIAttributeValueConstraint[] ValueConstraints;
        public readonly bool? IsID;

        public static CIAttributeTemplate BuildFromParams(string name, AttributeValueType? type, bool? isArray, bool? isID, params ICIAttributeValueConstraint[] valueConstraints)
        {
            return new CIAttributeTemplate(name, type, isArray, isID, valueConstraints);
        }

        public CIAttributeTemplate(string name, AttributeValueType? type, bool? isArray, bool? isID, ICIAttributeValueConstraint[] valueConstraints)
        {
            Name = name;
            Type = type;
            IsArray = isArray;
            ValueConstraints = valueConstraints;
            IsID = isID;
        }

        public bool Equals(CIAttributeTemplate? other)
        {
            // NOTE: see https://stackoverflow.com/questions/69133392/computing-hashcode-of-combination-of-value-type-and-array why we use StruturalComparisons
            return other != null && Name == other.Name && Type == other.Type && IsArray == other.IsArray && IsID == other.IsID && StructuralComparisons.StructuralEqualityComparer.Equals(ValueConstraints, other.ValueConstraints);
        }
        public override bool Equals(object? other) => Equals(other as CIAttributeTemplate);
        public override int GetHashCode() => HashCode.Combine(Name, Type, IsArray, IsID, StructuralComparisons.StructuralEqualityComparer.GetHashCode(ValueConstraints));
    }
}
