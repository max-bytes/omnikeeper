using Newtonsoft.Json.Linq;
using Omnikeeper.Base.Entity;

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

        public Validation(string id, string ruleName, JObject ruleConfig)
        {
            RuleName = ruleName;
            RuleConfig = ruleConfig;
            ID = id;
            Name = $"Validation - {ID}";
        }

        public Validation()
        {
            RuleName = "";
            RuleConfig = new JObject();
            ID = "";
            Name = "";
        }
    }
}
