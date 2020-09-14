﻿using Landscape.Base.Entity;
using Landscape.Base.Model;
using Landscape.Base.Service;
using Landscape.Base.Utils;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapeRegistry.Service
{

    public class IngestDataService
    {
        private readonly CIMappingService ciMappingService;

        private IAttributeModel AttributeModel { get; }
        private ICIModel CIModel { get; }
        private NpgsqlConnection Connection { get; }
        private IChangesetModel ChangesetModel { get; }
        private IRelationModel RelationModel { get; }

        public IngestDataService(IAttributeModel attributeModel, ICIModel ciModel, IChangesetModel changesetModel, IRelationModel relationModel, CIMappingService ciMappingService, NpgsqlConnection connection)
        {
            AttributeModel = attributeModel;
            CIModel = ciModel;
            ChangesetModel = changesetModel;
            RelationModel = relationModel;
            this.ciMappingService = ciMappingService;
            Connection = connection;
        }


        public async Task<(int numIngestedCIs, int numIngestedRelations)> Ingest(IngestData data, Layer writeLayer, AuthenticatedUser user, ILogger logger)
        {
            using var trans = Connection.BeginTransaction();
            var changesetProxy = ChangesetProxy.Build(user.InDatabase, DateTimeOffset.Now, ChangesetModel);

            var timeThreshold = TimeThreshold.BuildLatest();

            var ciMappingContext = new CIMappingService.CIMappingContext(AttributeModel, TimeThreshold.BuildLatest());
            var attributeData = new Dictionary<Guid, CICandidateAttributeData>();
            foreach (var cic in data.CICandidates)
            {
                var attributes = cic.Value.Attributes;
                var ciCandidateID = cic.Key;

                // find out if it's a new CI or an existing one
                var foundCIID = await ciMappingService.TryToMatch(ciCandidateID.ToString(), cic.Value.IdentificationMethod, ciMappingContext, trans, logger);

                Guid finalCIID;
                if (foundCIID.HasValue)
                {
                    finalCIID = foundCIID.Value;
                } else
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

            var bulkAttributeData = BulkCIAttributeDataLayerScope.Build("", writeLayer.ID, attributeData.SelectMany(ad =>
                ad.Value.Fragments.Select(f => BulkCIAttributeDataLayerScope.Fragment.Build(f.Name, f.Value, ad.Key))
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
                relationFragments.Add(BulkRelationDataLayerScope.Fragment.Build(fromCIID, toCIID, cic.PredicateID));
            }
            var bulkRelationData = BulkRelationDataLayerScope.Build(writeLayer.ID, relationFragments.ToArray());
            await RelationModel.BulkReplaceRelations(bulkRelationData, changesetProxy, trans);

            trans.Commit();

            return (attributeData.Keys.Count, bulkRelationData.Fragments.Length);
        }
    }

    public class CICandidate
    {
        public ICIIdentificationMethod IdentificationMethod { get; private set; }
        public CICandidateAttributeData Attributes { get; private set; }

        public static CICandidate Build(ICIIdentificationMethod identificationMethod, CICandidateAttributeData attributes)
        {
            return new CICandidate()
            {
                IdentificationMethod = identificationMethod,
                Attributes = attributes
            };
        }

        public static CICandidate BuildWithAdditionalAttributes(CICandidate @base, CICandidateAttributeData additionalAttributes)
        {
            return new CICandidate()
            {
                IdentificationMethod = @base.IdentificationMethod,
                Attributes = @base.Attributes.Concat(additionalAttributes)
            };
        }
    }

    public class RelationCandidate
    {
        public CIIdentificationMethodByTemporaryCIID IdentificationMethodFromCI { get; private set; }
        public CIIdentificationMethodByTemporaryCIID IdentificationMethodToCI { get; private set; }
        public string PredicateID { get; private set; }

        public static RelationCandidate Build(CIIdentificationMethodByTemporaryCIID identificationMethodFromCI, CIIdentificationMethodByTemporaryCIID identificationMethodToCI, string predicateID)
        {
            return new RelationCandidate()
            {
                IdentificationMethodFromCI = identificationMethodFromCI,
                IdentificationMethodToCI = identificationMethodToCI,
                PredicateID = predicateID
            };
        }
    }

    public class IngestData
    {
        public IDictionary<Guid, CICandidate> CICandidates { get; private set; }
        public IEnumerable<RelationCandidate> RelationCandidates { get; private set; }
        // TODO: relation candidates
        public static IngestData Build(IDictionary<Guid, CICandidate> cis, IEnumerable<RelationCandidate> relationCandidates)
        {
            return new IngestData()
            {
                CICandidates = cis,
                RelationCandidates = relationCandidates
            };
        }
    }
}
