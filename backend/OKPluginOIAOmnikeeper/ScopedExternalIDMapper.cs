using Omnikeeper.Base.Inbound;
using System;

namespace OKPluginOIAOmnikeeper
{
    public class ScopedExternalIDMapper : ScopedExternalIDMapper<ExternalIDGuid>
    {
        public ScopedExternalIDMapper(IScopedExternalIDMapPersister persister) : base(persister, (s) => new ExternalIDGuid(Guid.Parse(s)))
        {
        }
    }
}
