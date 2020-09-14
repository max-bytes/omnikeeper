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
using System.Threading.Tasks;

namespace Landscape.Base.Service
{
    public class CIMappingService
    {
        public async Task<Guid?> TryToMatch(string ciCandidateID, ICIIdentificationMethod method, CIMappingContext ciMappingContext, NpgsqlTransaction trans, ILogger logger)
        {
            Guid? ciid = null;
            switch (method)
            {
                case CIIdentificationMethodByData d: // use identifiable data for finding out CIID

                    var candidateCIIDs = new List<Guid>();
                    var isFirst = true;
                    foreach (var f in d.IdentifiableFragments)
                    {
                        var ma = await ciMappingContext.GetMergedAttributesByAttributeNameAndValue(f.Name, f.Value, d.SearchableLayers, trans);
                        if (isFirst)
                            candidateCIIDs = new List<Guid>(ma.Keys);
                        else
                            candidateCIIDs = candidateCIIDs.Intersect(ma.Keys).ToList();
                        isFirst = false;
                    }

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
                    if (!ciMappingContext.TryGetMappedTemp2FinalCIID(t.CIID, out ciid))
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

        /// <summary>
        /// class is used to store intermediate data while mapping multiple CIs, such as cached data
        /// </summary>
        public class CIMappingContext
        {
            private readonly IAttributeModel attributeModel;
            private readonly TimeThreshold atTime;

            private readonly IDictionary<string, IImmutableDictionary<Guid, MergedCIAttribute>> attributeCache = new Dictionary<string, IImmutableDictionary<Guid, MergedCIAttribute>>();
            private readonly IDictionary<Guid, Guid> temp2finalCIIDMapping = new Dictionary<Guid, Guid>();


            public CIMappingContext(IAttributeModel attributeModel, TimeThreshold atTime)
            {
                this.attributeModel = attributeModel;
                this.atTime = atTime;
            }

            internal async Task<IDictionary<Guid, MergedCIAttribute>> GetMergedAttributesByAttributeNameAndValue(string name, IAttributeValue value, LayerSet searchableLayers, NpgsqlTransaction trans)
            {
                if (!attributeCache.ContainsKey(name))
                {
                    attributeCache[name] = await attributeModel.FindMergedAttributesByFullName(name, new AllCIIDsSelection(), searchableLayers, trans, atTime);
                }
                // TODO: performance improvement: instead of doing a where() based linear search, use a lookup
                var found = attributeCache[name].Where(kv => kv.Value.Attribute.Value.Equals(value)).ToDictionary(kv => kv.Key, kv => kv.Value);
                return found;
            }

            public bool TryGetMappedTemp2FinalCIID(Guid temp, out Guid final)
            {
                return temp2finalCIIDMapping.TryGetValue(temp, out final);
            }
            public bool TryGetMappedTemp2FinalCIID(Guid temp, out Guid? final)
            {
                return temp2finalCIIDMapping.TryGetValue(temp, out final);
            }

            public void AddTemp2FinallCIIDMapping(Guid temp, Guid final)
            {
                temp2finalCIIDMapping.Add(temp, final);
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
