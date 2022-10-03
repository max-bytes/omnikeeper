using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Service
{
    public class IngestDataService
    {
        private readonly CIMappingService ciMappingService;

        private IAttributeModel AttributeModel { get; }
        private ICIModel CIModel { get; }
        private IRelationModel RelationModel { get; }

        public IngestDataService(IAttributeModel attributeModel, ICIModel ciModel, IRelationModel relationModel, CIMappingService ciMappingService)
        {
            AttributeModel = attributeModel;
            CIModel = ciModel;
            RelationModel = relationModel;
            this.ciMappingService = ciMappingService;
        }

        // TODO: add ci-based authorization
        public async Task<(int numAffectedAttributes, int numAffectedRelations)> Ingest(IngestData data, Layer writeLayer, ChangesetProxy changesetProxy, 
            IIssueAccumulator issueAccumulator, IModelContext trans)
        {
            var ciMappingContext = new CIMappingService.CIMappingContext(AttributeModel, RelationModel, changesetProxy.TimeThreshold);
            var cisToCreate = new List<Guid>();
            var ciCandidatesToInsert = new Dictionary<Guid, (CICandidateAttributeData attributes, Guid targetCIID, string tempID)>();
            var droppedCandidateCIs = new HashSet<Guid>();
            var processedTempIDs = new HashSet<string>();

            foreach (var cic in data.CICandidates)
            {
                var attributes = cic.Attributes;
                var ciCandidateCIID = cic.TempCIID;
                try
                {
                    // detect if another candidate CI with the same tempID was already prepared and act according to SameTempIDHandling
                    var dropCandidateCIBecauseOfSameTempID = false;
                    if (processedTempIDs.Contains(cic.TempID))
                    {
                        switch (cic.SameTempIDHandling)
                        {
                            case SameTempIDHandling.Drop:
                                dropCandidateCIBecauseOfSameTempID = true;
                                break;
                            case SameTempIDHandling.DropAndWarn:
                                dropCandidateCIBecauseOfSameTempID = true;
                                issueAccumulator.TryAdd("same_temp_id", cic.TempID, $"Dropping candidate CI with temp-ID {cic.TempID}, as there is already a candidate CI with that tempID");
                                break;
                        }
                    } 
                    else
                    {
                        processedTempIDs.Add(cic.TempID);
                    }
                    if (dropCandidateCIBecauseOfSameTempID)
                    {
                        droppedCandidateCIs.Add(ciCandidateCIID);
                        continue;
                    }

                    // find out if it's a new CI or an existing one
                    var foundCIIDs = await ciMappingService.TryToMatch(cic.IdentificationMethod, ciMappingContext, trans);

                    var dropCandidateCIBecauseOfSameTargetCI = false;
                    Guid? mergeIntoOtherTargetCIID = null;
                    if (!foundCIIDs.IsEmpty() && ciCandidatesToInsert.ContainsKey(foundCIIDs[0]))
                    {
                        // the mapping process cannot map multiple candidates to the same targetCI, it would result in an ingest error;
                        // so we must do something... what we do is governed by SameTargetCIHandling
                        switch (cic.SameTargetCIHandling)
                        {
                            case SameTargetCIHandling.Error:
                                throw new Exception($"Candidate CI with temp-ID {ciCandidatesToInsert[foundCIIDs[0]].tempID} already targets the CI with ID {foundCIIDs[0]}");
                            case SameTargetCIHandling.Drop:
                                dropCandidateCIBecauseOfSameTargetCI = true;
                                break;
                            case SameTargetCIHandling.DropAndWarn:
                                dropCandidateCIBecauseOfSameTargetCI = true;
                                issueAccumulator.TryAdd("same_target_ci", cic.TempID, $"Dropping candidate CI with temp-ID {cic.TempID}, as candidate CI with temp-ID {ciCandidatesToInsert[foundCIIDs[0]].tempID} already targets the CI with ID {foundCIIDs[0]}");
                                break;
                            case SameTargetCIHandling.Evade:
                            case SameTargetCIHandling.EvadeAndWarn:
                                {
                                    // remove all target CIIDs that are already targeted
                                    for (int i = foundCIIDs.Count - 1; i >= 0; i--)
                                    {
                                        if (ciCandidatesToInsert.ContainsKey(foundCIIDs[i]))
                                        {
                                            var toRemove = foundCIIDs[i];
                                            foundCIIDs.RemoveAt(i);
                                            if (cic.SameTargetCIHandling == SameTargetCIHandling.EvadeAndWarn)
                                            {
                                                issueAccumulator.TryAdd("same_target_ci", cic.TempID, $"Candidate CI with temp-ID {cic.TempID}: evading to other CI, because candidate CI with temp-ID {ciCandidatesToInsert[toRemove].tempID} already targets the CI with ID {toRemove}");
                                            }
                                        }
                                    }
                                }
                                break;
                            case SameTargetCIHandling.Merge:
                                mergeIntoOtherTargetCIID = foundCIIDs[0];
                                break;
                        }
                    }

                    var dropCandidateCIBecauseOfNoFoundTargetCI = false;
                    if (foundCIIDs.IsEmpty())
                    {
                        switch (cic.NoFoundTargetCIHandling)
                        {
                            case NoFoundTargetCIHandling.CreateNew:
                            case NoFoundTargetCIHandling.CreateNewAndWarn:
                                {
                                    // CI is new, create a ciid for it (we'll later batch create all CIs)
                                    var newCIID = CIModel.CreateCIID();
                                    foundCIIDs = new Guid[] { newCIID }; // use a totally new CIID, do NOT use the temporary CIID of the ciCandidate
                                    cisToCreate.Add(newCIID);

                                    if (cic.NoFoundTargetCIHandling == NoFoundTargetCIHandling.CreateNewAndWarn)
                                    {
                                        issueAccumulator.TryAdd("no_found_target_ci", cic.TempID, $"Candidate CI with temp-ID {cic.TempID}: found no matching target CI, creating new CI", newCIID);
                                    }
                                }
                                break;
                            case NoFoundTargetCIHandling.Drop:
                            case NoFoundTargetCIHandling.DropAndWarn:
                                dropCandidateCIBecauseOfNoFoundTargetCI = true;
                                if (cic.NoFoundTargetCIHandling == NoFoundTargetCIHandling.DropAndWarn)
                                {
                                    issueAccumulator.TryAdd("no_found_target_ci", cic.TempID, $"Candidate CI with temp-ID {cic.TempID}: found no matching target CI, dropping");
                                }
                                break;
                        }
                    }

                    if (dropCandidateCIBecauseOfNoFoundTargetCI)
                    { // drop candidate completely
                        droppedCandidateCIs.Add(ciCandidateCIID);
                    } else if (dropCandidateCIBecauseOfSameTargetCI)
                    { // drop candidate completely
                        droppedCandidateCIs.Add(ciCandidateCIID);
                    } else if (mergeIntoOtherTargetCIID.HasValue)
                    { // merged candidate into another, already existing candidate

                        var otherTargetCIID = mergeIntoOtherTargetCIID.Value;

                        // merge attributes and replace CI candidate to be inserted
                        var otherCICandidate = ciCandidatesToInsert[otherTargetCIID];
                        var mergedAttributes = otherCICandidate.attributes.Merge(cic.Attributes);
                        ciCandidatesToInsert[otherCICandidate.targetCIID] = (mergedAttributes, otherCICandidate.targetCIID, otherCICandidate.tempID);

                        // add a Temp2FinalCIID mapping that makes the merged candidate CI point to the base candidate CI's target CIID
                        ciMappingContext.AddTemp2FinallCIIDMapping(ciCandidateCIID, otherTargetCIID);
                    }
                    else 
                    { // regular candidate
                        // determine final CIID from found candidate CIIDs
                        Guid finalCIID;
                        if (foundCIIDs.Count() == 1)
                            finalCIID = foundCIIDs.First();
                        else
                        {
                            // NOTE: how to deal with ambiguities? In other words: more than one CI fit, where to put the data?
                            // ciMappingService.TryToMatch() returns an already ordered list (by varying criteria, or - if nothing else - by CIID)
                            finalCIID = foundCIIDs.First();
                            issueAccumulator.TryAdd("multiple_target_cis", cic.TempID, $"Multiple CIs match for candidate with temp-ID {cic.TempID}: {string.Join(",", foundCIIDs)}, taking first one", foundCIIDs.ToArray());
                        }

                        // add to mapping context
                        ciMappingContext.AddTemp2FinallCIIDMapping(ciCandidateCIID, finalCIID);

                        ciCandidatesToInsert.Add(finalCIID, (cic.Attributes, finalCIID, cic.TempID));
                    }
                }
                catch (Exception e)
                {
                    throw new Exception($"Error mapping CI-candidate with temp-ID {cic.TempID}: {e.Message}", e);
                }
            }

            // TODO: mask handling
            var maskHandling = MaskHandlingForRemovalApplyNoMask.Instance;
            // TODO: other-layers-value handling
            var otherLayersValueHandling = OtherLayersValueHandlingForceWrite.Instance;

            // batch process CI creation
            if (!cisToCreate.IsEmpty())
                await CIModel.BulkCreateCIs(cisToCreate, trans);

            var bulkAttributeData = new BulkCIAttributeDataLayerScope(writeLayer.ID, ciCandidatesToInsert.Values.SelectMany(cic =>
                cic.attributes.Fragments.Select(f => new BulkCIAttributeDataLayerScope.Fragment(f.Name, f.Value, cic.targetCIID))
            ));

            var numAffectedAttributes = await AttributeModel.BulkReplaceAttributes(bulkAttributeData, changesetProxy, trans, maskHandling, otherLayersValueHandling);

            var relationFragments = new List<BulkRelationDataLayerScope.Fragment>();
            var usedRelations = new HashSet<(Guid fromCIID, Guid toCIID, string predicateID)>();
            foreach (var cic in data.RelationCandidates)
            {
                // TODO: make it work with other usecases, such as where the final CIID is known and/or the relevant CIs are already present in omnikeeper
                // find CIIDs
                var tempFromCIID = cic.IdentificationMethodFromCI.CIID;
                var tempToCIID = cic.IdentificationMethodToCI.CIID;
                if (droppedCandidateCIs.Contains(tempFromCIID))
                    continue; // candidate CI was dropped, drop relation too
                if (droppedCandidateCIs.Contains(tempToCIID))
                    continue; // candidate CI was dropped, drop relation too

                if (!ciMappingContext.TryGetMappedTemp2FinalCIID(tempFromCIID, out Guid fromCIID))
                    throw new Exception($"Could not find temporary CIID {tempFromCIID}, tried using it as the \"from\" of a relation");
                if (!ciMappingContext.TryGetMappedTemp2FinalCIID(tempToCIID, out Guid toCIID))
                    throw new Exception($"Could not find temporary CIID {tempToCIID}, tried using it as the \"to\" of a relation");

                // duplicate handling
                if (usedRelations.Contains((fromCIID, toCIID, cic.PredicateID)))
                {
                    switch (cic.DuplicateRelationHandling)
                    {
                        case DuplicateRelationHandling.DropAndWarn:
                        case DuplicateRelationHandling.Drop:
                            if (cic.DuplicateRelationHandling == DuplicateRelationHandling.DropAndWarn)
                            {
                                issueAccumulator.TryAdd("duplicate_relation_candidate", $"{fromCIID}_{toCIID}_{cic.PredicateID}", $"Duplicate relation candidate detected: (fromCIID: {fromCIID}, toCIID: {toCIID}, predicateID: {cic.PredicateID}), dropping", fromCIID, toCIID);
                            }
                            break;
                        case DuplicateRelationHandling.Error:
                            throw new Exception($"Duplicate relation candidate detected: (fromCIID: {fromCIID}, toCIID: {toCIID}, predicateID: {cic.PredicateID})");
                    }
                    continue;
                }

                relationFragments.Add(new BulkRelationDataLayerScope.Fragment(fromCIID, toCIID, cic.PredicateID, false));
                usedRelations.Add((fromCIID, toCIID, cic.PredicateID));
            }
            var bulkRelationData = new BulkRelationDataLayerScope(writeLayer.ID, relationFragments.ToArray());
            var numAffectedRelations = await RelationModel.BulkReplaceRelations(bulkRelationData, changesetProxy, trans, maskHandling, otherLayersValueHandling);

            return (numAffectedAttributes, numAffectedRelations);
        }
    }

    public enum SameTargetCIHandling
    {
        Error, // default
        Drop,
        DropAndWarn,
        Evade,
        EvadeAndWarn,
        Merge
    }

    public enum SameTempIDHandling
    {
        DropAndWarn, // default
        Drop
    };

    public enum NoFoundTargetCIHandling
    {
        CreateNew, // default
        CreateNewAndWarn,
        DropAndWarn,
        Drop
    }

    public enum DuplicateRelationHandling
    {
        DropAndWarn, // default
        Drop,
        Error
    }

    public class CICandidate
    {
        public Guid TempCIID { get; set; }
        public string TempID { get; set; }
        public ICIIdentificationMethod IdentificationMethod { get; private set; }
        public SameTempIDHandling SameTempIDHandling { get; private set; }
        public SameTargetCIHandling SameTargetCIHandling { get; private set; }
        public NoFoundTargetCIHandling NoFoundTargetCIHandling { get; private set; }
        public CICandidateAttributeData Attributes { get; private set; }

        public CICandidate(Guid tempCIID, string tempID, ICIIdentificationMethod identificationMethod, SameTempIDHandling sameTempIDHandling, SameTargetCIHandling sameTargetCIHandling, NoFoundTargetCIHandling noFoundTargetCIHandling, CICandidateAttributeData attributes)
        {
            TempCIID = tempCIID;
            TempID = tempID;
            IdentificationMethod = identificationMethod;
            SameTempIDHandling = sameTempIDHandling;
            SameTargetCIHandling = sameTargetCIHandling;
            NoFoundTargetCIHandling = noFoundTargetCIHandling;
            Attributes = attributes;
        }
    }

    public class RelationCandidate
    {
        public CIIdentificationMethodByTempCIID IdentificationMethodFromCI { get; private set; }
        public CIIdentificationMethodByTempCIID IdentificationMethodToCI { get; private set; }
        public string PredicateID { get; private set; }
        public DuplicateRelationHandling DuplicateRelationHandling { get; private set; }

        public RelationCandidate(CIIdentificationMethodByTempCIID identificationMethodFromCI, CIIdentificationMethodByTempCIID identificationMethodToCI, string predicateID, DuplicateRelationHandling duplicateRelationHandling)
        {
            IdentificationMethodFromCI = identificationMethodFromCI;
            IdentificationMethodToCI = identificationMethodToCI;
            PredicateID = predicateID;
            DuplicateRelationHandling = duplicateRelationHandling;
        }
    }

    public class IngestData
    {
        public IEnumerable<CICandidate> CICandidates { get; private set; }
        public IEnumerable<RelationCandidate> RelationCandidates { get; private set; }

        public IngestData(IEnumerable<CICandidate> cis, IEnumerable<RelationCandidate> relationCandidates)
        {
            CICandidates = cis;
            RelationCandidates = relationCandidates;
        }
    }
}
