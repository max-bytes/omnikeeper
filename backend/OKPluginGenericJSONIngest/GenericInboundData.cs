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
    [SwaggerSubType(typeof(InboundIDMethodByData), DiscriminatorValue = "OKPluginGenericJSONIngest.InboundIDMethodByData, OKPluginGenericJSONIngest")]
    [SwaggerSubType(typeof(InboundIDMethodByAttribute), DiscriminatorValue = "OKPluginGenericJSONIngest.InboundIDMethodByAttribute, OKPluginGenericJSONIngest")]
    [SwaggerSubType(typeof(InboundIDMethodByAttributeExists), DiscriminatorValue = "OKPluginGenericJSONIngest.InboundIDMethodByAttributeExists, OKPluginGenericJSONIngest")]
    [SwaggerSubType(typeof(InboundIDMethodByRelatedTempID), DiscriminatorValue = "OKPluginGenericJSONIngest.InboundIDMethodByRelatedTempID, OKPluginGenericJSONIngest")]
    [SwaggerSubType(typeof(InboundIDMethodByTemporaryCIID), DiscriminatorValue = "OKPluginGenericJSONIngest.InboundIDMethodByTemporaryCIID, OKPluginGenericJSONIngest")]
    [SwaggerSubType(typeof(InboundIDMethodByUnion), DiscriminatorValue = "OKPluginGenericJSONIngest.InboundIDMethodByUnion, OKPluginGenericJSONIngest")]
    [SwaggerSubType(typeof(InboundIDMethodByIntersect), DiscriminatorValue = "OKPluginGenericJSONIngest.InboundIDMethodByIntersect, OKPluginGenericJSONIngest")]
    public abstract class AbstractInboundIDMethod
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }
    }

    public class InboundIDMethodByData : AbstractInboundIDMethod
    {
        //[JsonPropertyName("type")]
        //public override string type
        //{
        //    get
        //    {
        //        return SystemTextJSONSerializerMigrationHelper.GetTypeString(GetType());
        //    }
        //    set;
        //}

        [JsonPropertyName("attributes")]
        public string[] Attributes { get; }

        [JsonConstructor]
        public InboundIDMethodByData(string[] attributes)
        {
            Attributes = attributes;
            Type = SystemTextJSONSerializerMigrationHelper.GetTypeString(GetType());
        }
    }

    public class InboundIDMethodByAttributeModifiers
    {
        [JsonPropertyName("caseInsensitive")]
        public bool CaseInsensitive { get; }

        [JsonConstructor]
        public InboundIDMethodByAttributeModifiers(bool caseInsensitive)
        {
            CaseInsensitive = caseInsensitive;
        }
    }

    public class InboundIDMethodByAttribute : AbstractInboundIDMethod
    {
        [JsonPropertyName("attribute")]
        public GenericInboundAttribute Attribute { get; set; }
        [JsonPropertyName("modifiers")]
        public InboundIDMethodByAttributeModifiers Modifiers { get; set; }

        [JsonConstructor]
        public InboundIDMethodByAttribute(GenericInboundAttribute attribute, InboundIDMethodByAttributeModifiers modifiers)
        {
            Attribute = attribute;
            Modifiers = modifiers;
            Type = SystemTextJSONSerializerMigrationHelper.GetTypeString(GetType());
        }
    }

    public class InboundIDMethodByAttributeExists : AbstractInboundIDMethod
    {
        [JsonPropertyName("attributes")]
        public string[] Attributes { get; set; }

        [JsonConstructor]
        public InboundIDMethodByAttributeExists(string[] attributes)
        {
            Attributes = attributes;
            Type = SystemTextJSONSerializerMigrationHelper.GetTypeString(GetType());
        }
    }

    public class InboundIDMethodByRelatedTempID : AbstractInboundIDMethod
    {
        //public override string type => SystemTextJSONSerializerMigrationHelper.GetTypeString(GetType());
        [JsonPropertyName("tempID")]
        public string TempID { get; set; }
        [JsonPropertyName("outgoingRelation")]
        public bool OutgoingRelation { get; set; }
        [JsonPropertyName("predicateID")]
        public string PredicateID { get; set; }

        [JsonConstructor]
        public InboundIDMethodByRelatedTempID(string tempID, bool outgoingRelation, string predicateID)
        {
            TempID = tempID;
            OutgoingRelation = outgoingRelation;
            PredicateID = predicateID;
            Type = SystemTextJSONSerializerMigrationHelper.GetTypeString(GetType());
        }
    }

    public class InboundIDMethodByTemporaryCIID : AbstractInboundIDMethod
    {
        //public override string type => SystemTextJSONSerializerMigrationHelper.GetTypeString(GetType());
        [JsonPropertyName("tempID")]
        public string TempID { get; set; }

        [JsonConstructor]
        public InboundIDMethodByTemporaryCIID(string tempID)
        {
            TempID = tempID;
            Type = SystemTextJSONSerializerMigrationHelper.GetTypeString(GetType());
        }
    }

    public class InboundIDMethodByUnion : AbstractInboundIDMethod
    {
        //public override string type => SystemTextJSONSerializerMigrationHelper.GetTypeString(GetType());
        [JsonPropertyName("inner")]
        public AbstractInboundIDMethod[] Inner { get; set; }

        [JsonConstructor]
        public InboundIDMethodByUnion(AbstractInboundIDMethod[] inner)
        {
            Inner = inner;
            Type = SystemTextJSONSerializerMigrationHelper.GetTypeString(GetType());
        }
    }
    public class InboundIDMethodByIntersect : AbstractInboundIDMethod
    {
        //public override string type => SystemTextJSONSerializerMigrationHelper.GetTypeString(GetType());
        [JsonPropertyName("inner")]
        public AbstractInboundIDMethod[] Inner { get; set; }

        [JsonConstructor]
        public InboundIDMethodByIntersect(AbstractInboundIDMethod[] inner)
        {
            Inner = inner;
            Type = SystemTextJSONSerializerMigrationHelper.GetTypeString(GetType());
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

        [JsonConverter(typeof(JsonStringEnumConverter))]
        [JsonPropertyName("duplicateRelationHandling")]
        public DuplicateRelationHandling DuplicateRelationHandling { get; set; }
    }
}
#pragma warning restore CS8618
