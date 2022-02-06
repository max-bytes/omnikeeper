using Newtonsoft.Json.Linq;
using Omnikeeper.Base.Entity;

namespace OKPluginNaemonConfig.Entity
{
    [TraitEntity("naemon_instance", TraitOriginType.Core)]
    public class NaemonInstance : TraitEntity
    {
        [TraitAttribute("id", "naemon_instance.id")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        [TraitEntityID]
        public readonly string Id;

        [TraitAttribute("name", "naemon_instance.name")]
        public readonly string Name;

        [TraitAttribute("config", "config", optional: true)]
        public readonly JObject Config;
        public NaemonInstance()
        {
            Id = "";
            Name = "";
        }

    }
}
