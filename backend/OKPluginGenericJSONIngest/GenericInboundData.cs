using OKPluginGenericJSONIngest.Transform.JMESPath;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using Swashbuckle.AspNetCore.Annotations;
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
        // NOTE, HACK: I'd much rather use fields instead of properties, but swashbuckle schema generation does not seem to support that properly
        // so we use properties for now and explicitly set the propertyName on this class and all related classes
        [JsonPropertyName("cis")]
        public IEnumerable<GenericInboundCI> CIs { get; set; }

        [JsonPropertyName("relations")]
        public IEnumerable<GenericInboundRelation> Relations { get; set; }
    }

    public class GenericInboundCI
    {
        [JsonPropertyName("tempID")]
        public string TempID { get; set; }

        [JsonPropertyName("idMethod")]
        public IInboundIDMethod IDMethod { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        [JsonPropertyName("sameTempIDHandling")]
        public SameTempIDHandling SameTempIDHandling { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        [JsonPropertyName("sameTargetCIHandling")]
        public SameTargetCIHandling SameTargetCIHandling { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        [JsonPropertyName("noFoundTargetCIHandling")]
        public NoFoundTargetCIHandling NoFoundTargetCIHandling { get; set; }

        [JsonPropertyName("attributes")]
        public IEnumerable<GenericInboundAttribute> Attributes { get; set; }
    }


    public class InboundIDMethodDiscriminatorConverter : TypeDiscriminatorConverter<IInboundIDMethod>
    {
        public InboundIDMethodDiscriminatorConverter() : base("type", typeof(InboundIDMethodDiscriminatorConverter))
        {
        }
    }

    [JsonConverter(typeof(InboundIDMethodDiscriminatorConverter))]
    [SwaggerDiscriminator("type")]
    [SwaggerSubType(typeof(InboundIDMethodByData))]
    [SwaggerSubType(typeof(InboundIDMethodByAttributeModifiers))]
    [SwaggerSubType(typeof(InboundIDMethodByAttribute))]
    [SwaggerSubType(typeof(InboundIDMethodByRelatedTempID))]
    [SwaggerSubType(typeof(InboundIDMethodByTemporaryCIID))]
    [SwaggerSubType(typeof(InboundIDMethodByByUnion))]
    [SwaggerSubType(typeof(InboundIDMethodByIntersect))]
    public interface IInboundIDMethod
    {
        string type { get; }
    }

    public class InboundIDMethodByData : IInboundIDMethod
    {
        public string type => SystemTextJSONSerializerMigrationHelper.GetTypeString(GetType());

        [JsonPropertyName("attributes")]
        public string[] Attributes { get; }

        [JsonConstructor]
        public InboundIDMethodByData(string[] attributes)
        {
            Attributes = attributes;
        }
    }

    public class InboundIDMethodByAttributeModifiers
    {
        [JsonPropertyName("caseInsensitive")]
        public bool CaseInsensitive { get; }

        public InboundIDMethodByAttributeModifiers(bool caseInsensitive)
        {
            CaseInsensitive = caseInsensitive;
        }
    }

    public class InboundIDMethodByAttribute : IInboundIDMethod
    {
        public string type => SystemTextJSONSerializerMigrationHelper.GetTypeString(GetType());
        [JsonPropertyName("attribute")]
        public GenericInboundAttribute Attribute { get; set; }
        [JsonPropertyName("modifiers")]
        public InboundIDMethodByAttributeModifiers Modifiers { get; set; }

        public InboundIDMethodByAttribute(GenericInboundAttribute attribute, InboundIDMethodByAttributeModifiers modifiers)
        {
            Attribute = attribute;
            Modifiers = modifiers;
        }
    }

    public class InboundIDMethodByRelatedTempID : IInboundIDMethod
    {
        public string type => SystemTextJSONSerializerMigrationHelper.GetTypeString(GetType());
        [JsonPropertyName("tempID")]
        public string TempID { get; set; }
        [JsonPropertyName("outgoingRelation")]
        public bool OutgoingRelation { get; set; }
        [JsonPropertyName("predicateID")]
        public string PredicateID { get; set; }

        public InboundIDMethodByRelatedTempID(string tempID, bool outgoingRelation, string predicateID)
        {
            TempID = tempID;
            OutgoingRelation = outgoingRelation;
            PredicateID = predicateID;
        }
    }

    public class InboundIDMethodByTemporaryCIID : IInboundIDMethod
    {
        public string type => SystemTextJSONSerializerMigrationHelper.GetTypeString(GetType());
        [JsonPropertyName("tempID")]
        public string TempID { get; set; }

        public InboundIDMethodByTemporaryCIID(string tempID)
        {
            TempID = tempID;
        }
    }

    public class InboundIDMethodByByUnion : IInboundIDMethod
    {
        public string type => SystemTextJSONSerializerMigrationHelper.GetTypeString(GetType());
        [JsonPropertyName("inner")]
        public IInboundIDMethod[] Inner { get; set; }

        public InboundIDMethodByByUnion(IInboundIDMethod[] inner)
        {
            Inner = inner;
        }
    }
    public class InboundIDMethodByIntersect : IInboundIDMethod
    {
        public string type => SystemTextJSONSerializerMigrationHelper.GetTypeString(GetType());
        [JsonPropertyName("inner")]
        public IInboundIDMethod[] Inner { get; set; }

        public InboundIDMethodByIntersect(IInboundIDMethod[] inner)
        {
            Inner = inner;
        }
    }

    public class GenericInboundAttribute
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonConverter(typeof(SystemTextJsonAttributeValueConverter))]
        [JsonPropertyName("value")]
        public IAttributeValue Value { get; set; }
    }

    public class GenericInboundRelation
    {
        [JsonPropertyName("from")]
        public string From { get; set; }

        [JsonPropertyName("predicate")]
        public string Predicate { get; set; }

        [JsonPropertyName("to")]
        public string To { get; set; }
    }
}
#pragma warning restore CS8618
