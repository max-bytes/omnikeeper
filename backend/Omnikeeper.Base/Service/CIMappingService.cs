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
        /// <summary>
        /// returns a distinct list of matching CIIDs, sorted by preference
        /// </summary>
        public async Task<IList<Guid>> TryToMatch(ICIIdentificationMethod method, ICIMappingContext ciMappingContext, IModelContext trans)
        {
            switch (method)
            {
                case CIIdentificationMethodByData d: // use identifiable data for finding out CIID
                    {
                        ISet<Guid> candidateCIIDs = new HashSet<Guid>();
                        var isFirst = true;
                        foreach (var f in d.IdentifiableFragments)
                        {
                            var ciids = await ciMappingContext.GetMergedCIIDsByAttributeNameAndValue(f.Name, f.Value, d.SearchableLayers, false, trans);
                            if (isFirst)
                                candidateCIIDs.UnionWith(ciids);
                            else
                                candidateCIIDs.IntersectWith(ciids);
                            isFirst = false;
                        }

                        return candidateCIIDs.OrderBy(ciid => ciid).ToList(); // order by ciid
                    }
                case CIIdentificationMethodByAttributeExists ae:
                    {
                        ISet<Guid> candidateCIIDs = new HashSet<Guid>();
                        var isFirst = true;
                        foreach (var attributeName in ae.Attributes)
                        {
                            var ciids = await ciMappingContext.GetMergedCIIDsByExistingAttribute(attributeName, ae.SearchableLayers, trans);
                            if (isFirst)
                                candidateCIIDs.UnionWith(ciids);
                            else
                                candidateCIIDs.IntersectWith(ciids);
                            isFirst = false;
                        }

                        return candidateCIIDs.OrderBy(ciid => ciid).ToList(); // order by ciid
                    }
                case CIIdentificationMethodByFragment f:
                    {
                        var candidateCIIDs = await ciMappingContext.GetMergedCIIDsByAttributeNameAndValue(f.Fragment.Name, f.Fragment.Value, f.SearchableLayers, f.CaseInsensitive, trans);

                        return candidateCIIDs.OrderBy(ciid => ciid).ToList(); // order by ciid
                    }
                case CIIdentificationMethodByRelatedTempCIID rt:
                    {
                        if (!ciMappingContext.TryGetMappedTemp2FinalCIID(rt.RelatedTempCIID, out Guid relatedCIID))
                        {
                            return new List<Guid>() { };
                            // TODO: we should actually check if the related CI was dropped because of a reason or if there's an actual error
                            //throw new Exception($"Could not find related temporary CIID {rt.RelatedTempCIID}");
                        }

                        var outgoing = !rt.OutgoingRelation; // NOTE: we invert the direction because we are coming from the related CI
                        var candidateCIIDs = await ciMappingContext.GetMergedCIIDsByRelation(relatedCIID, outgoing, rt.PredicateID, rt.SearchableLayers, trans);
                        return candidateCIIDs.OrderBy(ciid => ciid).ToList(); // order by ciid
                    }
                case CIIdentificationMethodByTempCIID t:
                    {
                        if (!ciMappingContext.TryGetMappedTemp2FinalCIID(t.CIID, out Guid ciid))
                            throw new Exception($"Could not find temporary CIID {t.CIID}");
                        return new List<Guid>() { ciid };
                    }
                case CIIdentificationMethodByIntersect a:
                    {
                        var ret = new List<Guid>();
                        var isFirst = true;
                        foreach (var inner in a.Inner)
                        {
                            var ciids = await TryToMatch(inner, ciMappingContext, trans);
                            if (isFirst)
                            {
                                ret.AddRange(ciids);
                            }
                            else
                            {
                                var tmpSet = ciids.ToHashSet();
                                for (int i = ret.Count - 1; i >= 0; i--)
                                {
                                    if (!tmpSet.Contains(ret[i]))
                                    {
                                        ret.RemoveAt(i);
                                    }
                                    else
                                    {
                                        tmpSet.Remove(ret[i]);
                                    }
                                }
                            }
                            isFirst = false;
                        }
                        return ret;
                    }
                case CIIdentificationMethodByUnion f:
                    {
                        var ret = new List<Guid>();
                        var tmpSet = new HashSet<Guid>();
                        foreach (var inner in f.Inner)
                        {
                            var r = await TryToMatch(inner, ciMappingContext, trans);
                            foreach (var rr in r)
                            {
                                if (!tmpSet.Contains(rr))
                                {
                                    ret.Add(rr);
                                    tmpSet.Add(rr);
                                }
                            }
                        }
                        return ret;
                    }
                case CIIdentificationMethodByCIID c:
                    return new List<Guid>() { c.CIID };
                case CIIdentificationMethodNoop _:
                    return new List<Guid>() { };
                default:
                    throw new Exception($"Unknown CI Identification method detected");
            }
        }

        /// <summary>
        /// class is used to store intermediate data while mapping multiple CIs, such as cached data
        /// </summary>
        public interface ICIMappingContext
        {
            Task<IEnumerable<Guid>> GetMergedCIIDsByRelation(Guid startCIID, bool outgoing, string predicateID, LayerSet searchableLayers, IModelContext trans);
            Task<IEnumerable<Guid>> GetMergedCIIDsByAttributeNameAndValue(string name, IAttributeValue value, LayerSet searchableLayers, bool caseInsensitive, IModelContext trans);
            Task<IEnumerable<Guid>> GetMergedCIIDsByExistingAttribute(string name, LayerSet searchableLayers, IModelContext trans);
            bool TryGetMappedTemp2FinalCIID(Guid temp, out Guid final);
            void AddTemp2FinallCIIDMapping(Guid temp, Guid final);
        }

        // mapping context that works without previous knowledge and builds its own caches while working
        public class CIMappingContext : ICIMappingContext
        {
            private readonly IAttributeModel attributeModel;
            private readonly IRelationModel relationModel;
            private readonly TimeThreshold atTime;

            private readonly IDictionary<string, ILookup<string, Guid>> attributeValueCache = new Dictionary<string, ILookup<string, Guid>>();
            private readonly IDictionary<string, IEnumerable<Guid>> attributeExistsCache = new Dictionary<string, IEnumerable<Guid>>();
            private readonly IDictionary<string, ILookup<Guid, Guid>> outgoingRelationsCache = new Dictionary<string, ILookup<Guid, Guid>>();
            private readonly IDictionary<string, ILookup<Guid, Guid>> incomingRelationsCache = new Dictionary<string, ILookup<Guid, Guid>>();

            private readonly IDictionary<Guid, Guid> temp2finalCIIDMapping = new Dictionary<Guid, Guid>();


            public CIMappingContext(IAttributeModel attributeModel, IRelationModel relationModel, TimeThreshold atTime)
            {
                this.attributeModel = attributeModel;
                this.relationModel = relationModel;
                this.atTime = atTime;
            }

            public async Task<IEnumerable<Guid>> GetMergedCIIDsByRelation(Guid startCIID, bool outgoing, string predicateID, LayerSet searchableLayers, IModelContext trans)
            {
                var cache = (outgoing) ? outgoingRelationsCache : incomingRelationsCache;
                if (cache.TryGetValue(predicateID, out var rc))
                {
                    return rc[startCIID];
                }
                else
                {
                    // TODO: mask handling?
                    var allRelations = await relationModel.GetMergedRelations(RelationSelectionWithPredicate.Build(predicateID), searchableLayers, trans, atTime, MaskHandlingForRetrievalGetMasks.Instance, GeneratedDataHandlingInclude.Instance);
                    var outgoingCache = allRelations.ToLookup(r => r.Relation.FromCIID, r => r.Relation.ToCIID);
                    outgoingRelationsCache[predicateID] = outgoingCache;
                    var incomingCache = allRelations.ToLookup(r => r.Relation.ToCIID, r => r.Relation.FromCIID);
                    incomingRelationsCache[predicateID] = incomingCache;

                    var c = (outgoing) ? outgoingCache : incomingCache;
                    return c[startCIID];
                }
            }

            public async Task<IEnumerable<Guid>> GetMergedCIIDsByAttributeNameAndValue(string name, IAttributeValue value, LayerSet searchableLayers, bool caseInsensitive, IModelContext trans)
            {
                if (value.IsArray)
                    throw new Exception("Searching by attribue value that is array is not supported");

                var valueKey = value.Value2String();

                if (caseInsensitive)
                {
                    valueKey = valueKey.ToLower();
                }

                var cacheKey = name + caseInsensitive;

                if (attributeValueCache.TryGetValue(cacheKey, out var ac))
                {
                    return ac[valueKey];
                }
                else
                {
                    var attributes = await attributeModel.FindMergedAttributesByFullName(name, AllCIIDsSelection.Instance, searchableLayers, trans, atTime);
                    var attributesLookup = attributes.ToLookup(kv =>
                    {
                        var v = kv.Value.Attribute.Value.Value2String();
                        if (caseInsensitive)
                            v = v.ToLower();
                        return v;
                    }, kv => kv.Key);
                    attributeValueCache[cacheKey] = attributesLookup;
                    return attributesLookup[valueKey];
                }
            }

            public async Task<IEnumerable<Guid>> GetMergedCIIDsByExistingAttribute(string name, LayerSet searchableLayers, IModelContext trans)
            {
                var cacheKey = name;
                if (attributeExistsCache.TryGetValue(cacheKey, out var ac))
                {
                    return ac;
                }
                else
                {
                    var attributes = await attributeModel.FindMergedAttributesByFullName(name, AllCIIDsSelection.Instance, searchableLayers, trans, atTime);
                    var ciids = attributes.Keys;
                    attributeExistsCache[cacheKey] = ciids;
                    return ciids;
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
        public CICandidateAttributeData Merge(CICandidateAttributeData attributesToMerge)
        {
            var fragmentsToAdd = new List<Fragment>();
            foreach(var fragmentToAdd in attributesToMerge.Fragments)
            {
                if (!Fragments.Any(f => f.Name == fragmentToAdd.Name))
                    fragmentsToAdd.Add(fragmentToAdd);
            }
            return new CICandidateAttributeData(Fragments.Concat(fragmentsToAdd));
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

    public class CIIdentificationMethodByAttributeExists : ICIIdentificationMethod
    {
        private CIIdentificationMethodByAttributeExists(string[] attributes, LayerSet searchableLayers)
        {
            Attributes = attributes;
            SearchableLayers = searchableLayers;
        }

        public string[] Attributes { get; private set; }
        public LayerSet SearchableLayers { get; private set; }

        public static CIIdentificationMethodByAttributeExists Build(string[] attributes, LayerSet searchableLayers)
        {
            return new CIIdentificationMethodByAttributeExists(attributes, searchableLayers);
        }
    }

    public class CIIdentificationMethodByFragment : ICIIdentificationMethod
    {
        private CIIdentificationMethodByFragment(CICandidateAttributeData.Fragment fragment, bool caseInsensitive, LayerSet searchableLayers)
        {
            Fragment = fragment;
            SearchableLayers = searchableLayers;
            CaseInsensitive = caseInsensitive;
        }

        public readonly CICandidateAttributeData.Fragment Fragment;
        public readonly LayerSet SearchableLayers;
        public readonly bool CaseInsensitive;

        public static CIIdentificationMethodByFragment Build(CICandidateAttributeData.Fragment fragment, bool caseInsensitive, LayerSet searchableLayers)
        {
            return new CIIdentificationMethodByFragment(fragment, caseInsensitive, searchableLayers);
        }
    }

    public class CIIdentificationMethodByRelatedTempCIID : ICIIdentificationMethod
    {
        public readonly Guid RelatedTempCIID;
        public readonly bool OutgoingRelation;
        public readonly string PredicateID;
        public readonly LayerSet SearchableLayers;

        private CIIdentificationMethodByRelatedTempCIID(Guid relatedTempCIID, bool outgoingRelation, string predicateID, LayerSet searchableLayers)
        {
            RelatedTempCIID = relatedTempCIID;
            OutgoingRelation = outgoingRelation;
            PredicateID = predicateID;
            SearchableLayers = searchableLayers;
        }

        public static CIIdentificationMethodByRelatedTempCIID Build(Guid relatedTempCIID, bool outgoingRelation, string predicateID, LayerSet searchLayers)
        {
            return new CIIdentificationMethodByRelatedTempCIID(relatedTempCIID, outgoingRelation, predicateID, searchLayers);
        }
    }

    public class CIIdentificationMethodByTempCIID : ICIIdentificationMethod
    {
        public Guid CIID { get; private set; }
        public static CIIdentificationMethodByTempCIID Build(Guid ciid)
        {
            return new CIIdentificationMethodByTempCIID() { CIID = ciid };
        }
        private CIIdentificationMethodByTempCIID() { }
    }
    public class CIIdentificationMethodByUnion : ICIIdentificationMethod
    {
        public ICIIdentificationMethod[] Inner { get; private set; }
        public static CIIdentificationMethodByUnion Build(ICIIdentificationMethod[] inner)
        {
            return new CIIdentificationMethodByUnion() { Inner = inner };
        }
        private CIIdentificationMethodByUnion() { Inner = Array.Empty<ICIIdentificationMethod>(); }
    }
    public class CIIdentificationMethodByIntersect : ICIIdentificationMethod
    {
        public ICIIdentificationMethod[] Inner { get; private set; }
        public static CIIdentificationMethodByIntersect Build(ICIIdentificationMethod[] inner)
        {
            return new CIIdentificationMethodByIntersect() { Inner = inner };
        }
        private CIIdentificationMethodByIntersect() { Inner = Array.Empty<ICIIdentificationMethod>(); }
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
