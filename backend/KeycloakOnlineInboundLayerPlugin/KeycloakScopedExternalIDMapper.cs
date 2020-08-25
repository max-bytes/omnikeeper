using Landscape.Base.Inbound;
using Landscape.Base.Service;
using System;
using System.Collections.Generic;
using System.Text;

namespace OnlineInboundAdapterKeycloak
{
    public class KeycloakScopedExternalIDMapper : ScopedExternalIDMapper<ExternalIDString>
    {
        public KeycloakScopedExternalIDMapper(string scope, IExternalIDMapPersister persister) : base(scope, persister, (s) => new ExternalIDString(s))
        {
        }

        public override ICIIdentificationMethod GetIdentificationMethod(ExternalIDString externalID) => CIIdentificationMethodNoop.Build();
    }
}
