using Omnikeeper.Base.Entity;
using System;

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

        [TraitAttribute("address", "cmdb.mon_ip_address")]
        public readonly string Address;

        [TraitAttribute("port", "cmdb.mon_ip_port")]
        public readonly string Port;

        [TraitAttribute("cust", "cmdb.customer")]
        public readonly string Cust;

        [TraitAttribute("criticality", "cmdb.criticality")]
        public readonly string Criticality;

        //NOTE currently not imported
        //[TraitAttribute("suppOS", "")]
        //public readonly string SuppOS;

        //NOTE currently not imported
        //[TraitAttribute("suppApp", "")]
        //public readonly string SuppApp;

        [TraitRelation("category", "has_category_member", false, 1, -1)]
        public readonly Guid[] CategoriesIds;
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
            CategoriesIds = new Guid[0];
        }
    }
}
