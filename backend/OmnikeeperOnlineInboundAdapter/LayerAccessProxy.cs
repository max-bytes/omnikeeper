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

        private const string ClientVersion = "1";

        // TODO: changeset
        private static readonly Guid staticChangesetID = GuidUtility.Create(new Guid("a09018d6-d302-4137-acae-a81f2aa1a243"), "omnikeeper");

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
                var r = AttributeDTO2Regular(attribute);
                if (r != null) yield return r;
            }
        }
        private CIAttribute AttributeDTO2Regular(CIAttributeDTO dto)
        {
            // we force a mapping to ensure only attributes of properly mapped cis are used
            var ciid = mapper.GetCIID(new ExternalIDGuid(dto.Ciid));

            if (ciid.HasValue)
            {
                // TODO: because we use a code generator, it does not use our own DTO classes but generates its own
                // and we need to manually do a mapping here -> sucks, make that work
                return CIAttribute.Build(dto.Id, dto.Name, ciid.Value,
                    AttributeValueBuilder.Build(Landscape.Base.Entity.DTO.AttributeValueDTO.Build(dto.Value.Values.ToArray(), dto.Value.IsArray, (LandscapeRegistry.Entity.AttributeValues.AttributeValueType)dto.Value.Type)),
                    Landscape.Base.Entity.AttributeState.New, staticChangesetID);
            }
            else return null;
        }

        private IEnumerable<Relation> RelationDTO2Regular(IEnumerable<RelationDTO> dto)
        {
            foreach (var relation in dto)
            {
                // we need to reduce the relations to those whose related CIs are actually present in the mapper, to ensure that only relations of mapped cis are fetched
                var fromCIID = mapper.GetCIID(new ExternalIDGuid(relation.FromCIID));
                var toCIID = mapper.GetCIID(new ExternalIDGuid(relation.ToCIID));

                if (fromCIID.HasValue && toCIID.HasValue)
                {
                    // TODO: because we use a code generator, it does not use our own DTO classes but generates its own
                    // and we need to manually do a mapping here -> sucks, make that work
                    yield return Relation.Build(relation.Id, fromCIID.Value, toCIID.Value,
                        // TODO: can we just create a predicate on the fly?!? ignoring what predicates are actually present in the omnikeeper instance?
                        Predicate.Build(relation.Predicate.Id, relation.Predicate.WordingFrom, relation.Predicate.WordingTo, AnchorState.Active, PredicateConstraints.Default),
                        Landscape.Base.Entity.RelationState.New, staticChangesetID);
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

            var remoteLayers = await client.GetLayersByNameAsync(remoteLayerNames, ClientVersion);
            var remoteLayerIDs = remoteLayers.Select(rl => rl.Id).ToArray();

            var externalIDs = IDPairs.Select(p => p.Item2.ID);
            var attributesDTO = await client.GetMergedAttributesAsync(externalIDs, remoteLayerIDs, (atTime.IsLatest) ? (DateTimeOffset?)null : atTime.Time, ClientVersion);
            foreach (var a in AttributeDTO2Regular(attributesDTO))
                yield return a;
        }

        public async Task<CIAttribute> GetAttribute(string name, Guid ciid, TimeThreshold atTime)
        {
            await mapper.Setup();

            if (!atTime.IsLatest) return null; // TODO: implement historic information

            var externalID = mapper.GetExternalID(ciid);
            if (externalID == null) return null;

            var remoteLayers = await client.GetLayersByNameAsync(remoteLayerNames, ClientVersion);
            var remoteLayerIDs = remoteLayers.Select(rl => rl.Id).ToArray();

            var attributeDTO = await client.GetMergedAttributeAsync(ciid, name, remoteLayerIDs, (atTime.IsLatest) ? (DateTimeOffset?)null : atTime.Time, ClientVersion);
            if (attributeDTO == null) return null;

            return AttributeDTO2Regular(attributeDTO);
        }

        public async IAsyncEnumerable<CIAttribute> FindAttributesByFullName(string name, ICIIDSelection selection, TimeThreshold atTime)
        {
            await mapper.Setup();

            if (!atTime.IsLatest) yield break; // TODO: implement historic information

            var remoteLayers = await client.GetLayersByNameAsync(remoteLayerNames, ClientVersion);
            var remoteLayerIDs = remoteLayers.Select(rl => rl.Id).ToArray();

            var attributesDTO = await client.GetMergedAttributesWithNameAsync(name, remoteLayerIDs, (atTime.IsLatest) ? (DateTimeOffset?)null : atTime.Time, ClientVersion);


            foreach (var a in AttributeDTO2Regular(attributesDTO))
                if (selection.Contains(a.CIID)) // TODO, HACK: we fetch without doing any ciid filtering, rework Rest API endpoint to allow CIID filtering
                    yield return a;
        }

        public async IAsyncEnumerable<CIAttribute> FindAttributesByName(string regex, ICIIDSelection selection, TimeThreshold atTime)
        {
            await mapper.Setup();

            if (!atTime.IsLatest) yield break; // TODO: implement historic information

            throw new NotImplementedException(); // TODO
        }

        public async IAsyncEnumerable<Relation> GetRelations(IRelationSelection rl, TimeThreshold atTime)
        {
            await mapper.Setup();

            if (!atTime.IsLatest) yield break; // TODO: implement historic information

            var remoteLayers = await client.GetLayersByNameAsync(remoteLayerNames, ClientVersion);
            var remoteLayerIDs = remoteLayers.Select(rl => rl.Id).ToArray();

            var relationsDTO = rl switch
            {
                RelationSelectionFrom f => await client.GetMergedRelationsOutgoingFromCIAsync(f.fromCIID, remoteLayerIDs, (atTime.IsLatest) ? (DateTimeOffset?)null : atTime.Time, ClientVersion),
                RelationSelectionWithPredicate p => await client.GetMergedRelationsWithPredicateAsync(p.predicateID, remoteLayerIDs, (atTime.IsLatest) ? (DateTimeOffset?)null : atTime.Time, ClientVersion),
                RelationSelectionEitherFromOrTo fot => await client.GetMergedRelationsFromOrToCIAsync(fot.ciid, remoteLayerIDs, (atTime.IsLatest) ? (DateTimeOffset?)null : atTime.Time, ClientVersion),
                RelationSelectionAll a => await client.GetAllMergedRelationsAsync(remoteLayerIDs, (atTime.IsLatest) ? (DateTimeOffset?)null : atTime.Time, ClientVersion),
                _ => null,// must not be
            };

            // we need to reduce the relations to those whose related CIs are actually present in the mapper, to ensure that only relations of mapped cis are fetched
            foreach (var r in RelationDTO2Regular(relationsDTO))
                yield return r;
        }

        public async Task<Relation> GetRelation(Guid fromCIID, Guid toCIID, string predicateID, TimeThreshold atTime)
        {
            return null; // TODO: implement
        }
    }
}
