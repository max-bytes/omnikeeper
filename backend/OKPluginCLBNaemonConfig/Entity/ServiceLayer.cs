using Omnikeeper.Base.Entity;

namespace OKPluginNaemonConfig.Entity
{
    [TraitEntity("service_layer", TraitOriginType.Core)]
    public class ServiceLayer : TraitEntity
    {
        [TraitAttribute("id", "naemon_service_layer.id")]
        public readonly long Id;

        [TraitAttribute("name", "naemon_service_layer.name", optional: true)]
        [TraitEntityID]
        public readonly string Name;

        [TraitAttribute("num", "naemon_service_layer.num")]
        public readonly long Num;

        public ServiceLayer()
        {
            Name = "";
        }
    }
}
