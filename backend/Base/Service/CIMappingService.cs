using Landscape.Base.Entity;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using LandscapeRegistry.Entity.AttributeValues;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Landscape.Base.Service
{
    public class CIMappingService
    {
        public static async Task<Guid?> TryToMatch(string ciCandidateID, ICIIdentificationMethod method, IAttributeModel attributeModel,
            IDictionary<Guid, Guid> tempCIMappingContext, TimeThreshold timeThreshold, NpgsqlTransaction trans, ILogger logger)
        {
            Guid? ciid = null;
            switch (method)
            {
                case CIIdentificationMethodByData d: // use identifiable data for finding out CIID
                    var dataIdentifier = new DataIdentifier(attributeModel, timeThreshold);
                    var candidateCIIDs = await dataIdentifier.Identify(d, trans);
                    if (!candidateCIIDs.IsEmpty())
                    { // we found at least one fitting ci, use that // TODO: order matters!!! Find out how to deal with that
                        if (candidateCIIDs.Count > 1)
                        {
                            logger.LogWarning($"Ambiguous identification of CICandidate {ciCandidateID}, using first one");
                        }
                        ciid = candidateCIIDs[0]; // simply use first matching ciid for now
                        logger.LogInformation($"Fitting CI found for identification. CI-ID: {ciid}");
                    }
                    else
                    {
                        // we didn't find a matching CI
                        logger.LogInformation($"No fitting CI found for identification");
                    }

                    break;
                case CIIdentificationMethodByTemporaryCIID t:
                    if (tempCIMappingContext == null)
                        throw new Exception($"Cannot match using temporary CIIDs when tempCIMappingContext is null");
                    if (!tempCIMappingContext.TryGetValue(t.CIID, out ciid))
                        throw new Exception($"Could not find temporary CIID {t.CIID} while trying to match CICandidate {ciCandidateID}");
                    break;
                case CIIdentificationMethodByCIID c:
                    ciid = c.CIID;
                    break;
                case CIIdentificationMethodNoop _:
                    break;
            }
            return ciid;
        }

        private class DataIdentifier
        {
            private readonly IDictionary<string, IImmutableDictionary<Guid, MergedCIAttribute>> attributeCache = new Dictionary<string, IImmutableDictionary<Guid, MergedCIAttribute>>();
            private readonly IAttributeModel attributeModel;
            private readonly TimeThreshold atTime;

            public DataIdentifier(IAttributeModel attributeModel, TimeThreshold atTime)
            {
                this.attributeModel = attributeModel;
                this.atTime = atTime;
            }

            private async Task<IDictionary<Guid, MergedCIAttribute>> GetMergedAttributesByAttributeNameAndValue(string name, IAttributeValue value, LayerSet searchableLayers, NpgsqlTransaction trans)
            {
                if (!attributeCache.ContainsKey(name))
                {
                    attributeCache[name] = await attributeModel.FindMergedAttributesByFullName(name, new AllCIIDsSelection(), searchableLayers, trans, atTime);
                }
                var found = attributeCache[name].Where(kv => kv.Value.Attribute.Value.Equals(value)).ToDictionary(kv => kv.Key, kv => kv.Value);
                return found;
            }

            public async Task<IList<Guid>> Identify(CIIdentificationMethodByData d, NpgsqlTransaction trans)
            {
                var candidateCIIDs = new List<Guid>();
                var isFirst = true;
                foreach (var f in d.IdentifiableFragments)
                {
                    var ma = await GetMergedAttributesByAttributeNameAndValue(f.Name, f.Value, d.SearchableLayers, trans);
                    if (isFirst)
                        candidateCIIDs = new List<Guid>(ma.Keys);
                    else
                        candidateCIIDs = candidateCIIDs.Intersect(ma.Keys).ToList();
                    isFirst = false;
                }
                return candidateCIIDs;
            }
        }
    }

    public class CICandidateAttributeData 
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

        public static CICandidateAttributeData Build(IEnumerable<Fragment> fragments)
        {
            return new CICandidateAttributeData()
            {
                Fragments = fragments.ToArray()
            };
        }

        public CICandidateAttributeData Concat(CICandidateAttributeData attributes)
        {
            return Build(Fragments.Concat(attributes.Fragments));
        }
    }

    public interface ICIIdentificationMethod
    {

    }

    public class CIIdentificationMethodByData : ICIIdentificationMethod
    {
        public CICandidateAttributeData.Fragment[] IdentifiableFragments { get; private set; }
        public LayerSet SearchableLayers { get; private set; }

        public static CIIdentificationMethodByData Build(string[] identifiableAttributes, CICandidateAttributeData allAttributes, LayerSet searchableLayers)
        {
            // TODO: build from candidate attributes already

            var identifiableFragments = identifiableAttributes.Select(ia =>
            {
                var identifiableFragment = allAttributes.Fragments.FirstOrDefault(f => f.Name.Equals(ia));
                if (identifiableFragment == null)
                {
                    throw new Exception($"Could not find identifiable attribute named {ia} in fragments");
                }
                return identifiableFragment;
            }).Where(f => f != null).ToList();

            return new CIIdentificationMethodByData() { IdentifiableFragments = identifiableFragments.ToArray(), SearchableLayers = searchableLayers };
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
        public static CIIdentificationMethodByCIID Build(Guid ciid)
        {
            return new CIIdentificationMethodByCIID() { CIID = ciid };
        }
    }

    public class CIIdentificationMethodNoop : ICIIdentificationMethod
    {
        private CIIdentificationMethodNoop() { }
        private readonly static CIIdentificationMethodNoop instance = new CIIdentificationMethodNoop();
        public static CIIdentificationMethodNoop Build()
        {
            return instance;
        }
    }


}
