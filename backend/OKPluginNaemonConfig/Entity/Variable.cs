using Omnikeeper.Base.Entity;

namespace OKPluginNaemonConfig.Entity
{
    [TraitEntity("variable", TraitOriginType.Core)]
    public class Variable : TraitEntity
    {
        [TraitAttribute("id", "naemon_variable.id")]
        public readonly long Id;

        [TraitAttribute("name", "naemon_variable.name")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        [TraitEntityID]
        public readonly string Name;

        [TraitAttribute("type", "naemon_variable.type")]
        public readonly string Type;

        [TraitAttribute("reftype", "naemon_variable.reftype", optional: true)]
        public readonly string RefType;

        [TraitAttribute("value", "naemon_variable.value", optional: true)]
        public readonly string Value;

        [TraitAttribute("issecret", "naemon_variable.issecret", optional: true)]
        public readonly long IsSecret;

        public Variable()
        {
            Name = "";
            RefType = "";
            Type = "";
            Value = "";
        }
    }
}
