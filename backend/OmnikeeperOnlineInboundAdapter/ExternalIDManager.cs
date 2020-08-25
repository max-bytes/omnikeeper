using Landscape.Base.Inbound;
using Landscape.Base.Model;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static OnlineInboundAdapterOmnikeeper.OnlineInboundAdapter;

namespace OnlineInboundAdapterOmnikeeper
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
        private readonly ILandscapeRegistryRESTAPIClient client;
        private readonly Config config;

        private const string ClientVersion = "1";

        public ExternalIDManager(ILandscapeRegistryRESTAPIClient client, Config config, ScopedExternalIDMapper mapper) : base(mapper, config.preferredIDMapUpdateRate)
        {
            this.client = client;
            this.config = config;
        }

        protected override async Task<IEnumerable<ExternalIDGuid>> GetExternalIDs()
        {
            var layers = await client.GetLayersByNameAsync(config.remoteLayerNames, ClientVersion);
            var layerIDs = layers.Select(l => l.Id);
            var ciids = await client.GetCIIDsOfNonEmptyCIsAsync(layerIDs, null, ClientVersion); // get all ciids from cis who have at least one attribute or relation in the specified layerset
            return ciids.Select(id => new ExternalIDGuid(id));
        }
    }
}
