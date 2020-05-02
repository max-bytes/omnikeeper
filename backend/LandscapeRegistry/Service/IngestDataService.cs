using Landscape.Base.Entity;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using LandscapeRegistry.Entity.AttributeValues;
using LandscapeRegistry.Utils;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Landscape.Base.Model.IAttributeModel;

namespace LandscapeRegistry.Service
{
    public class IngestDataService
    {
        private IAttributeModel AttributeModel { get; }
        private ICIModel CIModel { get; }
        private NpgsqlConnection Connection { get; }
        private IChangesetModel ChangesetModel { get; }
        private IRelationModel RelationModel { get; }

        public IngestDataService(IAttributeModel attributeModel, ICIModel ciModel, IChangesetModel changesetModel, IRelationModel relationModel, NpgsqlConnection connection)
        {
            AttributeModel = attributeModel;
            CIModel = ciModel;
            ChangesetModel = changesetModel;
            RelationModel = relationModel;
            Connection = connection;
        }

        private class DataIdentifier
        {
            private readonly IDictionary<string, IDictionary<Guid, MergedCIAttribute>> attributeCache = new Dictionary<string, IDictionary<Guid, MergedCIAttribute>>();
            private readonly IAttributeModel attributeModel;
            private readonly LayerSet searchableLayers;
            private readonly TimeThreshold atTime;

            public DataIdentifier(IAttributeModel attributeModel, LayerSet searchableLayers, TimeThreshold atTime)
            {
                this.attributeModel = attributeModel;
                this.searchableLayers = searchableLayers;
                this.atTime = atTime;
            }

            private async Task<IDictionary<Guid, MergedCIAttribute>> GetMergedAttributesByAttributeNameAndValue(string name, IAttributeValue value, NpgsqlTransaction trans)
            {
                if (!attributeCache.ContainsKey(name))
                {
                    attributeCache[name] = await attributeModel.FindMergedAttributesByFullName(name, new AllCIIDsAttributeSelection(), false, searchableLayers, trans, atTime);
                }
                var found = attributeCache[name].Where(kv => kv.Value.Attribute.Value.Equals(value)).ToDictionary(kv => kv.Key, kv => kv.Value);
                return found;
            }

            public async Task<IList<Guid>> Identify(BulkCICandidateAttributeData attributeData, CIIdentificationMethodByData d, NpgsqlTransaction trans)
            {
                var identifiableFragments = d.IdentifiableAttributes.Select(ia =>
                {
                    return attributeData.Fragments.FirstOrDefault(f => f.Name == ia);
                }).Where(f => f != null).ToList();

                var candidateCIIDs = new List<Guid>();
                var isFirst = true;
                foreach (var f in identifiableFragments)
                {
                    var ma = await GetMergedAttributesByAttributeNameAndValue(f.Name, f.Value, trans);
                    if (isFirst)
                        candidateCIIDs = new List<Guid>(ma.Keys);
                    else
                        candidateCIIDs = candidateCIIDs.Intersect(ma.Keys).ToList();
                    isFirst = false;
                }
                return candidateCIIDs;
            }
        }

