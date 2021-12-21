﻿using Microsoft.Extensions.Logging;
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
        public async Task<ISet<Guid>> TryToMatch(string ciCandidateID, ICIIdentificationMethod method, CIMappingContext ciMappingContext, IModelContext trans, ILogger logger)
        {
            switch (method)
            {
                case CIIdentificationMethodByData d: // use identifiable data for finding out CIID

                    ISet<Guid> candidateCIIDs = new HashSet<Guid>();
                    var isFirst = true;
                    foreach (var f in d.IdentifiableFragments)
                    {
                        var ciids = await ciMappingContext.GetMergedCIIDsByAttributeNameAndValue(f.Name, f.Value, d.SearchableLayers, trans);
                        if (isFirst)
                            candidateCIIDs.UnionWith(ciids);
                        else
                            candidateCIIDs.IntersectWith(ciids);
                        isFirst = false;
                    }

                    return candidateCIIDs;
                case CIIdentificationMethodByTemporaryCIID t:
                    if (!ciMappingContext.TryGetMappedTemp2FinalCIID(t.CIID, out Guid ciid))
                        throw new Exception($"Could not find temporary CIID {t.CIID} while trying to match CICandidate {ciCandidateID}");
                    return new HashSet<Guid>() { ciid };
                case CIIdentificationMethodByFirstOf f:
                    foreach(var inner in f.Inner)
                    {
                        var r = await TryToMatch(ciCandidateID, inner, ciMappingContext, trans, logger);
                        if (!r.IsEmpty())
                            return r;
                    }
                    return new HashSet<Guid>() { };
                case CIIdentificationMethodByCIID c:
                    return new HashSet<Guid>() { c.CIID };
                case CIIdentificationMethodNoop _:
                    return new HashSet<Guid>() { };
                default:
                    logger.LogWarning("Unknown CI Identification method detected");
                    return new HashSet<Guid>() { };
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
                if (attributeCache.TryGetValue(name, out var ac))
                {
                    return ac[value];
                } else
                {
                    var attributes = await attributeModel.FindMergedAttributesByFullName(name, new AllCIIDsSelection(), searchableLayers, trans, atTime);
                    var attributesLookup = attributes.ToLookup(kv => kv.Value.Attribute.Value, kv => kv.Key);
                    attributeCache[name] = attributesLookup;
                    return attributesLookup[value];
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
    public class CIIdentificationMethodByFirstOf : ICIIdentificationMethod
    {
        public ICIIdentificationMethod[] Inner { get; private set; }
        public static CIIdentificationMethodByFirstOf Build(ICIIdentificationMethod[] inner)
        {
            return new CIIdentificationMethodByFirstOf() { Inner = inner };
        }
        private CIIdentificationMethodByFirstOf() { Inner = Array.Empty<ICIIdentificationMethod>(); }
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
