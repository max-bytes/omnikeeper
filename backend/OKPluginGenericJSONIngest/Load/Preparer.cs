using Microsoft.Extensions.Logging;
using Omnikeeper.Base.AttributeValues;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OKPluginGenericJSONIngest.Load
{
    public class Preparer
    {
        private CICandidateAttributeData.Fragment GenericAttribute2Fragment(GenericInboundAttribute a)
        {
            var value = AttributeValueBuilder.BuildFromTypeAndObject(a.type, a.value);
            return new CICandidateAttributeData.Fragment(a.name, value);
        }

        public IngestData GenericInboundData2IngestData(GenericInboundData data, LayerSet searchLayers)
        {
            var tempCIIDMapping = new Dictionary<string, Guid>(); // maps tempIDs to temporary Guids

            // TODO: rewrite to for loop instead of select to explicitly model sideeffect of writing tempCIIDMapping
            var ciCandidates = data.cis.Select(ci =>
            {
                var fragments = ci.attributes.Select(a => GenericAttribute2Fragment(a));
                var attributes = new CICandidateAttributeData(fragments);

                // id method, TODO: make proper, not via fields that have different meanings depending on .method
                ICIIdentificationMethod idMethod = ci.idMethod.method switch
                {
                    "byData" => CIIdentificationMethodByData.BuildFromAttributes(ci.idMethod.attributes, attributes, searchLayers),
                    "byTempID" => CIIdentificationMethodByTemporaryCIID.Build(tempCIIDMapping.GetValueOrDefault(ci.idMethod.tempID)),
                    _ => throw new Exception($"Invalid idMethod \"{ci.idMethod.method}\" for ci candidate \"{ci.tempID}\" encountered")
                };

                var tempGuid = Guid.NewGuid();
                tempCIIDMapping.TryAdd(ci.tempID, tempGuid);

                return new CICandidate(tempGuid, idMethod, attributes);
            }).ToList();

            var relationCandidates = data.relations.Select(r =>
            {
                if (!tempCIIDMapping.TryGetValue(r.from, out var tempFromGuid))
                    throw new Exception($"From-ci \"{r.from}\" of relation could not be resolved");
                if (!tempCIIDMapping.TryGetValue(r.to, out var tempToGuid))
                    throw new Exception($"To-ci \"{r.to}\" of relation could not be resolved");
                return new RelationCandidate(
                    CIIdentificationMethodByTemporaryCIID.Build(tempFromGuid),
                    CIIdentificationMethodByTemporaryCIID.Build(tempToGuid), r.predicate);
            }).ToList(); // NOTE: we force linq evaluation here
            return new IngestData(ciCandidates, relationCandidates);
        }

        //public async Task Load(IngestData ingestData, Layer writeLayer, AuthenticatedUser user, IngestDataService ingestDataService)
        //{
        //    var (numIngestedCIs, numIngestedRelations) = await ingestDataService.Ingest(ingestData, writeLayer, user);
        //    // TODO: result
        //}
    }
}
