using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OKPluginGenericJSONIngest.Load
{
    public class Preparer
    {
        public IngestData GenericInboundData2IngestData(GenericInboundData data, LayerSet searchLayers, IIssueAccumulator issueAccumulator)
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

                    ICIIdentificationMethod idMethod = BuildCIIDMethod(ci.idMethod, attributes, searchLayers, ci.tempID, tempCIIDMapping, issueAccumulator);

                    var tempGuid = Guid.NewGuid();
                    tempCIIDMapping.TryAdd(ci.tempID, tempGuid);

                    ciCandidates.Add(new CICandidate(tempGuid, ci.tempID, idMethod, ci.sameTempIDHandling, ci.sameTargetCIHandling, ci.noFoundTargetCIHandling, attributes));
                }
                catch (Exception e)
                {
                    issueAccumulator.TryAdd("prepare_ci", ci.tempID.ToString(), $"Could not create CI-candidate with temp ID {ci.tempID}: {e.Message}");
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
                        issueAccumulator.TryAdd("prepare_relation", $"{r.from}_{r.to}_{r.predicate}", $"From-ci \"{r.from}\" of relation could not be resolved");
                        return null;
                    }
                    else
                        throw new Exception($"From-ci \"{r.from}\" of relation could not be resolved");
                }
                if (!tempCIIDMapping.TryGetValue(r.to, out var tempToGuid))
                {
                    if (gracefulToErrorHandling)
                    {
                        issueAccumulator.TryAdd("prepare_relation", $"{r.from}_{r.to}_{r.predicate}", $"To-ci \"{r.to}\" of relation could not be resolved");
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

        private ICIIdentificationMethod BuildCIIDMethod(IInboundIDMethod idMethod, CICandidateAttributeData attributes, LayerSet searchLayers, string tempID, Dictionary<string, Guid> tempCIIDMapping, IIssueAccumulator issueAccumulator)
        {
            ICIIdentificationMethod attributeF(InboundIDMethodByAttribute a)
            {
                if (a.attribute.value == null)
                {
                    issueAccumulator.TryAdd("prepare_id_method_by_attribute", tempID, $"Could not create fragment from generic attribute for idMethod CIIdentificationMethodByFragment using attribute name {a.attribute.name} on candidate CI with temp ID {tempID}");
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
                InboundIDMethodByByUnion f => CIIdentificationMethodByUnion.Build(f.inner.Select(i => BuildCIIDMethod(i, attributes, searchLayers, tempID, tempCIIDMapping, issueAccumulator)).ToArray()),
                InboundIDMethodByIntersect a => CIIdentificationMethodByIntersect.Build(a.inner.Select(i => BuildCIIDMethod(i, attributes, searchLayers, tempID, tempCIIDMapping, issueAccumulator)).ToArray()),
                _ => throw new Exception($"unknown idMethod \"{idMethod.GetType()}\" for ci candidate \"{tempID}\" encountered")
            };
        }
    }
}
