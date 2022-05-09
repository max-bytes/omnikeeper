using Omnikeeper.Base.Entity;

namespace OKPluginNaemonConfig.Entity
{
    [TraitEntity("host_action", TraitOriginType.Core)]
    public class HostAction : TraitEntity
    {
        [TraitAttribute("id", "cmdb.host_action.id")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        [TraitEntityID]
        public readonly string Id;

        [TraitAttribute("type", "cmdb.host_action.type")]
        public readonly string Type;

        [TraitAttribute("cmd", "cmdb.host_action.cmd")]
        public readonly string Cmd;

        [TraitAttribute("cmduser", "cmdb.host_action.cmduser")]
        public readonly string CmdUser;

        [TraitAttribute("hostid", "cmdb.host_action.hostid")]
        public readonly string HostId;

        public HostAction()
        {
            Id = "";
            HostId = "";
            Type = "";
            Cmd = "";
            CmdUser = "";
        }
    }
}
