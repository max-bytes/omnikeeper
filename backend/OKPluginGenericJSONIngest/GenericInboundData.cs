using JsonSubTypes;
using Newtonsoft.Json;
using OKPluginGenericJSONIngest.Transform.JMESPath;
using Omnikeeper.Entity.AttributeValues;
using System.Collections.Generic;

#pragma warning disable CS8618
namespace OKPluginGenericJSONIngest
{
    public class GenericInboundData
    {
        public IEnumerable<GenericInboundCI> cis;
        public IEnumerable<GenericInboundRelation> relations;
    }

    public class GenericInboundCI
    {
        public string tempID;
        public IInboundIDMethod idMethod;
        public IEnumerable<GenericInboundAttribute> attributes;
    }


    [JsonConverter(typeof(JsonSubtypes), "type")]
    [JsonSubtypes.KnownSubType(typeof(InboundIDMethodByData), "byData")]
    [JsonSubtypes.KnownSubType(typeof(InboundIDMethodByAttribute), "byAttribute")]
    [JsonSubtypes.KnownSubType(typeof(InboundIDMethodByRelatedTempID), "byRelatedTempID")]
    [JsonSubtypes.KnownSubType(typeof(InboundIDMethodByTemporaryCIID), "byTempID")]
    [JsonSubtypes.KnownSubType(typeof(InboundIDMethodByByUnion), "byUnion")]
    [JsonSubtypes.KnownSubType(typeof(InboundIDMethodByIntersect), "byIntersect")]
    public interface IInboundIDMethod {
        string type { get; }
    }

    public class InboundIDMethodByData : IInboundIDMethod
    {
        public string type { get; } = "byData";
        public readonly string[] attributes;

        public InboundIDMethodByData(string[] attributes)
        {
            this.attributes = attributes;
        }
    }

    public class InboundIDMethodByAttributeModifiers
    {
        public readonly bool caseInsensitive;

        public InboundIDMethodByAttributeModifiers(bool caseInsensitive)
        {
            this.caseInsensitive = caseInsensitive;
        }
    }

    public class InboundIDMethodByAttribute : IInboundIDMethod
    {
        public string type { get; } = "byAttribute";
        public readonly GenericInboundAttribute attribute;
        public readonly InboundIDMethodByAttributeModifiers modifiers;

        public InboundIDMethodByAttribute(GenericInboundAttribute attribute, InboundIDMethodByAttributeModifiers modifiers)
        {
            this.attribute = attribute;
            this.modifiers = modifiers;
        }
    }

    public class InboundIDMethodByRelatedTempID : IInboundIDMethod
    {
        public string type { get; } = "byRelatedTempID";
        public readonly string tempID;
        public readonly bool outgoingRelation;
        public readonly string predicateID;

        public InboundIDMethodByRelatedTempID(string tempID, bool outgoingRelation, string predicateID)
        {
            this.tempID = tempID;
            this.outgoingRelation = outgoingRelation;
            this.predicateID = predicateID;
        }
    }

    public class InboundIDMethodByTemporaryCIID : IInboundIDMethod
    {
        public string type { get; } = "byTempID";
        public readonly string tempID;

        public InboundIDMethodByTemporaryCIID(string tempID)
        {
            this.tempID = tempID;
        }
    }

    public class InboundIDMethodByByUnion : IInboundIDMethod
    {
        public string type { get; } = "byUnion";
        public readonly IInboundIDMethod[] inner;

        public InboundIDMethodByByUnion(IInboundIDMethod[] inner)
        {
            this.inner = inner;
        }
    }
    public class InboundIDMethodByIntersect : IInboundIDMethod
    {
        public string type { get; } = "byIntersect";
        public readonly IInboundIDMethod[] inner;

        public InboundIDMethodByIntersect(IInboundIDMethod[] inner)
        {
            this.inner = inner;
        }
    }

    public class GenericInboundAttribute
    {
        public string name;
        [JsonConverter(typeof(AttributeValueConverter<string>))]
        public object? value;
        public AttributeValueType type;
    }

    public class GenericInboundRelation
    {
        public string from;
        public string predicate;
        public string to;
    }
}
#pragma warning restore CS8618
