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
    [JsonSubtypes.KnownSubType(typeof(InboundIDMethodByTemporaryCIID), "byTempID")]
    public interface IInboundIDMethod
    {

    }
    public class InboundIDMethodByData : IInboundIDMethod
    {
        public readonly string[] attributes;

        public InboundIDMethodByData(string[] attributes)
        {
            this.attributes = attributes;
        }
    }

    public class InboundIDMethodByTemporaryCIID : IInboundIDMethod
    {
        public readonly string tempID;

        public InboundIDMethodByTemporaryCIID(string tempID)
        {
            this.tempID = tempID;
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
