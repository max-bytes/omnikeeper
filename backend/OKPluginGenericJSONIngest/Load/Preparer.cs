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
        private CICandidateAttributeData.Fragment? GenericAttribute2Fragment(GenericInboundAttribute a)
        {
            if (a.value == null)
                return null;
            var value = AttributeValueBuilder.BuildFromTypeAndObject(a.type, a.value);
            return new CICandidateAttributeData.Fragment(a.name, value);
        }

        public IngestData GenericInboundData2IngestData(GenericInboundData data, LayerSet searchLayers, ILogger logger)
        {
            var tempCIIDMapping = new Dictionary<string, Guid>(); // maps tempIDs to temporary Guids

            var ciCandidates = new List<CICandidate>(data.cis.Count());
            foreach(var ci in data.cis)
            {
                var fragments = ci.attributes.Select(a =>
                {
                    try
                    {
                        return GenericAttribute2Fragment(a);
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"Could not build attribute {a.name} with value {a.value} and type {a.type} for ci {ci.tempID}", e);
                    }
                });

                // TODO: make configurable
                var gracefullNullHandling = true;
                if (gracefullNullHandling)
                    fragments = fragments.WhereNotNull();

                var attributes = new CICandidateAttributeData(fragments!);

                ICIIdentificationMethod idMethod = BuildCIIDMethod(ci.idMethod, attributes, searchLayers, ci.tempID, tempCIIDMapping);

                var tempGuid = Guid.NewGuid();
                tempCIIDMapping.TryAdd(ci.tempID, tempGuid);

                ciCandidates.Add(new CICandidate(tempGuid, idMethod, attributes));
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
                    CIIdentificationMethodByTemporaryCIID.Build(tempFromGuid),
                    CIIdentificationMethodByTemporaryCIID.Build(tempToGuid), r.predicate);
            }).Where(d => d != null).ToList(); // NOTE: we force linq evaluation here
            return new IngestData(ciCandidates, relationCandidates!);
        }

        private ICIIdentificationMethod BuildCIIDMethod(IInboundIDMethod idMethod, CICandidateAttributeData attributes, LayerSet searchLayers, string tempCIID, Dictionary<string, Guid> tempCIIDMapping)
        {
            return idMethod switch
            {
                InboundIDMethodByData d => CIIdentificationMethodByData.BuildFromAttributes(d.attributes, attributes, searchLayers),
                InboundIDMethodByTemporaryCIID t => CIIdentificationMethodByTemporaryCIID.Build(tempCIIDMapping.GetValueOrDefault(t.tempID)),
                InboundIDMethodByByFirstOf f => CIIdentificationMethodByFirstOf.Build(f.inner.Select(i => BuildCIIDMethod(i, attributes, searchLayers, tempCIID, tempCIIDMapping)).ToArray()),
                _ => throw new Exception($"unknown idMethod \"{idMethod.GetType()}\" for ci candidate \"{tempCIID}\" encountered")
            };
        }
    }
}
