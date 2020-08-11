using Landscape.Base.Entity;
using Landscape.Base.Inbound;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using LandscapeRegistry.Entity.AttributeValues;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        public async IAsyncEnumerable<CIAttribute> GetAttributes(ICIIDSelection selection, TimeThreshold atTime)
        {
            await mapper.Setup();

            if (!atTime.IsLatest) yield break; // TODO: implement historic information

            IEnumerable<Guid> GetCIIDs(ICIIDSelection selection)
            {
                return selection switch
                {
                    AllCIIDsSelection _ => mapper.GetAllCIIDs(),
                    MultiCIIDsSelection multiple => multiple.CIIDs,
                    SingleCIIDSelection single => new Guid[] { single.CIID },
                    _ => null,// must not be
                };
            }
            var ciids = GetCIIDs(selection).ToHashSet();

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

        public async Task<CIAttribute> GetAttribute(string name, Guid ciid, TimeThreshold atTime)
        {
            await mapper.Setup();

            if (!atTime.IsLatest) return null; // TODO: implement historic information

            //var externalID = mapper.GetExternalID(ciid);

            throw new NotImplementedException(); // TODO
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

        public async IAsyncEnumerable<CIAttribute> FindAttributesByName(string regex, TimeThreshold atTime, Guid? ciid)
        {
            await mapper.Setup();

            if (!atTime.IsLatest) yield break; // TODO: implement historic information

            if (ciid.HasValue)
            {
                //var externalID = mapper.GetExternalID(ciid.Value);

                throw new NotImplementedException(); // TODO
            }
            else
            {
                throw new NotImplementedException(); // TODO
            }
        }

        public IAsyncEnumerable<Relation> GetRelations(IRelationSelection rl, TimeThreshold atTime)
        {
            return AsyncEnumerable.Empty<Relation>();// TODO: implement
        }

        public async Task<Relation> GetRelation(Guid fromCIID, Guid toCIID, string predicateID, TimeThreshold atTime)
        {
            return null; // TODO: implement
        }
    }
}
