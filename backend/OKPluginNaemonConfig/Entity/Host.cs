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

        [TraitAttribute("name", "hostname")]
        public readonly string Name;

        [TraitAttribute("environment", "cmdb.host.environment")]
        public readonly string Environment;

        [TraitAttribute("status", "cmdb.host.status")]
        public readonly string Status;

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


        // HSUP,HSUPAPP,HMONIPADDRESS,HMONIPPORT,HCRITICALITY,HCOMMENT,HINSTANCE,HCUST,HSERVICETIME,HOPERTIMEATTENDED,HOS,HLOCATION

        //NOTE currently not imported
        //[TraitAttribute("suppOS", "")]
        //public readonly string SuppOS;

        //NOTE currently not imported
        //[TraitAttribute("suppApp", "")]
        //public readonly string SuppApp;

        [TraitRelation("category", "has_category_member", false, 1, -1)]
        public readonly Guid[] CategoriesIds;

        [TraitRelation("interface", "has_interface", true, 1, -1)]
        public readonly Guid[] InterfacesIds;

        public Host()
        {
            Id = "";
            Name = "";
            Environment = "";
            Status = "";
            Platform = "";
            Address = "";
            Port = "";
            Cust = "";
            Criticality = "";
            CategoriesIds = Array.Empty<Guid>();
            InterfacesIds = Array.Empty<Guid>();
        }

    }
}
