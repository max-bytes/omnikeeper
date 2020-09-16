using Landscape.Base.Inbound;
using Landscape.Base.Service;

namespace OKPluginOIASharepoint
{
    public class ScopedExternalIDMapper : ScopedExternalIDMapper<SharepointExternalListItemID>
    {
        public ScopedExternalIDMapper(string scope, IExternalIDMapPersister persister) : base(scope, persister, (s) => SharepointExternalListItemID.Deserialize(s))
        {
        }
    }
}
