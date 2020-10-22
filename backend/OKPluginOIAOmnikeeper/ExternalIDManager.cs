using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static OKPluginOIAOmnikeeper.OnlineInboundAdapter;

namespace OKPluginOIAOmnikeeper
{
    public class ExternalCI : IExternalItem<ExternalIDGuid>
    {
        public ExternalCI(Guid guid)
        {
            ID = new ExternalIDGuid(guid);
        }
        public ExternalIDGuid ID { get; }
    }
    public class ExternalIDManager : ExternalIDManager<ExternalIDGuid>
    {
        private readonly ILandscapeomnikeeperRESTAPIClient client;
        private readonly Config config;

        private const string ClientVersion = "1";

        public ExternalIDManager(ILandscapeomnikeeperRESTAPIClient client, Config config, ScopedExternalIDMapper mapper) : base(mapper, config.preferredIDMapUpdateRate)
        {
            this.client = client;
            this.config = config;
        }

        protected override async Task<IEnumerable<(ExternalIDGuid externalID, ICIIdentificationMethod idMethod)>> GetExternalIDs()
        {
            var layers = await client.GetLayersByNameAsync(config.remoteLayerNames, ClientVersion);
            var layerIDs = layers.Select(l => l.ID);
            var ciids = await client.GetCIIDsOfNonEmptyCIsAsync(layerIDs, null, ClientVersion); // get all ciids from cis who have at least one attribute or relation in the specified layerset
            return ciids.Select(id => (new ExternalIDGuid(id), (ICIIdentificationMethod)CIIdentificationMethodByCIID.Build(id)));
        }
    }
}
