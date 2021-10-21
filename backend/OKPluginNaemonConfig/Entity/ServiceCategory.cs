using Omnikeeper.Base.Entity;

namespace OKPluginNaemonConfig.Entity
{
    [TraitEntity("services_category", TraitOriginType.Core)]
    public class ServiceCategory : TraitEntity
    {
        [TraitAttribute("id", "cmdb.service_category_categoryid")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        [TraitEntityID]
        public readonly string Id;

        [TraitAttribute("svcid", "cmdb.service_category_svcid")]
        public readonly string ServiceId;

        [TraitAttribute("cattree", "cmdb.service_category_cattree")]
        public readonly string CatTree;

        [TraitAttribute("catgroup", "cmdb.service_category_catgroup")]
        public readonly string CatGroup;

        [TraitAttribute("category", "cmdb.service_category_category")]
        public readonly string Category;

        [TraitAttribute("catdesc", "cmdb.service_category_catdesc")]
        public readonly string CatDesc;

        public ServiceCategory()
        {
            Id = "";
            ServiceId = "";
            CatTree = "";
            CatGroup = "";
            Category = "";
            CatDesc = "";
        }
    }
}
