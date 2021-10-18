using Omnikeeper.Base.Entity;

namespace OKPluginNaemonConfig.Entity
{
    [TraitEntity("hosts_category", TraitOriginType.Core)]
    public class HostsCategory : TraitEntity
    {
        [TraitAttribute("id", "cmdb.host_category_categoryid")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        [TraitEntityID]
        public readonly string Id;

        [TraitAttribute("hostid", "cmdb.host_category_hostid")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        public readonly string HostId;

        [TraitAttribute("cattree", "cmdb.host_category_cattree")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        public readonly string CatTree;

        [TraitAttribute("catgroup", "cmdb.host_category_catgroup")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        public readonly string CatGroup;

        [TraitAttribute("category", "cmdb.host_category_category")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        public readonly string Category;

        [TraitAttribute("catdesc", "cmdb.host_category_catdesc")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        public readonly string CatDesc;

        public HostsCategory()
        {
            Id = "";
            HostId = "";
            CatTree = "";
            CatGroup = "";
            Category = "";
            CatDesc = "";
        }
    }
}
