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
        public async Task<(int numIngestedCIs, int numIngestedRelations)> Ingest(IngestData data, Layer writeLayer, AuthenticatedUser user)
        {
            using var trans = modelContextBuilder.BuildDeferred();
            var timeThreshold = TimeThreshold.BuildLatest();
            var changesetProxy = new ChangesetProxy(user.InDatabase, timeThreshold, ChangesetModel);

            var ciMappingContext = new CIMappingService.CIMappingContext(AttributeModel, RelationModel, TimeThreshold.BuildLatest());
            var affectedCIs = new HashSet<Guid>();
            var cisToCreate = new List<Guid>();
            foreach (var cic in data.CICandidates)
            {
                var attributes = cic.Attributes;
                var ciCandidateCIID = cic.TempCIID; // TODO: detect duplicate tempCIIDs in source -> throw exception if so, each candidate MUST have a unique ID
                try
                {
                    // find out if it's a new CI or an existing one
                    var foundCIIDs = await ciMappingService.TryToMatch(cic.IdentificationMethod, ciMappingContext, trans, logger);

                    // already affected/mapped CIs must not be mapped again, so we remove them
                    // if we wouldn't do that, the mapping process could map multiple candidates to the same target CI, which would result in an ingest error
                    for (int i = foundCIIDs.Count - 1; i >= 0; i--)
                    {
                        if (affectedCIs.Contains(foundCIIDs[i]))
                        {
                            foundCIIDs.RemoveAt(i);
                        }
                    }


                    Guid finalCIID;
                    if (!foundCIIDs.IsEmpty())
                    {
                        if (foundCIIDs.Count() == 1)
                            finalCIID = foundCIIDs.First();
                        else
                        {
                            // NOTE: how to deal with ambiguities? In other words: more than one CI fit, where to put the data?
                            // ciMappingService.TryToMatch() returns an already ordered list (by varying criteria, or - if nothing else - by CIID)
                            finalCIID = foundCIIDs.First();

                            logger.LogWarning($"Multiple CIs match for candidate with temp-ID {ciCandidateCIID}: {string.Join(",", foundCIIDs)}, taking first one");
                        }
                    }
                    else
                    {
                        // CI is new, create a ciid for it (we'll later batch create all CIs)
                        finalCIID = CIModel.CreateCIID(); // use a totally new CIID, do NOT use the temporary CIID of the ciCandidate
                        cisToCreate.Add(finalCIID);
                    }

                    // add to mapping context
                    ciMappingContext.AddTemp2FinallCIIDMapping(ciCandidateCIID, finalCIID);

                    cic.TempCIID = finalCIID; // we update the TempCIID of the CI candidate with its final ID

                    affectedCIs.Add(finalCIID);
                }
                catch (Exception e)
                {
                    throw new Exception($"Error mapping CI-candidate {ciCandidateCIID}", e);
                }
            }

            // TODO: mask handling
            var maskHandling = MaskHandlingForRemovalApplyNoMask.Instance;

            // batch process CI creation
            if (!cisToCreate.IsEmpty())
                await CIModel.BulkCreateCIs(cisToCreate, trans);

            var bulkAttributeData = new BulkCIAttributeDataLayerScope("", writeLayer.ID, data.CICandidates.SelectMany(cic =>
                cic.Attributes.Fragments.Select(f => new BulkCIAttributeDataLayerScope.Fragment(f.Name, f.Value, cic.TempCIID))
            ));
            // TODO: return number of affected attributes (instead of CIs)
            await AttributeModel.BulkReplaceAttributes(bulkAttributeData, changesetProxy, new DataOriginV1(DataOriginType.InboundIngest), trans, maskHandling);

            var relationFragments = new List<BulkRelationDataLayerScope.Fragment>();
            foreach (var cic in data.RelationCandidates)
            {
                // TODO: make it work with other usecases, such as where the final CIID is known and/or the relevant CIs are already present in omnikeeper
                // find CIIDs
                var tempFromCIID = cic.IdentificationMethodFromCI.CIID;
                if (!ciMappingContext.TryGetMappedTemp2FinalCIID(tempFromCIID, out Guid fromCIID))
                    throw new Exception($"Could not find temporary CIID {tempFromCIID}, tried using it as the \"from\" of a relation");
                var tempToCIID = cic.IdentificationMethodToCI.CIID;
                if (!ciMappingContext.TryGetMappedTemp2FinalCIID(tempToCIID, out Guid toCIID))
                    throw new Exception($"Could not find temporary CIID {tempToCIID}, tried using it as the \"to\" of a relation");
                relationFragments.Add(new BulkRelationDataLayerScope.Fragment(fromCIID, toCIID, cic.PredicateID));
            }
            var bulkRelationData = new BulkRelationDataLayerScope(writeLayer.ID, relationFragments.ToArray());
            var affectedRelations = await RelationModel.BulkReplaceRelations(bulkRelationData, changesetProxy, new DataOriginV1(DataOriginType.InboundIngest), trans, maskHandling);

            trans.Commit();

            return (affectedCIs.Count(), affectedRelations.Count());
        }
    }

    public class CICandidate
    {
        public Guid TempCIID { get; set; }
        public ICIIdentificationMethod IdentificationMethod { get; private set; }
        public CICandidateAttributeData Attributes { get; private set; }

        public CICandidate(Guid tempCIID, ICIIdentificationMethod identificationMethod, CICandidateAttributeData attributes)
        {
            TempCIID = tempCIID;
            IdentificationMethod = identificationMethod;
            Attributes = attributes;
        }

        public static CICandidate BuildWithAdditionalAttributes(CICandidate @base, CICandidateAttributeData additionalAttributes)
        {
            return new CICandidate(@base.TempCIID, @base.IdentificationMethod, @base.Attributes.Concat(additionalAttributes));
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
