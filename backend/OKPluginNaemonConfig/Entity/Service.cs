using Omnikeeper.Base.Entity;
using System;

namespace OKPluginNaemonConfig.Entity
{
    [TraitEntity("service", TraitOriginType.Core)]
    public class Service : TraitEntity
    {
        [TraitAttribute("id", "cmdb.service.id")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        [TraitEntityID]
        public readonly string Id;

        [TraitAttribute("name", "cmdb.service.name")]
        public readonly string Name;

        [TraitAttribute("status", "cmdb.service.status")]
        public readonly string Status;

        [TraitAttribute("environment", "cmdb.service.environment")]
        public readonly string Environment;

        [TraitAttribute("address", "cmdb.service.mon_ip_addres")]
        public readonly string Address;

        [TraitAttribute("port", "cmdb.service.mon_ip_port")]
        public readonly string Port;

        [TraitAttribute("cust", "cmdb.service.customer")]
        public readonly string Cust;

        [TraitAttribute("criticality", "cmdb.service.criticality")]
        public readonly string Criticality;

        [TraitAttribute("fkey", "cmdb.service.fkey")]
        public readonly string FKey;

        [TraitAttribute("fsource", "cmdb.service.fsource")]
        public readonly string FSource;

        [TraitAttribute("instance", "cmdb.service.instance")]
        public readonly string Instance;

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

        [TraitRelation("host", "runs_on", true)]
        public readonly Guid[] Hosts;

        public Service()
        {
            Id = "";
            Name = "";
            Status = "";
            Environment = "";
            Address = "";
            Port = "";
            Cust = "";
            Criticality = "";
            FKey = "";
            FSource = "";
            Instance = "";
            CategoriesIds = Array.Empty<Guid>();
            InterfacesIds = Array.Empty<Guid>();
            Hosts = Array.Empty<Guid>();
        }
    }
}
