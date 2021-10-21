using Omnikeeper.Base.Entity;

namespace OKPluginNaemonConfig.Entity
{
    [TraitEntity("hosts_category", TraitOriginType.Core)]
    public class HostCategory : TraitEntity
    {
        [TraitAttribute("id", "cmdb.host_category_categoryid")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        [TraitEntityID]
        public readonly string Id;

        [TraitAttribute("hostid", "cmdb.host_category_hostid")]
        public readonly string HostId;

        [TraitAttribute("cattree", "cmdb.host_category_cattree")]
        public readonly string CatTree;

        [TraitAttribute("catgroup", "cmdb.host_category_catgroup")]
        public readonly string CatGroup;

        [TraitAttribute("category", "cmdb.host_category_category")]
        public readonly string Category;

        [TraitAttribute("catdesc", "cmdb.host_category_catdesc")]
        public readonly string CatDesc;

        public HostCategory()
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
