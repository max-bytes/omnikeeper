using Landscape.Base.Inbound;
using Landscape.Base.Service;

namespace OKPluginOIASharepoint
{
    public class ScopedExternalIDMapper : ScopedExternalIDMapper<SharepointExternalListItemID>
    {
        public ScopedExternalIDMapper(IScopedExternalIDMapPersister persister) : base(persister, (s) => SharepointExternalListItemID.Deserialize(s))
        {
        }
    }
}
