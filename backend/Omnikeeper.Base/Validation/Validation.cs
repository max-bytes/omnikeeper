using Newtonsoft.Json.Linq;

namespace Omnikeeper.Base.Validation
{
    public class Validation
    {
        public readonly string ID;
        public readonly string RuleName;
        public readonly JObject RuleConfig;

        public Validation(string id, string ruleName, JObject ruleConfig)
        {
            this.RuleName = ruleName;
            this.RuleConfig = ruleConfig;
            this.ID = id;
        }
    }
}