        public async Task<(Dictionary<Guid, Guid> idMapping, int numIngestedRelations)> Ingest(IngestData data, Layer writeLayer, LayerSet searchableLayers, User user, ILogger logger)
        {
            using var trans = Connection.BeginTransaction();
            var changeset = await ChangesetModel.CreateChangeset(user.InDatabase.ID, trans);

            var timeThreshold = TimeThreshold.BuildLatest();
            var dataIdentifier = new DataIdentifier(AttributeModel, searchableLayers, timeThreshold);
            var temp2finalCIIDMap = new Dictionary<Guid, Guid>();

            var attributeData = new Dictionary<Guid, BulkCICandidateAttributeData>();
            foreach (var cic in data.CICandidates)
            {
                // find out if it's a new CI or an existing one
                var ciid = cic.Key;
                var foundMatchingCI = false;
                switch (cic.Value.IdentificationMethod)
                {
                    case CIIdentificationMethodByData d: // use identifiable data for finding out CIID
                        var candidateCIIDs = await dataIdentifier.Identify(cic.Value.Attributes, d, trans);
                        if (!candidateCIIDs.IsEmpty())
                        { // we found at least one fitting ci, use that // TODO: order matters!!! Find out how to deal with that
                            if (candidateCIIDs.Count > 1)
                            {
                                logger.LogWarning($"Unambiguous identification of CICandidate {cic.Key}, using first one");
                            }
                            ciid = candidateCIIDs[0]; // simply use first matching ciid for now
                            foundMatchingCI = true;
                        }

                        break;
                    case CIIdentificationMethodByTemporaryCIID t:
                        if (!temp2finalCIIDMap.TryGetValue(t.CIID, out ciid))
                            throw new Exception($"Could not find temporary CIID {t.CIID}");
                        foundMatchingCI = true;
                        break;
                    case CIIdentificationMethodByCIID c:
                        ciid = c.CIID;
                        foundMatchingCI = true;
                        break;
                }

                var attributes = cic.Value.Attributes;
                if (!foundMatchingCI) {
                    // CI is new, create it first
                    // TODO: batch process CI creation
                    await CIModel.CreateCI(trans, ciid); // TODO: we are simply using the passed ciid here, check if that is ok, or might lead to problems later
                }

                // save ciid mapping
                temp2finalCIIDMap.Add(cic.Key, ciid);

                if (attributeData.ContainsKey(ciid))
                    attributeData[ciid] = attributeData[ciid].Concat(attributes);
                else
                    attributeData.Add(ciid, attributes);
            }

            var bulkAttributeData = BulkCIAttributeDataLayerScope.Build("", writeLayer.ID, attributeData.SelectMany(ad =>
                ad.Value.Fragments.Select(f => BulkCIAttributeDataLayerScope.Fragment.Build(f.Name, f.Value, ad.Key))
            ));
            await AttributeModel.BulkReplaceAttributes(bulkAttributeData, changeset.ID, trans);


            var relationFragments = new List<BulkRelationDataLayerScope.Fragment>();
            foreach (var cic in data.RelationCandidates)
            {
                // find CIIDs
                var tempFromCIID = cic.IdentificationMethodFromCI.CIID;
                if (!temp2finalCIIDMap.TryGetValue(tempFromCIID, out var fromCIID))
                    throw new Exception($"Could not find temporary CIID {tempFromCIID}, tried using it as the \"from\" of a relation");
                var tempToCIID = cic.IdentificationMethodToCI.CIID;
                if (!temp2finalCIIDMap.TryGetValue(tempToCIID, out var toCIID))
                    throw new Exception($"Could not find temporary CIID {tempToCIID}, tried using it as the \"to\" of a relation");
                relationFragments.Add(BulkRelationDataLayerScope.Fragment.Build(fromCIID, toCIID, cic.PredicateID));
            }
            var bulkRelationData = BulkRelationDataLayerScope.Build(writeLayer.ID, relationFragments.ToArray());
            await RelationModel.BulkReplaceRelations(bulkRelationData, changeset.ID, trans);

            trans.Commit();

            return (temp2finalCIIDMap, bulkRelationData.Fragments.Length);
        }
    }

    public interface ICIIdentificationMethod
    {

    }

    public class CIIdentificationMethodByData : ICIIdentificationMethod
    {
        public string[] IdentifiableAttributes { get; private set; }
        public static CIIdentificationMethodByData Build(string[] identifiableAttributes)
        {
            return new CIIdentificationMethodByData() { IdentifiableAttributes = identifiableAttributes };
        }
    }
    public class CIIdentificationMethodByTemporaryCIID : ICIIdentificationMethod
    {
        public Guid CIID { get; private set; }
        public static CIIdentificationMethodByTemporaryCIID Build(Guid ciid)
        {
            return new CIIdentificationMethodByTemporaryCIID() { CIID = ciid };
        }
    }
    public class CIIdentificationMethodByCIID : ICIIdentificationMethod
    {
        public Guid CIID { get; private set; }
    }

    public class BulkCICandidateAttributeData
    {
        public class Fragment
        {
            public string Name { get; private set; }
            public IAttributeValue Value { get; private set; }

            public static Fragment Build(string name, IAttributeValue value)
            {
                return new Fragment()
                {
                    Name = name,
                    Value = value
                };
            }
        }

        public Fragment[] Fragments { get; private set; }

        public static BulkCICandidateAttributeData Build(IEnumerable<Fragment> fragments)
        {
            return new BulkCICandidateAttributeData()
            {
                Fragments = fragments.ToArray()
            };
        }

        internal BulkCICandidateAttributeData Concat(BulkCICandidateAttributeData attributes)
        {
            return Build(Fragments.Concat(attributes.Fragments));
        }
    }

    public class CICandidate
    {
        public ICIIdentificationMethod IdentificationMethod { get; private set; }
        //private Guid TemporaryCIID { get; set; }// TODO: needed?
        public BulkCICandidateAttributeData Attributes { get; private set; }

        public static CICandidate Build(/*Guid temporaryCIID, */ICIIdentificationMethod identificationMethod, BulkCICandidateAttributeData attributes)
        {
            return new CICandidate()
            {
                //TemporaryCIID = temporaryCIID,
                IdentificationMethod = identificationMethod,
                Attributes = attributes
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
