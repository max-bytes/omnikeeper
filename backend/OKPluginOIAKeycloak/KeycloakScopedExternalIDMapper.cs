using Omnikeeper.Base.Inbound;

namespace OKPluginOIAKeycloak
{
    public class KeycloakScopedExternalIDMapper : ScopedExternalIDMapper<ExternalIDString>
    {
        public KeycloakScopedExternalIDMapper(IScopedExternalIDMapPersister persister) : base(persister, (s) => new ExternalIDString(s))
        {
        }
    }
}
