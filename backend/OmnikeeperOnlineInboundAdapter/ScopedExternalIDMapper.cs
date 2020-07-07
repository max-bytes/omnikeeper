using Landscape.Base.Inbound;
using System;
using System.Collections.Generic;
using System.Text;

namespace OnlineInboundAdapterOmnikeeper
{
    public class ScopedExternalIDMapper : ScopedExternalIDMapper<ExternalIDGuid>
    {
        public ScopedExternalIDMapper(string scope, IExternalIDMapPersister persister) : base(scope, persister, (s) => new ExternalIDGuid(Guid.Parse(s)))
        {
        }

        public override Guid? DeriveCIIDFromExternalID(ExternalIDGuid externalID) => externalID.ID;
    }
}
