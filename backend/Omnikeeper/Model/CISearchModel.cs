using Npgsql;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Model
{
    public class CISearchModel : ICISearchModel
    {
        private readonly IAttributeModel attributeModel;
        private readonly ICIModel ciModel;
        private readonly IEffectiveTraitModel traitModel;
        private readonly ILayerModel layerModel;

        public CISearchModel(IAttributeModel attributeModel, ICIModel ciModel, IEffectiveTraitModel traitModel, ILayerModel layerModel)
        {
            this.attributeModel = attributeModel;
            this.ciModel = ciModel;
            this.traitModel = traitModel;
            this.layerModel = layerModel;
        }

        public async Task<IEnumerable<CompactCI>> FindCIsWithName(string CIName, LayerSet layerSet, NpgsqlTransaction trans, TimeThreshold timeThreshold)
        {
            // TODO: performance improvements, TODO: use ciModel.getCINames() instead?
            var ciNamesFromNameAttributes = await attributeModel.FindMergedAttributesByFullName(ICIModel.NameAttribute, new AllCIIDsSelection(), layerSet, trans, timeThreshold);
            var foundCIIDs = ciNamesFromNameAttributes.Where(a => a.Value.Attribute.Value.Value2String().Equals(CIName)).Select(a => a.Key).ToHashSet();
            if (foundCIIDs.IsEmpty()) return ImmutableArray<CompactCI>.Empty;
            var cis = await ciModel.GetCompactCIs(SpecificCIIDsSelection.Build(foundCIIDs), layerSet, trans, timeThreshold);
            return cis;
        }

        public async Task<IEnumerable<CompactCI>> SimpleSearch(string searchString, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            var finalSS = searchString.Trim();

            var ls = await layerModel.BuildLayerSet(trans);

            IEnumerable<CompactCI> cis = ImmutableArray<CompactCI>.Empty;
            if (Guid.TryParse(finalSS, out var guid))
            {
                cis = await ciModel.GetCompactCIs(SpecificCIIDsSelection.Build(guid), ls, trans, atTime);
            }
            else if (finalSS.Length > 0)
            {
                // TODO: performance improvements, TODO: use ciModel.getCINames() instead?
                var ciNamesFromNameAttributes = await attributeModel.FindMergedAttributesByFullName(ICIModel.NameAttribute, new AllCIIDsSelection(), ls, trans, atTime);
                var foundCIIDs = ciNamesFromNameAttributes.Where(kv =>
                {
                    if (kv.Value.Attribute.Value is IAttributeValueText t)
                    {
                        return t.FullTextSearch(finalSS, System.Globalization.CompareOptions.IgnoreCase);
                    }
                    return false;
                })
                    .Select(kv => kv.Key).ToHashSet();
                if (!foundCIIDs.IsEmpty())
                    cis = await ciModel.GetCompactCIs(SpecificCIIDsSelection.Build(foundCIIDs), ls, trans, atTime);
            }
            else
            {
                cis = await ciModel.GetCompactCIs(new AllCIIDsSelection(), ls, trans, atTime);
            }


            // HACK, properly sort unnamed CIs
            return cis.OrderBy(t => t.Name ?? "ZZZZZZZZZZZ").Take(1500); // TODO: remove hard limit, customize
        }

        public async Task<IEnumerable<CompactCI>> AdvancedSearch(string searchString, string[] withEffectiveTraits, LayerSet layerSet, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            var finalSS = searchString.Trim();
            var foundCIIDs = new HashSet<Guid>();

            if (withEffectiveTraits.Length <= 0) // if no traits are specified, there cannot be any results -> return early
                return ImmutableArray<CompactCI>.Empty;

            var searchAllCIsBasedOnSearchString = true;
            if (Guid.TryParse(finalSS, out var guid))
            {
                searchAllCIsBasedOnSearchString = false;
                foundCIIDs = (await ciModel.GetCIIDs(trans)).Where(ciid => ciid.Equals(guid)).ToHashSet(); // TODO: performance improvement
            }
            else if (finalSS.Length > 0)
            {
                searchAllCIsBasedOnSearchString = false;
                // TODO: performance improvements, TODO: use ciModel.getCINames() instead?
                var ciNamesFromNameAttributes = await attributeModel.FindMergedAttributesByFullName(ICIModel.NameAttribute, new AllCIIDsSelection(), layerSet, trans, atTime);
                foundCIIDs = ciNamesFromNameAttributes.Where(kv =>
                {
                    if (kv.Value.Attribute.Value is IAttributeValueText t)
                    {
                        return t.FullTextSearch(finalSS, System.Globalization.CompareOptions.IgnoreCase);
                    }
                    return false;
                })
                    .Select(kv => kv.Key).ToHashSet();
            }
            else
            {
                foundCIIDs = (await ciModel.GetCIIDs(trans)).ToHashSet(); // TODO: performance improvement
            }

            var resultIsReducedByETs = false;
            foreach (var etName in withEffectiveTraits)
            {
                var ciFilter = (searchAllCIsBasedOnSearchString && !resultIsReducedByETs) ? (Func<Guid, bool>)null : (ciid) => foundCIIDs.Contains(ciid);
                // TODO: replace with something less performance intensive, that only fetches the CIIDs (and also cached)
                var ets = await traitModel.CalculateEffectiveTraitsForTraitName(etName, layerSet, trans, atTime, ciFilter);
                if (ets == null)
                { // searching for a non-existing trait -> make result empty and bail
                    foundCIIDs = new HashSet<Guid>();
                    break;
                }
                var cisFulfillingTraitRequirement = ets.Select(et => et.Key);
                foundCIIDs = cisFulfillingTraitRequirement.ToHashSet(); // reduce the number of cis to the ones that fulfill this trait requirement
                resultIsReducedByETs = true;
            }

            if (foundCIIDs.IsEmpty())
                return ImmutableArray<CompactCI>.Empty;

            var cis = await ciModel.GetCompactCIs(SpecificCIIDsSelection.Build(foundCIIDs), layerSet, trans, atTime);

            // HACK, properly sort unnamed CIs
            return cis.OrderBy(t => t.Name ?? "ZZZZZZZZZZZ").Take(500);
        }
    }
}
