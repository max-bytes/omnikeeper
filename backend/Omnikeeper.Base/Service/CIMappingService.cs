using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Service
{
    public class CIMappingService
    {
        public async Task<IEnumerable<Guid>> TryToMatch(string ciCandidateID, ICIIdentificationMethod method, CIMappingContext ciMappingContext, IModelContext trans, ILogger logger)
        {
            switch (method)
            {
                case CIIdentificationMethodByData d: // use identifiable data for finding out CIID

                    var candidateCIIDs = new List<Guid>();
                    var isFirst = true;
                    foreach (var f in d.IdentifiableFragments)
                    {
                        var ciids = await ciMappingContext.GetMergedCIIDsByAttributeNameAndValue(f.Name, f.Value, d.SearchableLayers, trans);
                        if (isFirst)
                            candidateCIIDs.AddRange(ciids);
                        else
                            candidateCIIDs = candidateCIIDs.Intersect(ciids).ToList();
                        isFirst = false;
                    }

                    return candidateCIIDs;
                case CIIdentificationMethodByTemporaryCIID t:
                    if (!ciMappingContext.TryGetMappedTemp2FinalCIID(t.CIID, out Guid ciid))
                        throw new Exception($"Could not find temporary CIID {t.CIID} while trying to match CICandidate {ciCandidateID}");
                    return new List<Guid>() { ciid };
                case CIIdentificationMethodByCIID c:
                    return new List<Guid>() { c.CIID };
                case CIIdentificationMethodNoop _:
                    return new List<Guid>() { };
                default:
                    logger.LogWarning("Unknown CI Identification method detected");
                    return new List<Guid>() { };
            }
        }

        /// <summary>
        /// class is used to store intermediate data while mapping multiple CIs, such as cached data
        /// </summary>
        public class CIMappingContext
        {
            private readonly IAttributeModel attributeModel;
            private readonly TimeThreshold atTime;

            private readonly IDictionary<string, ILookup<IAttributeValue, Guid>> attributeCache = new Dictionary<string, ILookup<IAttributeValue, Guid>>();
            private readonly IDictionary<Guid, Guid> temp2finalCIIDMapping = new Dictionary<Guid, Guid>();


            public CIMappingContext(IAttributeModel attributeModel, TimeThreshold atTime)
            {
                this.attributeModel = attributeModel;
                this.atTime = atTime;
            }

            internal async Task<IEnumerable<Guid>> GetMergedCIIDsByAttributeNameAndValue(string name, IAttributeValue value, LayerSet searchableLayers, IModelContext trans)
            {
                if (!attributeCache.ContainsKey(name))
                {
                    var attributes = await attributeModel.FindMergedAttributesByFullName(name, new AllCIIDsSelection(), searchableLayers, trans, atTime);
                    attributeCache[name] = attributes.ToLookup(kv => kv.Value.Attribute.Value, kv => kv.Key);
                }
                var ac = attributeCache[name];
                if (ac.Contains(value))
                {
                    return ac[value];
                } else
                {
                    return new List<Guid>();
                }
            }

            public bool TryGetMappedTemp2FinalCIID(Guid temp, out Guid final)
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

            public Fragment(string name, IAttributeValue value)
            {
                Name = name;
                Value = value;
            }
        }

        public Fragment[] Fragments { get; private set; }

        public CICandidateAttributeData(IEnumerable<Fragment> fragments)
        {
            Fragments = fragments.ToArray();
        }

        public CICandidateAttributeData Concat(CICandidateAttributeData attributes)
        {
            return new CICandidateAttributeData(Fragments.Concat(attributes.Fragments));
        }
    }

    public interface ICIIdentificationMethod
    {

    }

    public class CIIdentificationMethodByData : ICIIdentificationMethod
    {
        private CIIdentificationMethodByData(CICandidateAttributeData.Fragment[] identifiableFragments, LayerSet searchableLayers)
        {
            IdentifiableFragments = identifiableFragments;
            SearchableLayers = searchableLayers;
        }

        public CICandidateAttributeData.Fragment[] IdentifiableFragments { get; private set; }
        public LayerSet SearchableLayers { get; private set; }

        public static CIIdentificationMethodByData BuildFromAttributes(string[] identifiableAttributes, CICandidateAttributeData allAttributes, LayerSet searchableLayers)
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

            return new CIIdentificationMethodByData(identifiableFragments.ToArray(), searchableLayers);
        }

        public static CIIdentificationMethodByData BuildFromFragments(IEnumerable<CICandidateAttributeData.Fragment> fragments, LayerSet searchableLayers)
        {
            return new CIIdentificationMethodByData(fragments.ToArray(), searchableLayers);
        }
    }
    public class CIIdentificationMethodByTemporaryCIID : ICIIdentificationMethod
    {
        public Guid CIID { get; private set; }
        public static CIIdentificationMethodByTemporaryCIID Build(Guid ciid)
        {
            return new CIIdentificationMethodByTemporaryCIID() { CIID = ciid };
        }
        private CIIdentificationMethodByTemporaryCIID() { }
    }
    public class CIIdentificationMethodByCIID : ICIIdentificationMethod
    {
        public Guid CIID { get; private set; }
        public static CIIdentificationMethodByCIID Build(Guid ciid)
        {
            return new CIIdentificationMethodByCIID() { CIID = ciid };
        }
        private CIIdentificationMethodByCIID() { }
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
