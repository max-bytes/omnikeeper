using Omnikeeper.Base.Entity;

namespace OKPluginNaemonConfig.Entity
{
    [TraitEntity("naemon_instance", TraitOriginType.Core)]
    public class NaemonInstance : TraitEntity
    {
        [TraitAttribute("id", "naemon_instance.id")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        [TraitEntityID]
        public readonly string Id;

        public NaemonInstance()
        {
            Id = "";
        }

    }
}
