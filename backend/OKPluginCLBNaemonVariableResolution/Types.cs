using Omnikeeper.Base.Entity;
using System.Text.Json;

namespace OKPluginCLBNaemonVariableResolution
{

    [TraitEntity("monman_v2.varres.naemon_variable_v1", TraitOriginType.Plugin)]
    public class NaemonVariableV1 : TraitEntity
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

        [TraitAttribute("isSecret", "naemon_variable.issecret")]
        public long isSecretLong;
        public bool isSecret => isSecretLong != 0L;

        public NaemonVariableV1()
        {
            ID = 0L;
            refType = "";
            refID = "";
            name = "";
            value = "";
            precedence = 0L;
            isSecretLong = 0L;
        }
    }

    [TraitEntity("monman_v2.varres.selfservice_variable", TraitOriginType.Plugin)]
    public class SelfServiceVariable : TraitEntity
    {
        [TraitAttribute("refType", "naemon_variable.reftype")]
        [TraitEntityID]
        public string refType;

        [TraitAttribute("refID", "naemon_variable.refid")]
        [TraitEntityID]
        public string refID;

        [TraitAttribute("name", "naemon_variable.name")]
        [TraitEntityID]
        public string name;

        [TraitAttribute("value", "naemon_variable.value")]
        public string value;

        public SelfServiceVariable()
        {
            refType = "";
            refID = "";
            name = "";
            value = "";
        }
    }

    [TraitEntity("monman_v2.varres.naemon_instance_v1", TraitOriginType.Plugin)]
    public class NaemonInstanceV1 : TraitEntity
    {
        [TraitAttribute("id", "naemon_instance.id")]
        [TraitEntityID]
        public string ID;

        [TraitAttribute("name", "naemon_instance.name")]
        public string Name;

        [TraitRelation("tags", "has_tag", true)]
        public Guid[] Tags;

        [TraitRelation("monitoringTargets", "monitors", true)]
        public Guid[] MonitoringTargets;

        [TraitRelation("runsOn", "runs_on", true, new string[] { "tsa_cmdb.host" })]
        public Guid? RunsOn;

        public NaemonInstanceV1()
        {
            ID = "";
            Name = "";
            Tags = Array.Empty<Guid>();
            MonitoringTargets = Array.Empty<Guid>();
            RunsOn = null;
        }
    }

    [TraitEntity("monman_v2.varres.tag_v1", TraitOriginType.Plugin)]
    public class TagV1 : TraitEntity
    {
        [TraitAttribute("name", "naemon_instance_tag.tag")]
        public string Name;

        public TagV1()
        {
            Name = "";
        }
    }

    [TraitEntity("monman_v2.varres.target", TraitOriginType.Plugin)]
    public class Target : TraitEntity
    {
        [TraitAttribute("id", "cmdb.id")]
        [TraitEntityID]
        public string ID;

        [TraitAttribute("resolvedVariables", "monman_v2.resolved_variables")]
        public JsonDocument ResolvedVariables;

        [TraitAttribute("useDirective", "monman_v2.use_directive")]
        public string[] UseDirective;

        [TraitRelation("monitoredByThrukHosts", "is_monitored_by_thruk_host", true, new string[] { "monman_v2.thruk_host" })]
        public Guid[] MonitoredByThrukHosts;

        public Target()
        {
            ID = "";
            ResolvedVariables = null;
            UseDirective = Array.Empty<string>();
            MonitoredByThrukHosts = Array.Empty<Guid>();
        }
    }

    [TraitEntity("monman_v2.varres.target_host", TraitOriginType.Plugin)]
    public class TargetHost : TraitEntity
    {
        [TraitAttribute("hostID", "cmdb.host.id")]
        [TraitEntityID]
        public string ID;

        [TraitAttribute("hostname", "hostname", optional: true)]
        public string? Hostname;

        [TraitAttribute("location", "cmdb.host.location", optional: true)]
        public string? Location;

        [TraitAttribute("os", "cmdb.host.os", optional: true)]
        public string? OS;

        [TraitAttribute("platform", "cmdb.host.platform", optional: true)]
        public string? Platform;

        [TraitAttribute("status", "cmdb.host.status", optional: true)]
        public string? Status;

        [TraitAttribute("customerNickname", "cmdb.host.customer")]
        public string CustomerNickname;

        [TraitAttribute("environment", "cmdb.host.environment", optional: true)]
        public string? Environment;

        [TraitAttribute("monIPAddress", "cmdb.host.mon_ip_address", optional: true)]
        public string? MonIPAddress;

        [TraitAttribute("monIPPort", "cmdb.host.mon_ip_port", optional: true)]
        public string? MonIPPort;

        [TraitAttribute("instance", "cmdb.host.instance", optional: true)]
        public string? Instance;

        [TraitAttribute("criticality", "cmdb.host.criticality", optional: true)]
        public string? Criticality;

        [TraitAttribute("foreignSource", "cmdb.host.fsource", optional: true)]
        public string? ForeignSource;

        [TraitAttribute("foreignKey", "cmdb.host.fkey", optional: true)]
        public string? ForeignKey;

        [TraitRelation("interfaces", "has_interface", true)]
        public Guid[] Interfaces;

        [TraitRelation("appSupportGroup", "belongs_to_host_app_support_group", true)]
        public Guid? AppSupportGroup;

        [TraitRelation("osSupportGroup", "belongs_to_host_support_group", true)]
        public Guid? OSSupportGroup;

        [TraitRelation("memberOfCategories", "has_category_member", false)]
        public Guid[] MemberOfCategories;

        [TraitRelation("runsOn", "runs_on", true)]
        public Guid[] RunsOn;

        public TargetHost()
        {
            ID = "";
            Hostname = null;
            Location = null;
            OS = null;
            Platform = null;
            Status = null;
            CustomerNickname = "";
            Environment = null;
            MonIPAddress = null;
            MonIPPort = null;
            Instance = null;
            Criticality = null;
            ForeignSource = null;
            ForeignKey = null;
            Interfaces = Array.Empty<Guid>();
            AppSupportGroup = null;
            OSSupportGroup = null;
            MemberOfCategories = Array.Empty<Guid>();
            RunsOn = Array.Empty<Guid>();
        }
    }

    [TraitEntity("monman_v2.varres.target_service", TraitOriginType.Plugin)]
    public class TargetService : TraitEntity
    {
        [TraitAttribute("serviceID", "cmdb.service.id")]
        [TraitEntityID]
        public string ID;

        [TraitAttribute("name", "cmdb.service.name", optional: true)]
        public string? Name;

        [TraitAttribute("class", "cmdb.service.class", optional: true)]
        public string? Class;

        [TraitAttribute("status", "cmdb.service.status", optional: true)]
        public string? Status;

        [TraitAttribute("customerNickname", "cmdb.service.customer")]
        public string CustomerNickname;

        [TraitAttribute("environment", "cmdb.service.environment", optional: true)]
        public string? Environment;

        [TraitAttribute("criticality", "cmdb.service.criticality", optional: true)]
        public string? Criticality;

        [TraitAttribute("foreignSource", "cmdb.service.fsource", optional: true)]
        public string? ForeignSource;

        [TraitAttribute("foreignKey", "cmdb.service.fkey", optional: true)]
        public string? ForeignKey;

        [TraitAttribute("instance", "cmdb.service.instance", optional: true)]
        public string? Instance;

        [TraitAttribute("type", "cmdb.service.type", optional: true)]
        public string? Type;

        [TraitAttribute("monIPAddress", "cmdb.service.mon_ip_address", optional: true)]
        public string? MonIPAddress;

        [TraitAttribute("monIPPort", "cmdb.service.mon_ip_port", optional: true)]
        public string? MonIPPort;

        [TraitRelation("osSupportGroup", "belongs_to_service_support_group", true)]
        public Guid? OSSupportGroup;

        [TraitRelation("appSupportGroup", "belongs_to_service_app_support_group", true)]
        public Guid? AppSupportGroup;

        [TraitRelation("memberOfCategories", "has_category_member", false)]
        public Guid[] MemberOfCategories;

        [TraitRelation("runsOn", "runs_on", true)]
        public Guid[] RunsOn;

        public TargetService()
        {
            ID = "";
            Name = null;
            Class = null;
            Status = null;
            CustomerNickname = "";
            Environment = null;
            Criticality = null;
            ForeignSource = null;
            ForeignKey = null;
            Instance = null;
            Type = null;
            MonIPAddress = null;
            MonIPPort = null;
            OSSupportGroup = null;
            AppSupportGroup = null;
            MemberOfCategories = Array.Empty<Guid>();
            RunsOn = Array.Empty<Guid>();
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

        [TraitAttribute("tree", "cmdb.category.tree")]
        public string Tree;

        [TraitAttribute("group", "cmdb.category.group")]
        public string Group;

        [TraitAttribute("instance", "cmdb.category.instance")]
        public string Instance;

        [TraitRelation("members", "has_category_member", true)]
        public Guid[] Members;

        public Category()
        {
            ID = "";
            Name = "";
            Tree = "";
            Group = "";
            Instance = "";
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

    [TraitEntity("monman_v2.varres.service_action", TraitOriginType.Plugin)]
    public class ServiceAction : TraitEntity
    {
        [TraitAttribute("id", "cmdb.service_action.id")]
        [TraitEntityID]
        public string ID;

        [TraitAttribute("serviceID", "cmdb.service_action.svcid")]
        public string ServiceID;

        [TraitAttribute("command", "cmdb.service_action.cmd", optional: true)]
        public string? Command;

        [TraitAttribute("commandUser", "cmdb.service_action.cmduser", optional: true)]
        public string? CommandUser;

        [TraitAttribute("type", "cmdb.service_action.type")]
        public string Type;

        public ServiceAction()
        {
            ID = "";
            ServiceID = "";
            Command = null;
            CommandUser = null;
            Type = "";
        }
    }

    [TraitEntity("monman_v2.varres.interface", TraitOriginType.Plugin)]
    public class Interface : TraitEntity
    {
        [TraitAttribute("id", "cmdb.interface.id")]
        [TraitEntityID]
        public string ID;

        [TraitAttribute("name", "cmdb.interface.name", optional: true)]
        public string? Name;

        [TraitAttribute("lanType", "cmdb.interface.lantype", optional: true)]
        public string? LanType;

        [TraitAttribute("dnsName", "cmdb.interface.dns", optional: true)]
        public string? DnsName;

        [TraitAttribute("ip", "cmdb.interface.ip", optional: true)]
        public string? IP;

        public Interface()
        {
            ID = "";
            Name = null;
            LanType = null;
            DnsName = null;
            IP = null;
        }
    }

    [TraitEntity("monman_v2.varres.group", TraitOriginType.Plugin)]
    public class Group : TraitEntity
    {
        [TraitAttribute("id", "cmdb.group.id")]
        [TraitEntityID]
        public string ID;

        [TraitAttribute("name", "cmdb.group.name")]
        public string Name;

        public Group()
        {
            ID = "";
            Name = "";
        }
    }


    [TraitEntity("monman_v2.thruk_host", TraitOriginType.Plugin)]
    public class ThrukHost : TraitEntity
    {
        [TraitAttribute("name", "thruk.host.name")]
        [TraitEntityID]
        public string Name;

        [TraitAttribute("peerKey", "thruk.host.peer_key")]
        [TraitEntityID]
        public string PeerKey;

        [TraitAttribute("customVariables", "thruk.host.custom_variables")]
        public JsonDocument CustomVariables;

        [TraitAttribute("checkCommand", "thruk.host.check_command", optional: true)] // TODO: set optional: false
        public string CheckCommand;

        [TraitRelation("services", "belongs_to_thruk_host", false, new string[] { "monman_v2.thruk_service" })]
        public Guid[] Services;

        [TraitRelation("cmdbCI", "is_monitored_by_thruk_host", false, new string[] { "monman_v2.varres.target" })]
        public Guid? CMDBCI;

        public ThrukHost()
        {
            Name = "";
            PeerKey = "";
            CustomVariables = null;
            CheckCommand = "";
            Services = Array.Empty<Guid>();
            CMDBCI = null;
        }
    }

    [TraitEntity("monman_v2.thruk_service", TraitOriginType.Plugin)]
    public class ThrukService : TraitEntity
    {
        [TraitAttribute("hostName", "thruk.service.host_name")]
        [TraitEntityID]
        public string HostName;

        [TraitAttribute("peerKey", "thruk.service.peer_key")]
        [TraitEntityID]
        public string PeerKey;

        [TraitAttribute("description", "thruk.service.description")]
        [TraitEntityID]
        public string Description;

        [TraitAttribute("checkCommand", "thruk.service.check_command", optional: true)] // TODO: set optional: false
        public string CheckCommand;

        [TraitRelation("host", "belongs_to_thruk_host", true, new string[] { "monman_v2.thruk_host" })]
        public Guid? Host;

        public ThrukService()
        {
            HostName = "";
            PeerKey = "";
            Description = "";
            CheckCommand = "";
            Host = null;
        }
    }
}
