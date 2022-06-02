using Microsoft.Extensions.Logging;
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
        private readonly IModelContextBuilder modelContextBuilder;
        private readonly ILogger<IngestDataService> logger;

        private IAttributeModel AttributeModel { get; }
        private ICIModel CIModel { get; }
        private IChangesetModel ChangesetModel { get; }
        private IRelationModel RelationModel { get; }

        public IngestDataService(IAttributeModel attributeModel, ICIModel ciModel, IChangesetModel changesetModel, IRelationModel relationModel,
            CIMappingService ciMappingService, IModelContextBuilder modelContextBuilder, ILogger<IngestDataService> logger)
        {
            AttributeModel = attributeModel;
            CIModel = ciModel;
            ChangesetModel = changesetModel;
            RelationModel = relationModel;
            this.ciMappingService = ciMappingService;
            this.modelContextBuilder = modelContextBuilder;
            this.logger = logger;
        }

        // TODO: add ci-based authorization
        public async Task<(int numAffectedAttributes, int numAffectedRelations)> Ingest(IngestData data, Layer writeLayer, AuthenticatedUser user)
        {
            using var trans = modelContextBuilder.BuildDeferred();
            var timeThreshold = TimeThreshold.BuildLatest();
            var changesetProxy = new ChangesetProxy(user.InDatabase, timeThreshold, ChangesetModel);

            var ciMappingContext = new CIMappingService.CIMappingContext(AttributeModel, RelationModel, TimeThreshold.BuildLatest());
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
                                logger.LogWarning($"Dropping candidate CI with temp-ID {cic.TempID}, as there is already a candidate CI with that tempID");
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
                    var foundCIIDs = await ciMappingService.TryToMatch(cic.IdentificationMethod, ciMappingContext, trans, logger);

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
                                logger.LogWarning($"Dropping candidate CI with temp-ID {cic.TempID}, as candidate CI with temp-ID {ciCandidatesToInsert[foundCIIDs[0]].tempID} already targets the CI with ID {foundCIIDs[0]}");
                                break;
                            case SameTargetCIHandling.Evade:
                            case SameTargetCIHandling.EvadeAndWarn:
                                {
                                    // remove all target CIIDs that are already targeted
                                    for (int i = foundCIIDs.Count - 1; i >= 0; i--)
                                    {
                                        if (ciCandidatesToInsert.ContainsKey(foundCIIDs[i]))
                                        {
                                            foundCIIDs.RemoveAt(i);
                                            if (cic.SameTargetCIHandling == SameTargetCIHandling.EvadeAndWarn)
                                            {
                                                logger.LogWarning($"Candidate CI with temp-ID {cic.TempID}: evading to other CI, because candidate CI with temp-ID {ciCandidatesToInsert[foundCIIDs[i]].tempID} already targets the CI with ID {foundCIIDs[i]}");
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

                    if (foundCIIDs.IsEmpty())
                    {
                        // CI is new, create a ciid for it (we'll later batch create all CIs)
                        var newCIID = CIModel.CreateCIID();
                        foundCIIDs = new Guid[] { newCIID }; // use a totally new CIID, do NOT use the temporary CIID of the ciCandidate
                        cisToCreate.Add(newCIID);
                    }

                    // TODO: add handling option for new CIs: if its allowed or not

                    if (dropCandidateCIBecauseOfSameTargetCI)
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
                            logger.LogWarning($"Multiple CIs match for candidate with temp-ID {cic.TempID}: {string.Join(",", foundCIIDs)}, taking first one");
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

            var numAffectedAttributes = await AttributeModel.BulkReplaceAttributes(bulkAttributeData, changesetProxy, new DataOriginV1(DataOriginType.InboundIngest), trans, maskHandling, otherLayersValueHandling);

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
                    // TODO: different handling options
                    logger.LogWarning($"Duplicate relation candidate detected: (fromCIID: {fromCIID}, toCIID: {toCIID}, predicateID: {cic.PredicateID}), dropping");
                    continue;
                }

                relationFragments.Add(new BulkRelationDataLayerScope.Fragment(fromCIID, toCIID, cic.PredicateID, false));
                usedRelations.Add((fromCIID, toCIID, cic.PredicateID));
            }
            var bulkRelationData = new BulkRelationDataLayerScope(writeLayer.ID, relationFragments.ToArray());
            var numAffectedRelations = await RelationModel.BulkReplaceRelations(bulkRelationData, changesetProxy, new DataOriginV1(DataOriginType.InboundIngest), trans, maskHandling, otherLayersValueHandling);

            trans.Commit();

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

    public class CICandidate
    {
        public Guid TempCIID { get; set; }
        public string TempID { get; set; }
        public ICIIdentificationMethod IdentificationMethod { get; private set; }
        public SameTempIDHandling SameTempIDHandling { get; private set; }
        public SameTargetCIHandling SameTargetCIHandling { get; private set; }
        public CICandidateAttributeData Attributes { get; private set; }

        public CICandidate(Guid tempCIID, string tempID, ICIIdentificationMethod identificationMethod, SameTempIDHandling sameTempIDHandling, SameTargetCIHandling sameTargetCIHandling, CICandidateAttributeData attributes)
        {
            TempCIID = tempCIID;
            TempID = tempID;
            IdentificationMethod = identificationMethod;
            SameTempIDHandling = sameTempIDHandling;
            SameTargetCIHandling = sameTargetCIHandling;
            Attributes = attributes;
        }
    }

    public class RelationCandidate
    {
        public CIIdentificationMethodByTempCIID IdentificationMethodFromCI { get; private set; }
        public CIIdentificationMethodByTempCIID IdentificationMethodToCI { get; private set; }
        public string PredicateID { get; private set; }

        public RelationCandidate(CIIdentificationMethodByTempCIID identificationMethodFromCI, CIIdentificationMethodByTempCIID identificationMethodToCI, string predicateID)
        {
            IdentificationMethodFromCI = identificationMethodFromCI;
            IdentificationMethodToCI = identificationMethodToCI;
            PredicateID = predicateID;
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
