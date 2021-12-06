using Omnikeeper.Base.Entity;

namespace OKPluginNaemonConfig.Entity
{
    [TraitEntity("interface", TraitOriginType.Core)]
    public class Interface : TraitEntity
    {
        [TraitAttribute("id", "cmdb.interface.id")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        [TraitEntityID]
        public readonly string Id;

        [TraitAttribute("type", "cmdb.interface.type")]
        public readonly string Type;

        [TraitAttribute("lantype", "cmdb.interface.lantype")]
        public readonly string LanType;

        [TraitAttribute("name", "cmdb.interface.name")]
        public readonly string Name;

        [TraitAttribute("ip", "cmdb.interface.ip")]
        public readonly string IP;

        [TraitAttribute("dns", "cmdb.interface.dns")]
        public readonly string DNSName;

        [TraitAttribute("vlan", "cmdb.interface.vlan")]
        public readonly string Vlan;

        public Interface()
        {
            Id = "";
            Type = "";
            LanType = "";
            Name = "";
            IP = "";
            DNSName = "";
            Vlan = "";
        }
    }
}
