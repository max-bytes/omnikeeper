using Omnikeeper.Base.Entity;

namespace OKPluginNaemonConfig.Entity
{
    [TraitEntity("host", TraitOriginType.Core)]
    public class Host : TraitEntity
    {
        [TraitAttribute("id", "cmdb.id")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        [TraitEntityID]
        public readonly string Id;

        [TraitAttribute("name", "hostname")]
        public readonly string Name;

        [TraitAttribute("status", "cmdb.status")]
        public readonly string Status;

        public Host()
        {
            Id = "";
            Name = "";
            Status = "";
        }

    }
}
