using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Service;
using System;
using System.Collections.Generic;
using System.Text;

namespace OKPluginOIAOmnikeeper
{
    public class ScopedExternalIDMapper : ScopedExternalIDMapper<ExternalIDGuid>
    {
        public ScopedExternalIDMapper(IScopedExternalIDMapPersister persister) : base(persister, (s) => new ExternalIDGuid(Guid.Parse(s)))
        {
        }
    }
}
