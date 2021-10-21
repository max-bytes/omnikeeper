using Omnikeeper.Base.Entity;

namespace OKPluginNaemonConfig.Entity
{
    [TraitEntity("service_action", TraitOriginType.Core)]
    public class ServiceAction : TraitEntity
    {
        [TraitAttribute("id", "cmdb.service_action_id")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        [TraitEntityID]
        public readonly string Id;

        [TraitAttribute("type", "cmdb.service_action_type")]
        public readonly string Type;

        [TraitAttribute("cmd", "cmdb.service_action_cmd")]
        public readonly string Cmd;

        [TraitAttribute("cmduser", "cmdb.service_action_cmduser")]
        public readonly string CmdUser;

        [TraitAttribute("svcid", "cmdb.service_action_svcid")]
        public readonly string ServiceId;

        public ServiceAction()
        {
            Id = "";
            ServiceId = "";
            Type = "";
            Cmd = "";
            CmdUser = "";
        }
    }
}
