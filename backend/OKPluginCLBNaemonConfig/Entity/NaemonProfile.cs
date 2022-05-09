using Omnikeeper.Base.Entity;

namespace OKPluginNaemonConfig.Entity
{
    [TraitEntity("profile", TraitOriginType.Core)]
    public class NaemonProfile : TraitEntity
    {
        [TraitAttribute("id", "naemon_profile.id")]
        // NOTE when we use Id as TraitEntityID we get an error
        public readonly long Id;

        [TraitAttribute("name", "naemon_profile.name")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        [TraitEntityID]
        public readonly string Name;

        public NaemonProfile()
        {
            Name = "";
        }
    }
}
