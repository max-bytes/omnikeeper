using Landscape.Base.Entity;
using Landscape.Base.Inbound;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using LandscapeRegistry.Entity.AttributeValues;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OnlineInboundAdapterOmnikeeper
{
    public class LayerAccessProxy : IOnlineInboundLayerAccessProxy
    {
        private readonly string[] remoteLayerNames;
        private readonly ILandscapeRegistryRESTAPIClient client;
        private readonly ScopedExternalIDMapper mapper;
        private readonly Layer layer;

        public string Name => "Omnikeeper";

        public LayerAccessProxy(string[] remoteLayerNames, ILandscapeRegistryRESTAPIClient client, ScopedExternalIDMapper mapper, Layer layer)
        {
            this.remoteLayerNames = remoteLayerNames;
            this.client = client;
            this.mapper = mapper;
            this.layer = layer;
        }

        private IEnumerable<CIAttribute> AttributeDTO2Regular(IEnumerable<CIAttributeDTO> dto)
        {
            foreach (var attribute in dto)
            {
                // we force a mapping to ensure only attributes of properly mapped cis are used
                var ciid = mapper.GetCIID(new ExternalIDGuid(attribute.Id));

                if (ciid.HasValue)
                {
                    // TODO: because we use a code generator, it does not use our own DTO classes but generates its own
                    // and we need to manually do a mapping here -> sucks, make that work
                    yield return CIAttribute.Build(attribute.Id, attribute.Name, attribute.Ciid,
                        AttributeValueBuilder.Build(Landscape.Base.Entity.DTO.AttributeValueDTO.Build(attribute.Value.Values.ToArray(), attribute.Value.IsArray, (LandscapeRegistry.Entity.AttributeValues.AttributeValueType)attribute.Value.Type)),
                        Landscape.Base.Entity.AttributeState.New, -1); // TODO: changeset
                }
            }
        }

        public async IAsyncEnumerable<CIAttribute> GetAttributes(ISet<Guid> ciids, TimeThreshold atTime)
        {
            await mapper.Setup();

            if (!atTime.IsLatest) yield break; // TODO: implement historic information

            // we need to map the ciids to external ciids, even if they are the same, to ensure that only mapped cis are fetched
            var IDPairs = mapper.GetIDPairs(ciids);

            if (IDPairs.IsEmpty()) yield break; // no ci maps, bail early

            var remoteLayers = await client.GetLayersByNameAsync(remoteLayerNames, "1");
            var remoteLayerIDs = remoteLayers.Select(rl => rl.Id).ToArray();

            var externalIDs = IDPairs.Select(p => p.Item2.ID);
            var attributesDTO = await client.GetMergedAttributesAsync(externalIDs, remoteLayerIDs, (atTime.IsLatest) ? (DateTimeOffset?)null : atTime.Time, "1");
            foreach (var a in AttributeDTO2Regular(attributesDTO))
                yield return a;
        }

        public async IAsyncEnumerable<CIAttribute> GetAttributesWithName(string name, TimeThreshold atTime)
        {
            await mapper.Setup();

            if (!atTime.IsLatest) yield break; // TODO: implement historic information

            var remoteLayers = await client.GetLayersByNameAsync(remoteLayerNames, "1");
            var remoteLayerIDs = remoteLayers.Select(rl => rl.Id).ToArray();

            var attributesDTO = await client.GetMergedAttributesWithNameAsync(name, remoteLayerIDs, (atTime.IsLatest) ? (DateTimeOffset?)null : atTime.Time, "1");

            foreach (var a in AttributeDTO2Regular(attributesDTO))
                yield return a;
        }

        public IAsyncEnumerable<Relation> GetRelations(Guid? ciid, IRelationModel.IncludeRelationDirections ird, TimeThreshold atTime)
        {
            return AsyncEnumerable.Empty<Relation>();// TODO: implement
        }

        public IAsyncEnumerable<Relation> GetRelationsWithPredicateID(string predicateID, TimeThreshold atTime)
        {
            return AsyncEnumerable.Empty<Relation>();// TODO: implement
        }
    }
}
