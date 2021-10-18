using Omnikeeper.Base.Entity;

namespace OKPluginNaemonConfig.Entity
{
    [TraitEntity("services_category", TraitOriginType.Core)]
    public class ServicesCategory : TraitEntity
    {
        [TraitAttribute("id", "cmdb.service_category_categoryid")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        [TraitEntityID]
        public readonly string Id;

        [TraitAttribute("svcid", "cmdb.service_category_svcid")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        public readonly string ServiceId;

        [TraitAttribute("cattree", "cmdb.service_category_cattree")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        public readonly string CatTree;

        [TraitAttribute("catgroup", "cmdb.service_category_catgroup")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        public readonly string CatGroup;

        [TraitAttribute("category", "cmdb.service_category_category")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        public readonly string Category;

        [TraitAttribute("catdesc", "cmdb.service_category_catdesc")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        public readonly string CatDesc;

        public ServicesCategory()
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
