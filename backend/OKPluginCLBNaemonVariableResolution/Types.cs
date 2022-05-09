using Omnikeeper.Base.Entity;

namespace OKPluginCLBNaemonVariableResolution
{

    [TraitEntity("monman_v2.varres.naemon_v1_variable", TraitOriginType.Plugin)]
    public class NaemonV1Variable : TraitEntity
    {
        [TraitAttribute("id", "naemon_variable.id")]
        [TraitEntityID]
        public long ID;

        [TraitAttribute("refType", "naemon_variable.reftype")]
        public string refType;

        [TraitAttribute("refID", "naemon_variable.refid")]
        public string refID;

        [TraitAttribute("name", "naemon_variable.name")]
        public string name;

        [TraitAttribute("value", "naemon_variable.value")]
        public string value;

        [TraitAttribute("precedence", "naemon_variable.precedence")]
        public long precedence;

        public NaemonV1Variable()
        {
            ID = 0L;
            this.refType = "";
            this.refID = "";
            this.name = "";
            this.value = "";
            this.precedence = 0L;
        }
    }

    [TraitEntity("monman_v2.varres.target_host", TraitOriginType.Plugin)]
    public class TargetHost : TraitEntity
    {
        [TraitAttribute("hostID", "cmdb.host.id")]
        [TraitEntityID]
        public string ID;

        public TargetHost()
        {
            ID = "";
        }
    }

    [TraitEntity("monman_v2.varres.target_service", TraitOriginType.Plugin)]
    public class TargetService : TraitEntity
    {
        [TraitAttribute("serviceID", "cmdb.service.id")]
        [TraitEntityID]
        public string ID;

        public TargetService()
        {
            ID = "";
        }
    }

    [TraitEntity("monman_v2.varres.profile", TraitOriginType.Plugin)]
    public class Profile : TraitEntity
    {
        [TraitAttribute("id", "naemon_profile.id")]
        [TraitEntityID]
        public long ID;

        [TraitAttribute("name", "naemon_profile.name")]
        public string Name;

        public Profile()
        {
            ID = 0L;
            Name = "";
        }
    }

    [TraitEntity("monman_v2.varres.category", TraitOriginType.Plugin)]
    public class Category : TraitEntity
    {
        [TraitAttribute("id", "cmdb.category.id")]
        [TraitEntityID]
        public string ID;

        [TraitAttribute("name", "cmdb.category.category")]
        public string Name;

        [TraitRelation("members", "has_category_member", true)]
        public Guid[] Members;

        public Category()
        {
            ID = "";
            Name = "";
            Members = Array.Empty<Guid>();
        }
    }

    [TraitEntity("monman_v2.varres.customer", TraitOriginType.Plugin)]
    public class Customer : TraitEntity
    {
        [TraitAttribute("id", "cmdb.customer.id")]
        [TraitEntityID]
        public string ID;

        [TraitAttribute("nickname", "cmdb.customer.nickname")]
        public string Nickname;

        [TraitRelation("associatedCIs", "is_assigned_to_customer", false)]
        public Guid[] AssociatedCIs;

        public Customer()
        {
            ID = "";
            Nickname = "";
            AssociatedCIs = Array.Empty<Guid>();
        }
    }
}
