using Omnikeeper.Base.AttributeValues;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Entity.DTO;
using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
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
        private CIAttribute? AttributeDTO2Regular(CIAttributeDTO dto)
        {
            // we force a mapping to ensure only attributes of properly mapped cis are used
            var ciid = mapper.GetCIID(new ExternalIDGuid(dto.CIID));

            if (ciid.HasValue)
            {
                return new CIAttribute(dto.ID, dto.Name, ciid.Value, AttributeValueHelper.BuildFromDTO(dto.Value), staticChangesetID);
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
        private Relation? RelationDTO2Regular(RelationDTO dto)
        {
            // we need to reduce the relations to those whose related CIs are actually present in the mapper, to ensure that only relations of mapped cis are fetched
            var fromCIID = mapper.GetCIID(new ExternalIDGuid(dto.FromCIID));
            var toCIID = mapper.GetCIID(new ExternalIDGuid(dto.ToCIID));

            if (fromCIID.HasValue && toCIID.HasValue)
            {
                return new Relation(dto.ID, fromCIID.Value, toCIID.Value, dto.PredicateID, staticChangesetID, dto.Mask);
            }
            else return null;
        }

        public async IAsyncEnumerable<CIAttribute> GetAttributes(ICIIDSelection selection, TimeThreshold atTime, IAttributeSelection attributeSelection)
        {
            if (!atTime.IsLatest) yield break; // TODO: implement historic information

            var ciids = selection.GetCIIDs(() => mapper.GetAllCIIDs()).ToHashSet();

            // we need to map the ciids to external ciids, even if they are the same, to ensure that only mapped cis are fetched
            var IDPairs = mapper.GetIDPairs(ciids);

            if (IDPairs.IsEmpty()) yield break; // no ci maps, bail early

            var remoteLayers = await client.GetLayersByNameAsync(remoteLayerNames, ClientVersion);
            var remoteLayerIDs = remoteLayers.Select(rl => rl.ID).ToArray();

            var externalIDs = IDPairs.Select(p => p.externalID.ID);
            var time = (atTime.IsLatest) ? (DateTimeOffset?)null : atTime.Time;
            var attributesDTO = (attributeSelection is RegexAttributeSelection ras) ? // TODO: support other attribute selections
                await client.FindMergedAttributesByNameAsync(ras.RegexStr, ciids, remoteLayerIDs, time, ClientVersion) : // TODO: does not exist anymore in later versions
                await client.GetMergedAttributesAsync(externalIDs, remoteLayerIDs, time, ClientVersion);
            foreach (var a in AttributeDTO2Regular(attributesDTO))
                yield return a;
        }

        public Task<CIAttribute?> GetFullBinaryAttribute(string name, Guid ciid, TimeThreshold atTime)
        {
            return Task.FromResult<CIAttribute?>(null); // TODO: not implemented
        }

        public async IAsyncEnumerable<Relation> GetRelations(IRelationSelection rl, TimeThreshold atTime)
        {
            if (!atTime.IsLatest) yield break; // TODO: implement historic information

            throw new NotImplementedException(); // TODO

            //var remoteLayers = await client.GetLayersByNameAsync(remoteLayerNames, ClientVersion);
            //var remoteLayerIDs = remoteLayers.Select(rl => rl.ID).ToArray();
            //var time = (atTime.IsLatest) ? (DateTimeOffset?)null : atTime.Time;

            //var relationsDTO = rl switch
            //{
            //    RelationSelectionFrom f => await client.GetMergedRelationsOutgoingFromCIAsync(f.fromCIIDs, remoteLayerIDs, time, ClientVersion),
            //    RelationSelectionTo f => throw new NotImplementedException(), // TODO
            //    RelationSelectionWithPredicate p => await client.GetMergedRelationsWithPredicateAsync(p.predicateID, remoteLayerIDs, time, ClientVersion),
            //    RelationSelectionAll a => await client.GetAllMergedRelationsAsync(remoteLayerIDs, time, ClientVersion),
            //    _ => throw new NotImplementedException(),// must not be
            //};

            //// we need to reduce the relations to those whose related CIs are actually present in the mapper, to ensure that only relations of mapped cis are fetched
            //foreach (var r in RelationDTO2Regular(relationsDTO))
            //    yield return r;
        }

        public async Task<Relation?> GetRelation(Guid fromCIID, Guid toCIID, string predicateID, TimeThreshold atTime)
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
