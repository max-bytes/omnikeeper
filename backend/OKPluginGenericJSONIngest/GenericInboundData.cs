using Omnikeeper.Base.Entity.DTO;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
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
        public AbstractInboundIDMethod IDMethod { get; set; }

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


    public class InboundIDMethodDiscriminatorConverter : TypeDiscriminatorConverter<AbstractInboundIDMethod>
    {
        public InboundIDMethodDiscriminatorConverter() : base("type", typeof(InboundIDMethodDiscriminatorConverter))
        {
        }
    }

    [JsonConverter(typeof(InboundIDMethodDiscriminatorConverter))]
    [SwaggerDiscriminator("type")]
    [SwaggerSubType(typeof(InboundIDMethodByData))]//, DiscriminatorValue = "OKPluginGenericJSONIngest.InboundIDMethodByData, OKPluginGenericJSONIngest")]
    [SwaggerSubType(typeof(InboundIDMethodByAttributeModifiers))]//, DiscriminatorValue = "OKPluginGenericJSONIngest.InboundIDMethodByAttributeModifiers, OKPluginGenericJSONIngest")]
    [SwaggerSubType(typeof(InboundIDMethodByAttribute))]//, DiscriminatorValue = "OKPluginGenericJSONIngest.InboundIDMethodByAttribute, OKPluginGenericJSONIngest")]
    [SwaggerSubType(typeof(InboundIDMethodByRelatedTempID))]//, DiscriminatorValue = "OKPluginGenericJSONIngest.InboundIDMethodByRelatedTempID, OKPluginGenericJSONIngest")]
    [SwaggerSubType(typeof(InboundIDMethodByTemporaryCIID))]//, DiscriminatorValue = "OKPluginGenericJSONIngest.InboundIDMethodByTemporaryCIID, OKPluginGenericJSONIngest")]
    [SwaggerSubType(typeof(InboundIDMethodByByUnion))]//, DiscriminatorValue = "OKPluginGenericJSONIngest.InboundIDMethodByByUnion, OKPluginGenericJSONIngest")]
    [SwaggerSubType(typeof(InboundIDMethodByIntersect))]//, DiscriminatorValue = "OKPluginGenericJSONIngest.InboundIDMethodByIntersect, OKPluginGenericJSONIngest")]
    public abstract class AbstractInboundIDMethod
    {
        public abstract string type { get; }
    }

    public class InboundIDMethodByData : AbstractInboundIDMethod
    {
        [JsonPropertyName("type")]
        public override string type => SystemTextJSONSerializerMigrationHelper.GetTypeString(GetType());

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

    public class InboundIDMethodByAttribute : AbstractInboundIDMethod
    {
        public override string type => SystemTextJSONSerializerMigrationHelper.GetTypeString(GetType());
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

    public class InboundIDMethodByRelatedTempID : AbstractInboundIDMethod
    {
        public override string type => SystemTextJSONSerializerMigrationHelper.GetTypeString(GetType());
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

    public class InboundIDMethodByTemporaryCIID : AbstractInboundIDMethod
    {
        public override string type => SystemTextJSONSerializerMigrationHelper.GetTypeString(GetType());
        [JsonPropertyName("tempID")]
        public string TempID { get; set; }

        public InboundIDMethodByTemporaryCIID(string tempID)
        {
            TempID = tempID;
        }
    }

    public class InboundIDMethodByByUnion : AbstractInboundIDMethod
    {
        public override string type => SystemTextJSONSerializerMigrationHelper.GetTypeString(GetType());
        [JsonPropertyName("inner")]
        public AbstractInboundIDMethod[] Inner { get; set; }

        public InboundIDMethodByByUnion(AbstractInboundIDMethod[] inner)
        {
            Inner = inner;
        }
    }
    public class InboundIDMethodByIntersect : AbstractInboundIDMethod
    {
        public override string type => SystemTextJSONSerializerMigrationHelper.GetTypeString(GetType());
        [JsonPropertyName("inner")]
        public AbstractInboundIDMethod[] Inner { get; set; }

        public InboundIDMethodByIntersect(AbstractInboundIDMethod[] inner)
        {
            Inner = inner;
        }
    }

    public class GenericInboundAttribute
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("value")]
        public AttributeValueDTO Value { get; set; }
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
