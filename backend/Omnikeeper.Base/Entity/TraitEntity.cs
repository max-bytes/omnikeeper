using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Linq;
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
        public object Deserialize(IAttributeValue attribute, Type type);
        public IAttributeValue Serialize(object o, bool isArray);
    }

    public abstract class AttributeJSONSerializer<T> : IAttributeJSONSerializer where T : class
    {
        private readonly SystemTextJSONSerializer<T> systemTextJsonSerializer;

        protected AttributeJSONSerializer(Func<JsonSerializerOptions> serializerOptions)
        {
            systemTextJsonSerializer = new SystemTextJSONSerializer<T>(serializerOptions);
        }

        public object Deserialize(IAttributeValue attribute, Type type)
        {
            if (attribute is AttributeScalarValueJSON @as)
            {
                return systemTextJsonSerializer.Deserialize(@as.Value, type);
            }
            else if (attribute is AttributeArrayValueJSON aa)
            {
                //var tokens = aa.Values.Select(v => v.Value).ToArray();
                //var deserialized = Array.CreateInstance(type, tokens.Length); // TODO: use proper generic types instead of Type
                //for (int i = 0; i < tokens.Length; i++)
                //{
                //    var e = systemTextJsonSerializer.Deserialize(tokens[i], type);
                //    if (e == null)
                //        throw new Exception(); // TODO
                //    deserialized.SetValue(e, i);
                //}
                //return deserialized;
                var ret = new T[aa.Length];
                for(int i = 0;i < aa.Values.Length;i++)
                {
                    ret[i] = systemTextJsonSerializer.Deserialize(aa.Values[i].Value);
                }
                return ret;
            }
            else
            {
                throw new Exception("Unexpected attribute value type encountered; expected JSON attribute value");
            }
        }

        public IAttributeValue Serialize(object o, bool isArray)
        {
            if (isArray)
            {
                var a = (object[])o;
                var serialized = new JsonDocument[a.Length];
                for (int i = 0; i < a.Length; i++)
                {
                    var e = systemTextJsonSerializer.SerializeToJsonDocument(a[i]);
                    serialized[i] = e;
                }
                return AttributeArrayValueJSON.Build(serialized);
            }
            else
            {
                var jo = systemTextJsonSerializer.SerializeToJsonDocument(o);
                return AttributeScalarValueJSON.Build(jo);
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

        public TraitRelationAttribute(string trName, string predicateID, bool directionForward)
        {
            this.trName = trName;
            this.predicateID = predicateID;
            this.predicateID = predicateID;
            this.directionForward = directionForward;
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
