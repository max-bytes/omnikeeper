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

        [TraitAttribute("reftype", "naemon_variable.reftype")]
        public readonly string RefType;

        [TraitAttribute("type", "naemon_variable.type")]
        public readonly string Type;

        [TraitAttribute("value", "naemon_variable.value")]
        public readonly string Value;

        [TraitAttribute("issecret", "naemon_variable.issecret")]
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
