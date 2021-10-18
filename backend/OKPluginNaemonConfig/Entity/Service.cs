using Omnikeeper.Base.Entity;

namespace OKPluginNaemonConfig.Entity
{
    [TraitEntity("service", TraitOriginType.Core)]
    public class Service : TraitEntity
    {
        [TraitAttribute("id", "cmdb.id")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        [TraitEntityID]
        public readonly string Id;

        [TraitAttribute("name", "cmdb.name")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        public readonly string Name;

        [TraitAttribute("status", "cmdb.status")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        public readonly string Status;

        [TraitAttribute("environment", "cmdb.environment")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        public readonly string Environment;

        public Service()
        {
            Id = "";
            Name = "";
            Status = "";
            Environment = "";
        }
    }
}
