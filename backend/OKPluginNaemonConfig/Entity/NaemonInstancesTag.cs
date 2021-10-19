﻿using Omnikeeper.Base.Entity;

namespace OKPluginNaemonConfig.Entity
{
    [TraitEntity("instances_tag", TraitOriginType.Core)]
    public class NaemonInstancesTag : TraitEntity
    {
        // NOTE not all rows are selected correctly here 
        [TraitAttribute("id", "naemon_instance_tag.id")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        [TraitEntityID]
        public readonly string Id;

        [TraitAttribute("tag", "naemon_instance_tag.tag")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        [TraitEntityID]
        public readonly string Tag;

        public NaemonInstancesTag()
        {
            Id = "";
            Tag = "";
        }
    }
}
