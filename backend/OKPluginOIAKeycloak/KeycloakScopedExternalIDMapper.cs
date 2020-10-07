using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Service;
using System;
using System.Collections.Generic;
using System.Text;

namespace OKPluginOIAKeycloak
{
    public class KeycloakScopedExternalIDMapper : ScopedExternalIDMapper<ExternalIDString>
    {
        public KeycloakScopedExternalIDMapper(IScopedExternalIDMapPersister persister) : base(persister, (s) => new ExternalIDString(s))
        {
        }
    }
}
