using Microsoft.Extensions.Logging;
using Npgsql;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
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
        private IChangesetModel ChangesetModel { get; }
        private IRelationModel RelationModel { get; }

        public IngestDataService(IAttributeModel attributeModel, ICIModel ciModel, IChangesetModel changesetModel, IRelationModel relationModel, CIMappingService ciMappingService)
        {
            AttributeModel = attributeModel;
            CIModel = ciModel;
            ChangesetModel = changesetModel;
            RelationModel = relationModel;
            this.ciMappingService = ciMappingService;
        }

        // TODO: add ci-based authorization
        public async Task<(int numIngestedCIs, int numIngestedRelations)> Ingest(IngestData data, Layer writeLayer, AuthenticatedUser user, IModelContextBuilder modelContextBuilder, ILogger logger)
        {
            using var trans = modelContextBuilder.BuildDeferred();
            var changesetProxy = new ChangesetProxy(user.InDatabase, DateTimeOffset.Now, ChangesetModel);

            var timeThreshold = TimeThreshold.BuildLatest();

            var ciMappingContext = new CIMappingService.CIMappingContext(AttributeModel, TimeThreshold.BuildLatest());
            var attributeData = new Dictionary<Guid, CICandidateAttributeData>();
            foreach (var cic in data.CICandidates)
            {
                var attributes = cic.Value.Attributes;
                var ciCandidateID = cic.Key;

                // find out if it's a new CI or an existing one
                var foundCIIDs = await ciMappingService.TryToMatch(ciCandidateID.ToString(), cic.Value.IdentificationMethod, ciMappingContext, trans, logger);

                Guid finalCIID;
                if (!foundCIIDs.IsEmpty())
                {
                    finalCIID = foundCIIDs.First(); // TODO: how to deal with ambiguities? In other words: more than one CI fit, where to put the data?
                }
                else
                {
                    // CI is new, create it first
                    // TODO: batch process CI creation
                    finalCIID = await CIModel.CreateCI(trans); // use a totally new CIID, do NOT use the temporary CIID of the ciCandidate
                }

                // add to mapping context
                ciMappingContext.AddTemp2FinallCIIDMapping(ciCandidateID, finalCIID);

                if (attributeData.ContainsKey(finalCIID))
                    attributeData[finalCIID] = attributeData[finalCIID].Concat(attributes);
                else
                    attributeData.Add(finalCIID, attributes);
            }

            var bulkAttributeData = new BulkCIAttributeDataLayerScope("", writeLayer.ID, attributeData.SelectMany(ad =>
                ad.Value.Fragments.Select(f => new BulkCIAttributeDataLayerScope.Fragment(f.Name, f.Value, ad.Key))
            ));
            await AttributeModel.BulkReplaceAttributes(bulkAttributeData, changesetProxy, trans);


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
            await RelationModel.BulkReplaceRelations(bulkRelationData, changesetProxy, trans);

            trans.Commit();

            return (attributeData.Keys.Count, bulkRelationData.Fragments.Length);
        }
    }

    public class CICandidate
    {
        public ICIIdentificationMethod IdentificationMethod { get; private set; }
        public CICandidateAttributeData Attributes { get; private set; }

        public CICandidate(ICIIdentificationMethod identificationMethod, CICandidateAttributeData attributes)
        {
            IdentificationMethod = identificationMethod;
            Attributes = attributes;
        }

        public static CICandidate BuildWithAdditionalAttributes(CICandidate @base, CICandidateAttributeData additionalAttributes)
        {
            return new CICandidate(@base.IdentificationMethod, @base.Attributes.Concat(additionalAttributes));
        }
    }

    public class RelationCandidate
    {
        public CIIdentificationMethodByTemporaryCIID IdentificationMethodFromCI { get; private set; }
        public CIIdentificationMethodByTemporaryCIID IdentificationMethodToCI { get; private set; }
        public string PredicateID { get; private set; }

        public RelationCandidate(CIIdentificationMethodByTemporaryCIID identificationMethodFromCI, CIIdentificationMethodByTemporaryCIID identificationMethodToCI, string predicateID)
        {
            IdentificationMethodFromCI = identificationMethodFromCI;
            IdentificationMethodToCI = identificationMethodToCI;
            PredicateID = predicateID;
        }
    }

    public class IngestData
    {
        public IDictionary<Guid, CICandidate> CICandidates { get; private set; }
        public IEnumerable<RelationCandidate> RelationCandidates { get; private set; }

        public IngestData(IDictionary<Guid, CICandidate> cis, IEnumerable<RelationCandidate> relationCandidates)
        {
            CICandidates = cis;
            RelationCandidates = relationCandidates;
        }
    }
}
