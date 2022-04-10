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

        [TraitAttribute("address", "cmdb.service.mon_ip_address")]
        public readonly string Address;

        [TraitAttribute("port", "cmdb.service.mon_ip_port")]
        public readonly string Port;

        [TraitAttribute("cust", "cmdb.service.customer")]
        public readonly string Cust;

        [TraitAttribute("criticality", "cmdb.service.criticality")]
        public readonly string Criticality;

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
            CategoriesIds = Array.Empty<Guid>();
            InterfacesIds = Array.Empty<Guid>();
        }
    }
}
