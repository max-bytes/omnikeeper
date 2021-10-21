using Omnikeeper.Base.Entity;

namespace OKPluginNaemonConfig.Entity
{
    [TraitEntity("instances_tag", TraitOriginType.Core)]
    public class NaemonInstanceTag : TraitEntity
    {
        // NOTE not all rows are selected correctly here since we have two columns as primary key
        // In this case only unique ids for Id columns are returned which is wrong
        [TraitAttribute("id", "naemon_instance_tag.id")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        [TraitEntityID]
        public readonly string Id;

        [TraitAttribute("tag", "naemon_instance_tag.tag")]
        public readonly string Tag;

        public NaemonInstanceTag()
        {
            Id = "";
            Tag = "";
        }
    }
}
