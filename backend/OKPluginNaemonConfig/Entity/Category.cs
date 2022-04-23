using Omnikeeper.Base.Entity;

namespace OKPluginNaemonConfig.Entity
{
    [TraitEntity("category", TraitOriginType.Core)]
    public class Category : TraitEntity
    {
        [TraitAttribute("id", "cmdb.category.id")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        [TraitEntityID]
        public readonly string Id;

        [TraitAttribute("cattree", "cmdb.category.tree", optional: true)]
        public readonly string CatTree;

        [TraitAttribute("catgroup", "cmdb.category.group", optional: true)]
        public readonly string CatGroup;

        [TraitAttribute("category", "cmdb.category.category", optional: true)]
        public readonly string Cat;

        [TraitAttribute("catdesc", "cmdb.category.desc", optional: true)]
        public readonly string CatDesc;

        public Category()
        {
            Id = "";
            CatTree = "";
            CatGroup = "";
            Cat = "";
            CatDesc = "";
        }

    }
}
