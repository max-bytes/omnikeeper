using Microsoft.Extensions.Logging;
using Omnikeeper.Base.AttributeValues;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OKPluginGenericJSONIngest.Load
{
    public class Preparer
    {
        public IngestData GenericInboundData2IngestData(GenericInboundData data, LayerSet searchLayers, ILogger logger)
        {
            var tempCIIDMapping = new Dictionary<string, Guid>(); // maps tempIDs to temporary Guids

            var ciCandidates = new List<CICandidate>(data.cis.Count());
            foreach (var ci in data.cis)
            {
                try
                {
                    var a = ci.attributes;
                    // TODO: make configurable
                    var gracefullNullHandling = true;
                    if (gracefullNullHandling)
                        a = a.Where(f => f.value != null);

                    var fragments = a.Select(a =>
                    {
                        return new CICandidateAttributeData.Fragment(a.name, a.value);
                    });

                    var attributes = new CICandidateAttributeData(fragments!);

                    ICIIdentificationMethod idMethod = BuildCIIDMethod(ci.idMethod, attributes, searchLayers, ci.tempID, tempCIIDMapping, logger);

                    var tempGuid = Guid.NewGuid();
                    tempCIIDMapping.TryAdd(ci.tempID, tempGuid);

                    ciCandidates.Add(new CICandidate(tempGuid, ci.tempID, idMethod, ci.sameTargetCIHandling, attributes));
                }
                catch (Exception e)
                {
                    logger.LogError(e, $"Could not create CI-candidate with temp ID {ci.tempID}");
                }
            }

            var relationCandidates = data.relations.Select(r =>
            {
                // TODO: make configurable
                var gracefulFromErrorHandling = true;
                var gracefulToErrorHandling = true;

                if (!tempCIIDMapping.TryGetValue(r.from, out var tempFromGuid))
                {
                    if (gracefulFromErrorHandling)
                    {
                        logger.LogWarning($"From-ci \"{r.from}\" of relation could not be resolved");
                        return null;
                    }
                    else
                        throw new Exception($"From-ci \"{r.from}\" of relation could not be resolved");
                }
                if (!tempCIIDMapping.TryGetValue(r.to, out var tempToGuid))
                {
                    if (gracefulToErrorHandling)
                    {
                        logger.LogWarning($"To-ci \"{r.to}\" of relation could not be resolved");
                        return null;
                    }
                    else
                        throw new Exception($"To-ci \"{r.to}\" of relation could not be resolved");
                }
                return new RelationCandidate(
                    CIIdentificationMethodByTempCIID.Build(tempFromGuid),
                    CIIdentificationMethodByTempCIID.Build(tempToGuid), r.predicate);
            }).Where(d => d != null).ToList(); // NOTE: we force linq evaluation here
            return new IngestData(ciCandidates, relationCandidates!);
        }

        private ICIIdentificationMethod BuildCIIDMethod(IInboundIDMethod idMethod, CICandidateAttributeData attributes, LayerSet searchLayers, string tempCIID, Dictionary<string, Guid> tempCIIDMapping, ILogger logger)
        {
            ICIIdentificationMethod attributeF(InboundIDMethodByAttribute a)
            {
                if (a.attribute.value == null)
                {
                    logger.LogWarning($"Could not create fragment from generic attribute for idMethod CIIdentificationMethodByFragment using attribute name {a.attribute.name}");
                    return CIIdentificationMethodNoop.Build();
                }
                var fragment = new CICandidateAttributeData.Fragment(a.attribute.name, a.attribute.value);
                return CIIdentificationMethodByFragment.Build(fragment, a.modifiers.caseInsensitive, searchLayers);
            }

            // TODO: error handling is inconsistent: sometimes a warning is generated, sometimes an exception is thrown, consider reworking
            return idMethod switch
            {
                InboundIDMethodByData d => CIIdentificationMethodByData.BuildFromAttributes(d.attributes, attributes, searchLayers),
                InboundIDMethodByAttribute a => attributeF(a),
                InboundIDMethodByRelatedTempID rt => CIIdentificationMethodByRelatedTempCIID.Build(tempCIIDMapping.GetValueOrDefault(rt.tempID), rt.outgoingRelation, rt.predicateID, searchLayers),
                InboundIDMethodByTemporaryCIID t => CIIdentificationMethodByTempCIID.Build(tempCIIDMapping.GetValueOrDefault(t.tempID)),
                InboundIDMethodByByUnion f => CIIdentificationMethodByUnion.Build(f.inner.Select(i => BuildCIIDMethod(i, attributes, searchLayers, tempCIID, tempCIIDMapping, logger)).ToArray()),
                InboundIDMethodByIntersect a => CIIdentificationMethodByIntersect.Build(a.inner.Select(i => BuildCIIDMethod(i, attributes, searchLayers, tempCIID, tempCIIDMapping, logger)).ToArray()),
                _ => throw new Exception($"unknown idMethod \"{idMethod.GetType()}\" for ci candidate \"{tempCIID}\" encountered")
            };
        }
    }
}
