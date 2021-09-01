using Newtonsoft.Json;
using OKPluginGenericJSONIngest.Transform.JMESPath;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Text;

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
        public GenericInboundIDMethod idMethod;
        public IEnumerable<GenericInboundAttribute> attributes;
    }

    public class GenericInboundIDMethod
    {
        public string method; // TODO: should be made generic
        public string[] attributes;
        public string tempID;
    }

    public class GenericInboundAttribute
    {
        public string name;
        [JsonConverter(typeof(AttributeValueConverter<string>))]
        public object value;
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
