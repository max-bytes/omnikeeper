using JsonSubTypes;
using Newtonsoft.Json;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using ProtoBuf;
using ProtoBuf.Serializers;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Omnikeeper.Base.Entity
{
    [JsonConverter(typeof(JsonSubtypes), "type")]
    [JsonSubtypes.KnownSubType(typeof(CIAttributeValueConstraintTextRegex), "textRegex")]
    [JsonSubtypes.KnownSubType(typeof(CIAttributeValueConstraintTextLength), "textLength")]
    [ProtoContract]
    [ProtoInclude(1, typeof(CIAttributeValueConstraintTextLength))]
    [ProtoInclude(2, typeof(CIAttributeValueConstraintTextRegex))]
    public interface ICIAttributeValueConstraint
    {
        public string type { get; }
        [Obsolete]
        IEnumerable<ITemplateErrorAttribute> CalculateErrors(IAttributeValue value);
        bool HasErrors(IAttributeValue value);
    }

    [ProtoContract(SkipConstructor = true)]
    public class CIAttributeValueConstraintTextLength : ICIAttributeValueConstraint
    {
        [ProtoMember(1)] public readonly int? Minimum;
        [ProtoMember(2)] public readonly int? Maximum;

        public CIAttributeValueConstraintTextLength(int? minimum, int? maximum)
        {
            Minimum = minimum;
            Maximum = maximum;
        }

        [JsonIgnore]
        public string type => "textLength";

        public static CIAttributeValueConstraintTextLength Build(int? min, int? max)
        {
            if (min > max) throw new Exception("Minimum value must not be larger than maximum value");
            return new CIAttributeValueConstraintTextLength(min, max);
        }

        [Obsolete]
        public IEnumerable<ITemplateErrorAttribute> CalculateErrors(IAttributeValue value)
        {
            // HACK: this is a bit unclean, as we do CLR type-checking, but return an error based on the AttributeValueType value
            if (value is IAttributeValueText v)
            {
                return v.ApplyTextLengthConstraint(Minimum, Maximum);
            }
            else
            {
                return new ITemplateErrorAttribute[] { new TemplateErrorAttributeWrongType(new AttributeValueType[] { AttributeValueType.Text, AttributeValueType.MultilineText }, value.Type) };
            }
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

    [ProtoContract(SkipConstructor = true)]
    public class CIAttributeValueConstraintArrayLength : ICIAttributeValueConstraint
    {
        [ProtoMember(1)] public readonly int? Minimum;
        [ProtoMember(2)] public readonly int? Maximum;

        public CIAttributeValueConstraintArrayLength(int? minimum, int? maximum)
        {
            Minimum = minimum;
            Maximum = maximum;
        }

        [JsonIgnore]
        public string type => "arrayLength";

        public static CIAttributeValueConstraintArrayLength Build(int? min, int? max)
        {
            if (min > max) throw new Exception("Minimum value must not be larger than maximum value");
            return new CIAttributeValueConstraintArrayLength(min, max);
        }

        [Obsolete]
        public IEnumerable<ITemplateErrorAttribute> CalculateErrors(IAttributeValue value)
        {
            if (value.IsArray)
            {
                var a = (value as IAttributeArrayValue);
                if (a == null)
                    yield return new TemplateErrorAttributeWrongMultiplicity();
                else if (Maximum.HasValue && a.Length > Maximum)
                    yield return new TemplateErrorAttributeGeneric("Array too long!");
                else if (Minimum.HasValue && a.Length < Minimum)
                    yield return new TemplateErrorAttributeGeneric("Array too short!");
            }
            else
            {
                yield return new TemplateErrorAttributeWrongMultiplicity();
            }
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

    [ProtoContract(Serializer = typeof(Serializer))]
    public class CIAttributeValueConstraintTextRegex : ICIAttributeValueConstraint
    {
        public readonly string RegexStr;
        public readonly RegexOptions RegexOptions;

        [JsonIgnore]
        public string type => "textRegex";

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

        [Obsolete]
        public IEnumerable<ITemplateErrorAttribute> CalculateErrors(IAttributeValue value)
        {
            // HACK: this is a bit unclean, as we do CLR type-checking, but return an error based on the AttributeValueType value
            if (value is IAttributeValueText v)
            {
                if (regex == null)
                    regex = new Regex(RegexStr, RegexOptions);
                return v.MatchRegex(regex);
            }
            else
            {
                return new ITemplateErrorAttribute[] { new TemplateErrorAttributeWrongType(new AttributeValueType[] { AttributeValueType.Text, AttributeValueType.MultilineText }, value.Type) };
            }
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

        public class Serializer : ISubTypeSerializer<CIAttributeValueConstraintTextRegex>, ISerializer<CIAttributeValueConstraintTextRegex>
        {
            SerializerFeatures ISerializer<CIAttributeValueConstraintTextRegex>.Features => SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString;
            void ISerializer<CIAttributeValueConstraintTextRegex>.Write(ref ProtoWriter.State state, CIAttributeValueConstraintTextRegex value)
                => ((ISubTypeSerializer<CIAttributeValueConstraintTextRegex>)this).WriteSubType(ref state, value);
            CIAttributeValueConstraintTextRegex ISerializer<CIAttributeValueConstraintTextRegex>.Read(ref ProtoReader.State state, CIAttributeValueConstraintTextRegex value)
                => ((ISubTypeSerializer<CIAttributeValueConstraintTextRegex>)this).ReadSubType(ref state, SubTypeState<CIAttributeValueConstraintTextRegex>.Create(state.Context, value));

            public void WriteSubType(ref ProtoWriter.State state, CIAttributeValueConstraintTextRegex value)
            {
                state.WriteFieldHeader(1, WireType.String);
                state.WriteString(value.RegexStr);
                state.WriteFieldHeader(2, WireType.Varint);
                state.WriteInt32((int)value.RegexOptions);
            }

            public CIAttributeValueConstraintTextRegex ReadSubType(ref ProtoReader.State state, SubTypeState<CIAttributeValueConstraintTextRegex> value)
            {
                int field;
                string regexStr = "";
                RegexOptions regexOptions = default;
                while ((field = state.ReadFieldHeader()) > 0)
                {
                    switch (field)
                    {
                        case 1:
                            regexStr = state.ReadString();
                            break;
                        case 2:
                            regexOptions = (RegexOptions)state.ReadInt32();
                            break;
                        default:
                            state.SkipField();
                            break;
                    }
                }
                return new CIAttributeValueConstraintTextRegex(regexStr, regexOptions);
            }
        }
    }

    [ProtoContract(SkipConstructor = true)]
    public class CIAttributeTemplate
    {
        [ProtoMember(1)] public readonly string Name;
        [ProtoMember(2)] public readonly AttributeValueType? Type;
        [ProtoMember(3)] public readonly bool? IsArray;
        [ProtoMember(4)] public readonly IEnumerable<ICIAttributeValueConstraint> ValueConstraints;
        [ProtoMember(5)] public readonly bool? IsID;

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
