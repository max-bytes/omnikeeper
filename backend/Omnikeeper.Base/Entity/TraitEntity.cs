using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Omnikeeper.Base.Entity
{
    // TODO: needed?
    public abstract class TraitEntity
    {
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class TraitEntityAttribute : Attribute
    {
        public readonly string traitName;
        public readonly TraitOriginType originType;

        public TraitEntityAttribute(string traitName, TraitOriginType originType)
        {
            this.traitName = traitName;
            this.originType = originType;
        }
    }

    public interface IAttributeJSONSerializer
    {
        public object DeserializeFromAttributeValue(IAttributeValue attribute);
        public IAttributeValue SerializeToAttributeValue(object o, bool isArray);
    }

    public abstract class AttributeJSONSerializer<T> : IAttributeJSONSerializer where T : class
    {
        private readonly SystemTextJSONSerializer<T> systemTextJsonSerializer;

        protected AttributeJSONSerializer(Func<JsonSerializerOptions> serializerOptions)
        {
            systemTextJsonSerializer = new SystemTextJSONSerializer<T>(serializerOptions);
        }

        public object DeserializeFromAttributeValue(IAttributeValue attribute)
        {
            if (attribute is AttributeScalarValueJSON @as)
            {
                return systemTextJsonSerializer.Deserialize(@as.ValueStr);
            }
            else if (attribute is AttributeArrayValueJSON aa)
            {
                var ret = new T[aa.Length];
                for (int i = 0; i < aa.Values.Length; i++)
                {
                    ret[i] = systemTextJsonSerializer.Deserialize(aa.Values[i].ValueStr);
                }
                return ret;
            }
            else
            {
                throw new Exception("Unexpected attribute value type encountered; expected JSON attribute value");
            }
        }

        public IAttributeValue SerializeToAttributeValue(object o, bool isArray)
        {
            if (isArray)
            {
                var a = (T[])o;
                var serialized = new string[a.Length];
                for (int i = 0; i < a.Length; i++)
                {
                    var e = systemTextJsonSerializer.SerializeToString(a[i]);
                    serialized[i] = e;
                }
                return AttributeArrayValueJSON.BuildFromString(serialized, false);
            }
            else
            {
                var jo = systemTextJsonSerializer.SerializeToString((T)o);
                return AttributeScalarValueJSON.BuildFromString(jo, false);
            }
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class TraitAttributeAttribute : Attribute
    {
        public readonly string taName;
        public readonly string aName;
        public readonly bool optional;
        public readonly Type? jsonSerializer;
        public readonly bool multilineTextHint;

        public TraitAttributeAttribute(string taName, string aName, bool optional = false, Type? jsonSerializer = null, bool multilineTextHint = false)
        {
            this.taName = taName;
            this.aName = aName;
            this.optional = optional;
            this.jsonSerializer = jsonSerializer;
            this.multilineTextHint = multilineTextHint;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class TraitRelationAttribute : Attribute
    {
        public readonly string trName;
        public readonly string predicateID;
        public readonly bool directionForward;
        public readonly string[]? traitHints;

        public TraitRelationAttribute(string trName, string predicateID, bool directionForward, string[]? traitHints = null)
        {
            this.trName = trName;
            this.predicateID = predicateID;
            this.predicateID = predicateID;
            this.directionForward = directionForward;
            this.traitHints = traitHints;
        }
    }



    [AttributeUsage(AttributeTargets.Field)]
    public sealed class TraitEntityIDAttribute : Attribute
    {
        public TraitEntityIDAttribute()
        {
        }
    }



    [AttributeUsage(AttributeTargets.Field)]
    public sealed class TraitAttributeValueConstraintTextLengthAttribute : Attribute
    {
        public readonly int? Minimum;
        public readonly int? Maximum;

        public TraitAttributeValueConstraintTextLengthAttribute(int minimum, int maximum)
        {
            if (minimum == -1)
                Minimum = null;
            else
                Minimum = minimum;
            if (maximum == -1)
                Maximum = null;
            else
                Maximum = maximum;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class TraitAttributeValueConstraintTextRegexAttribute : Attribute
    {
        public readonly string RegexStr;
        public readonly RegexOptions RegexOptions;

        public TraitAttributeValueConstraintTextRegexAttribute(string regexStr, RegexOptions regexOptions)
        {
            RegexStr = regexStr;
            RegexOptions = regexOptions;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class TraitAttributeValueConstraintArrayLengthAttribute : Attribute
    {
        public readonly int? Minimum;
        public readonly int? Maximum;

        public TraitAttributeValueConstraintArrayLengthAttribute(int minimum, int maximum)
        {
            if (minimum == -1)
                Minimum = null;
            else
                Minimum = minimum;
            if (maximum == -1)
                Maximum = null;
            else
                Maximum = maximum;
        }
    }
}
