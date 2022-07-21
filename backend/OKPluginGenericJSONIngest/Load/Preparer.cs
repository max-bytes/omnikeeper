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

            var ciCandidates = new List<CICandidate>(data.CIs.Count());
            foreach (var ci in data.CIs)
            {
                try
                {
                    var a = ci.Attributes;
                    // TODO: make configurable
                    var gracefullNullHandling = true;
                    if (gracefullNullHandling)
                        a = a.Where(f => f.Value != null);

                    var fragments = a.Select(a =>
                    {
                        return new CICandidateAttributeData.Fragment(a.Name, a.Value);
                    });

                    var attributes = new CICandidateAttributeData(fragments!);

                    ICIIdentificationMethod idMethod = BuildCIIDMethod(ci.IDMethod, attributes, searchLayers, ci.TempID, tempCIIDMapping, issueAccumulator);

                    var tempGuid = Guid.NewGuid();
                    tempCIIDMapping.TryAdd(ci.TempID, tempGuid);

                    ciCandidates.Add(new CICandidate(tempGuid, ci.TempID, idMethod, ci.SameTempIDHandling, ci.SameTargetCIHandling, ci.NoFoundTargetCIHandling, attributes));
                }
                catch (Exception e)
                {
                    issueAccumulator.TryAdd("prepare_ci", ci.TempID.ToString(), $"Could not create CI-candidate with temp ID {ci.TempID}: {e.Message}");
                }
            }

            List<RelationCandidate> relationCandidates;
            if (data.Relations != null)
            {
                relationCandidates = data.Relations.Select(r =>
                {
                    // TODO: make configurable
                    var gracefulFromErrorHandling = true;
                    var gracefulToErrorHandling = true;

                    if (!tempCIIDMapping.TryGetValue(r.From, out var tempFromGuid))
                    {
                        if (gracefulFromErrorHandling)
                        {
                            issueAccumulator.TryAdd("prepare_relation", $"{r.From}_{r.To}_{r.Predicate}", $"From-ci \"{r.From}\" of relation could not be resolved");
                            return null;
                        }
                        else
                            throw new Exception($"From-ci \"{r.From}\" of relation could not be resolved");
                    }
                    if (!tempCIIDMapping.TryGetValue(r.To, out var tempToGuid))
                    {
                        if (gracefulToErrorHandling)
                        {
                            issueAccumulator.TryAdd("prepare_relation", $"{r.From}_{r.To}_{r.Predicate}", $"To-ci \"{r.To}\" of relation could not be resolved");
                            return null;
                        }
                        else
                            throw new Exception($"To-ci \"{r.To}\" of relation could not be resolved");
                    }
                    return new RelationCandidate(
                        CIIdentificationMethodByTempCIID.Build(tempFromGuid),
                        CIIdentificationMethodByTempCIID.Build(tempToGuid), r.Predicate);
                }).Where(d => d != null).Cast<RelationCandidate>().ToList(); // NOTE: we force linq evaluation here
            } else
            {
                relationCandidates = new List<RelationCandidate>();
            }
            return new IngestData(ciCandidates, relationCandidates);
        }

        private ICIIdentificationMethod BuildCIIDMethod(IInboundIDMethod idMethod, CICandidateAttributeData attributes, LayerSet searchLayers, string tempID, Dictionary<string, Guid> tempCIIDMapping, IIssueAccumulator issueAccumulator)
        {
            ICIIdentificationMethod attributeF(InboundIDMethodByAttribute a)
            {
                if (a.Attribute.Value == null)
                {
                    issueAccumulator.TryAdd("prepare_id_method_by_attribute", tempID, $"Could not create fragment from generic attribute for idMethod CIIdentificationMethodByFragment using attribute name {a.Attribute.Name} on candidate CI with temp ID {tempID}");
                    return CIIdentificationMethodNoop.Build();
                }
                var fragment = new CICandidateAttributeData.Fragment(a.Attribute.Name, a.Attribute.Value);
                return CIIdentificationMethodByFragment.Build(fragment, a.Modifiers.CaseInsensitive, searchLayers);
            }

            // TODO: error handling is inconsistent: sometimes a warning is generated, sometimes an exception is thrown, consider reworking
            return idMethod switch
            {
                InboundIDMethodByData d => CIIdentificationMethodByData.BuildFromAttributes(d.Attributes, attributes, searchLayers),
                InboundIDMethodByAttribute a => attributeF(a),
                InboundIDMethodByRelatedTempID rt => CIIdentificationMethodByRelatedTempCIID.Build(tempCIIDMapping.GetValueOrDefault(rt.TempID), rt.OutgoingRelation, rt.PredicateID, searchLayers),
                InboundIDMethodByTemporaryCIID t => CIIdentificationMethodByTempCIID.Build(tempCIIDMapping.GetValueOrDefault(t.TempID)),
                InboundIDMethodByByUnion f => CIIdentificationMethodByUnion.Build(f.Inner.Select(i => BuildCIIDMethod(i, attributes, searchLayers, tempID, tempCIIDMapping, issueAccumulator)).ToArray()),
                InboundIDMethodByIntersect a => CIIdentificationMethodByIntersect.Build(a.Inner.Select(i => BuildCIIDMethod(i, attributes, searchLayers, tempID, tempCIIDMapping, issueAccumulator)).ToArray()),
                _ => throw new Exception($"unknown idMethod \"{idMethod.GetType()}\" for ci candidate \"{tempID}\" encountered")
            };
        }
    }
}
