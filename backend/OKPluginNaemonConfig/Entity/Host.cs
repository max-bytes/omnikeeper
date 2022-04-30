using Omnikeeper.Base.Entity;
using System;

namespace OKPluginNaemonConfig.Entity
{
    [TraitEntity("host", TraitOriginType.Core)]
    public class Host : TraitEntity
    {
        [TraitAttribute("id", "cmdb.host.id")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        [TraitEntityID]
        public readonly string Id;

        [TraitAttribute("hostname", "hostname")]
        public readonly string HostName;

        [TraitAttribute("environment", "cmdb.host.environment")]
        public readonly string Environment;

        [TraitAttribute("status", "cmdb.host.status")]
        public readonly string Status;

        [TraitAttribute("fkey", "cmdb.host.fkey")]
        public readonly string FKey;

        [TraitAttribute("fsource", "cmdb.host.fsource")]
        public readonly string FSource;

        [TraitAttribute("platform", "cmdb.host.platform")]
        public readonly string Platform;

        [TraitAttribute("address", "cmdb.host.mon_ip_address")]
        public readonly string Address;

        [TraitAttribute("port", "cmdb.host.mon_ip_port")]
        public readonly string Port;

        [TraitAttribute("cust", "cmdb.host.customer")]
        public readonly string Cust;

        [TraitAttribute("criticality", "cmdb.host.criticality")]
        public readonly string Criticality;

        [TraitAttribute("location", "cmdb.host.location")]
        public readonly string Location;

        [TraitAttribute("instance", "cmdb.host.instance")]
        public readonly string Instance;

        [TraitAttribute("os", "cmdb.host.os")]
        public readonly string OS;

        //NOTE currently not imported
        //[TraitAttribute("suppOS", "")]
        //public readonly string SuppOS;

        //NOTE currently not imported
        //[TraitAttribute("suppApp", "")]
        //public readonly string SuppApp;

        [TraitRelation("category", "has_category_member", false)]
        public readonly Guid[] CategoriesIds;

        [TraitRelation("interface", "has_interface", true)]
        public readonly Guid[] InterfacesIds;

        public Host()
        {
            Id = "";
            HostName = "";
            Environment = "";
            Status = "";
            FKey = "";
            FSource = "";
            Platform = "";
            Address = "";
            Port = "";
            Cust = "";
            Criticality = "";
            Location = "";
            Instance = "";
            OS = "";
            CategoriesIds = Array.Empty<Guid>();
            InterfacesIds = Array.Empty<Guid>();
        }

    }
}
