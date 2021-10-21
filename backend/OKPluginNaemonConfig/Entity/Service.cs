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
        public readonly string Name;

        [TraitAttribute("status", "cmdb.status")]
        public readonly string Status;

        [TraitAttribute("environment", "cmdb.environment")]
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
