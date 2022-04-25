using Omnikeeper.Base.Entity;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace OKPluginInsightDiscoveryScanIngest
{
    [TraitEntity("__meta.config.insight_discovers_ingest_context", TraitOriginType.Plugin)]
    public class Context : TraitEntity
    {
        [TraitAttribute("id", "insight_discovers_ingest_context.id")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        [TraitAttributeValueConstraintTextRegex(ContextIDRegexString, ContextIDRegexOptions)]
        [TraitEntityID]
        public readonly string ID;

        [TraitAttribute("name", "__name", optional: true)]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        public readonly string Name;

        [TraitAttribute("genericJsonIngestContextID", "insight_discovers_ingest_context.generic_json_ingest_context_id")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        public readonly string GenericJsonIngestContextID;

        public Context()
        {
            ID = "";
            Name = "";
            GenericJsonIngestContextID = "";
        }

        [JsonConstructor]
        public Context(string id, string genericJsonIngestContextID)
        {
            ID = id;
            Name = $"Insight-Discovery-Ingest-Context {ID}";
            GenericJsonIngestContextID = genericJsonIngestContextID;
        }

        public const string ContextIDRegexString = "^[a-z0-9_]+$";
        public const RegexOptions ContextIDRegexOptions = RegexOptions.None;
    }
}
