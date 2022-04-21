using Omnikeeper.Base.Entity;

namespace OKPluginNaemonConfig.Entity
{
    [TraitEntity("module", TraitOriginType.Core)]
    public class Module : TraitEntity
    {
        [TraitAttribute("id", "naemon_module.id")]
        public readonly long Id;

        [TraitAttribute("name", "naemon_module.name")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        [TraitEntityID]
        public readonly string Name;

        [TraitAttribute("type", "naemon_module.type")]
        public readonly long Type;

        public Module()
        {
            Name = "";
        }
    }
}
