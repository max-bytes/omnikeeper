using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DTO;
using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OKPluginOIAOmnikeeper
{
    public class LayerAccessProxy : ILayerAccessProxy
    {
        private readonly string[] remoteLayerNames;
        private readonly ILandscapeomnikeeperRESTAPIClient client;
        private readonly ScopedExternalIDMapper mapper;
        private readonly Layer layer;

        private const string ClientVersion = "1";

        // TODO: changeset
        private static readonly Guid staticChangesetID = GuidUtility.Create(new Guid("a09018d6-d302-4137-acae-a81f2aa1a243"), "omnikeeper");

        public LayerAccessProxy(string[] remoteLayerNames, ILandscapeomnikeeperRESTAPIClient client, ScopedExternalIDMapper mapper, Layer layer)
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
            var ciid = mapper.GetCIID(new ExternalIDGuid(dto.CIID));

            if (ciid.HasValue)
            {
                return CIAttribute.Build(dto.ID, dto.Name, ciid.Value, AttributeValueBuilder.Build(dto.Value), AttributeState.New, staticChangesetID);
            }
            else return null;
        }

        private IEnumerable<Relation> RelationDTO2Regular(IEnumerable<RelationDTO> dto)
        {
            foreach (var relation in dto)
            {
                var r = RelationDTO2Regular(relation);
                if (r != null) yield return r;
            }
        }
        private Relation RelationDTO2Regular(RelationDTO dto)
        {
            // we need to reduce the relations to those whose related CIs are actually present in the mapper, to ensure that only relations of mapped cis are fetched
            var fromCIID = mapper.GetCIID(new ExternalIDGuid(dto.FromCIID));
            var toCIID = mapper.GetCIID(new ExternalIDGuid(dto.ToCIID));

            if (fromCIID.HasValue && toCIID.HasValue)
            {
                return Relation.Build(dto.ID, fromCIID.Value, toCIID.Value,
                    // TODO: can we just create a predicate on the fly?!? ignoring what predicates are actually present in the omnikeeper instance?
                    // apparently we can, because it seems to work, but does that work in all edge-cases?
                    Predicate.Build(dto.Predicate.ID, dto.Predicate.WordingFrom, dto.Predicate.WordingTo, AnchorState.Active, PredicateConstraints.Default),
                    RelationState.New, staticChangesetID);
            }
            else return null;
        }

        public async IAsyncEnumerable<CIAttribute> GetAttributes(ICIIDSelection selection, TimeThreshold atTime)
        {
            if (!atTime.IsLatest) yield break; // TODO: implement historic information

            IEnumerable<Guid> GetCIIDs(ICIIDSelection selection)
            {
                return selection switch
                {
                    AllCIIDsSelection _ => mapper.GetAllCIIDs(),
                    SpecificCIIDsSelection multiple => multiple.CIIDs,
                    _ => null,// must not be
                };
            }
            var ciids = GetCIIDs(selection).ToHashSet();

            // we need to map the ciids to external ciids, even if they are the same, to ensure that only mapped cis are fetched
            var IDPairs = mapper.GetIDPairs(ciids);

            if (IDPairs.IsEmpty()) yield break; // no ci maps, bail early

            var remoteLayers = await client.GetLayersByNameAsync(remoteLayerNames, ClientVersion);
            var remoteLayerIDs = remoteLayers.Select(rl => rl.ID).ToArray();

            var externalIDs = IDPairs.Select(p => p.Item2.ID);
            var attributesDTO = await client.GetMergedAttributesAsync(externalIDs, remoteLayerIDs, (atTime.IsLatest) ? (DateTimeOffset?)null : atTime.Time, ClientVersion);
            foreach (var a in AttributeDTO2Regular(attributesDTO))
                yield return a;
        }

        public async Task<CIAttribute> GetAttribute(string name, Guid ciid, TimeThreshold atTime)
        {
            if (!atTime.IsLatest) return null; // TODO: implement historic information

            var externalID = mapper.GetExternalID(ciid);
            if (externalID == null) return null;

            var remoteLayers = await client.GetLayersByNameAsync(remoteLayerNames, ClientVersion);
            var remoteLayerIDs = remoteLayers.Select(rl => rl.ID).ToArray();

            var attributeDTO = await client.GetMergedAttributeAsync(ciid, name, remoteLayerIDs, (atTime.IsLatest) ? (DateTimeOffset?)null : atTime.Time, ClientVersion);
            if (attributeDTO == null) return null;

            return AttributeDTO2Regular(attributeDTO);
        }

        public async IAsyncEnumerable<CIAttribute> FindAttributesByFullName(string name, ICIIDSelection selection, TimeThreshold atTime)
        {
            if (!atTime.IsLatest) yield break; // TODO: implement historic information

            var remoteLayers = await client.GetLayersByNameAsync(remoteLayerNames, ClientVersion);
            var remoteLayerIDs = remoteLayers.Select(rl => rl.ID).ToArray();

            var attributesDTO = await client.GetMergedAttributesWithNameAsync(name, remoteLayerIDs, (atTime.IsLatest) ? (DateTimeOffset?)null : atTime.Time, ClientVersion);


            foreach (var a in AttributeDTO2Regular(attributesDTO))
                if (selection.Contains(a.CIID)) // TODO, HACK: we fetch without doing any ciid filtering, rework Rest API endpoint to allow CIID filtering
                    yield return a;
        }

        public async IAsyncEnumerable<CIAttribute> FindAttributesByName(string regex, ICIIDSelection selection, TimeThreshold atTime)
        {
            if (!atTime.IsLatest) yield break; // TODO: implement historic information

            var remoteLayers = await client.GetLayersByNameAsync(remoteLayerNames, ClientVersion);
            var remoteLayerIDs = remoteLayers.Select(rl => rl.ID).ToArray();
            var time = (atTime.IsLatest) ? (DateTimeOffset?)null : atTime.Time;

            var ciids = selection switch
            {
                AllCIIDsSelection _ => null,
                SpecificCIIDsSelection m => m.CIIDs,
                _ => throw new NotImplementedException()
            };
            var attributesDTO = await client.FindMergedAttributesByNameAsync(regex, ciids, remoteLayerIDs, time, ClientVersion);

            foreach (var r in AttributeDTO2Regular(attributesDTO))
                yield return r;
        }

        public async IAsyncEnumerable<Relation> GetRelations(IRelationSelection rl, TimeThreshold atTime)
        {
            if (!atTime.IsLatest) yield break; // TODO: implement historic information

            var remoteLayers = await client.GetLayersByNameAsync(remoteLayerNames, ClientVersion);
            var remoteLayerIDs = remoteLayers.Select(rl => rl.ID).ToArray();
            var time = (atTime.IsLatest) ? (DateTimeOffset?)null : atTime.Time;

            var relationsDTO = rl switch
            {
                RelationSelectionFrom f => await client.GetMergedRelationsOutgoingFromCIAsync(f.fromCIID, remoteLayerIDs, time, ClientVersion),
                RelationSelectionWithPredicate p => await client.GetMergedRelationsWithPredicateAsync(p.predicateID, remoteLayerIDs, time, ClientVersion),
                RelationSelectionEitherFromOrTo fot => await client.GetMergedRelationsFromOrToCIAsync(fot.ciid, remoteLayerIDs, time, ClientVersion),
                RelationSelectionAll a => await client.GetAllMergedRelationsAsync(remoteLayerIDs, time, ClientVersion),
                _ => throw new NotImplementedException(),// must not be
            };

            // we need to reduce the relations to those whose related CIs are actually present in the mapper, to ensure that only relations of mapped cis are fetched
            foreach (var r in RelationDTO2Regular(relationsDTO))
                yield return r;
        }

        public async Task<Relation> GetRelation(Guid fromCIID, Guid toCIID, string predicateID, TimeThreshold atTime)
        {
            if (!atTime.IsLatest) return null; // TODO: implement historic information

            var remoteLayers = await client.GetLayersByNameAsync(remoteLayerNames, ClientVersion);
            var remoteLayerIDs = remoteLayers.Select(rl => rl.ID).ToArray();
            var time = (atTime.IsLatest) ? (DateTimeOffset?)null : atTime.Time;

            var relationDTO = await client.GetMergedRelationAsync(fromCIID, toCIID, predicateID, remoteLayerIDs, time, ClientVersion);
            if (relationDTO == null) return null;

            return RelationDTO2Regular(relationDTO);
        }
    }
}
