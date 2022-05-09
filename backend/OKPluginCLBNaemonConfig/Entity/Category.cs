using Omnikeeper.Base.Entity;

namespace OKPluginNaemonConfig.Entity
{
    [TraitEntity("category", TraitOriginType.Core)]
    public class Category : TraitEntity
    {
        [TraitAttribute("id", "cmdb.category_id")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        [TraitEntityID]
        public readonly string Id;

        [TraitAttribute("cattree", "cmdb.category_tree")]
        public readonly string CatTree;

        [TraitAttribute("catgroup", "cmdb.category_group")]
        public readonly string CatGroup;

        [TraitAttribute("category", "cmdb.category_category")]
        public readonly string Cat;

        [TraitAttribute("catdesc", "cmdb.category_desc")]
        public readonly string CatDesc;

        public Category()
        {
            Id = "";
            CatTree = "";
            CatGroup = "";
            Cat = "";
            CatDesc = "";
        }

        /*
         				attribute('cmdb.category_id', CATEGORYID || ''),
				attribute('cmdb.category_tree', CATTREE || ''),
				attribute('cmdb.category_group', CATGROUP || ''),
				attribute('cmdb.category_category', CATEGORY || ''),
				attribute('cmdb.category_desc', CATDESC || ''),
				attribute('cmdb.category_owner', CATOWNER || ''),
				attribute('cmdb.category_instance', CATINSTANCE || '')

         
         */
    }
}
