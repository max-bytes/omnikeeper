
using OKPluginGenericJSONIngest.Transform.JMESPath;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using System.Collections.Generic;
using System.Text.Json.Serialization;

#pragma warning disable CS8618 // TODO
namespace OKPluginGenericJSONIngest
{
    [JsonSourceGenerationOptions(IncludeFields = true)]
    [JsonSerializable(typeof(GenericInboundData))]
    internal partial class GenericInboundDataJsonContext : JsonSerializerContext
    {
    }

    public class GenericInboundData
    {
        public IEnumerable<GenericInboundCI> cis;
        public IEnumerable<GenericInboundRelation> relations;
    }

    public class GenericInboundCI
    {
        public string tempID;
        public IInboundIDMethod idMethod;
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public SameTempIDHandling sameTempIDHandling;
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public SameTargetCIHandling sameTargetCIHandling;
        public IEnumerable<GenericInboundAttribute> attributes;
    }


    public class InboundIDMethodDiscriminatorConverter : TypeDiscriminatorConverter<IInboundIDMethod>
    {
        public InboundIDMethodDiscriminatorConverter() : base("type", typeof(InboundIDMethodDiscriminatorConverter))
        {
        }
    }

    [JsonConverter(typeof(InboundIDMethodDiscriminatorConverter))]
    public interface IInboundIDMethod
    {
        string type { get; }
    }

    public class InboundIDMethodByData : IInboundIDMethod
    {
        public string type => SystemTextJSONSerializerMigrationHelper.GetTypeString(GetType());

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
        public string type => SystemTextJSONSerializerMigrationHelper.GetTypeString(GetType());
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
        public string type => SystemTextJSONSerializerMigrationHelper.GetTypeString(GetType());
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
        public string type => SystemTextJSONSerializerMigrationHelper.GetTypeString(GetType());
        public readonly string tempID;

        public InboundIDMethodByTemporaryCIID(string tempID)
        {
            this.tempID = tempID;
        }
    }

    public class InboundIDMethodByByUnion : IInboundIDMethod
    {
        public string type => SystemTextJSONSerializerMigrationHelper.GetTypeString(GetType());
        public readonly IInboundIDMethod[] inner;

        public InboundIDMethodByByUnion(IInboundIDMethod[] inner)
        {
            this.inner = inner;
        }
    }
    public class InboundIDMethodByIntersect : IInboundIDMethod
    {
        public string type => SystemTextJSONSerializerMigrationHelper.GetTypeString(GetType());
        public readonly IInboundIDMethod[] inner;

        public InboundIDMethodByIntersect(IInboundIDMethod[] inner)
        {
            this.inner = inner;
        }
    }

    public class GenericInboundAttribute
    {
        public string name;
        [JsonConverter(typeof(SystemTextJsonAttributeValueConverter))]
        public IAttributeValue value;
    }

    public class GenericInboundRelation
    {
        public string from;
        public string predicate;
        public string to;
    }
}
#pragma warning restore CS8618
