using Newtonsoft.Json.Linq;
using Omnikeeper.Base.Entity;
using System;

namespace OKPluginValidation.Validation
{
    [TraitEntity("__meta.validation.validation", TraitOriginType.Plugin)]
    public class Validation : TraitEntity
    {
        [TraitAttribute("id", "validation.id")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        [TraitEntityID]
        public readonly string ID;

        [TraitAttribute("rule_name", "validation.rule_name")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        public readonly string RuleName;

        [TraitAttribute("rule_config", "validation.rule_config")]
        public readonly JObject RuleConfig;

        [TraitAttribute("name", "__name", optional: true)]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        public readonly string Name;

        [TraitRelation("detected_issues", "__meta.validation.belongs_to_validation", false)]
        public readonly Guid[] DetectedIssues;

        public Validation(string id, string ruleName, JObject ruleConfig)
        {
            RuleName = ruleName;
            RuleConfig = ruleConfig;
            ID = id;
            Name = $"Validation - {ID}";
            DetectedIssues = new Guid[0];
        }

        public Validation()
        {
            RuleName = "";
            RuleConfig = new JObject();
            ID = "";
            Name = "";
            DetectedIssues = new Guid[0];
        }
    }
}
