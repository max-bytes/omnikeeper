using Landscape.Base.Entity;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using LandscapeRegistry.Utils;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapeRegistry.Model
{
    public class CISearchModel : ICISearchModel
    {
        private readonly IAttributeModel attributeModel;
        private readonly ICIModel ciModel;
        private readonly ITraitModel traitModel;
        private readonly ILayerModel layerModel;

        public CISearchModel(IAttributeModel attributeModel, ICIModel ciModel, ITraitModel traitModel, ILayerModel layerModel)
        {
            this.attributeModel = attributeModel;
            this.ciModel = ciModel;
            this.traitModel = traitModel;
            this.layerModel = layerModel;
        }

        public async Task<IEnumerable<CompactCI>> FindCIsWithName(string CIName, LayerSet layerSet, NpgsqlTransaction trans, TimeThreshold timeThreshold)
        {
            // TODO: performance improvements, TODO: use ciModel.getCINames() instead?
            var ciNamesFromNameAttributes = await attributeModel.FindMergedAttributesByFullName(ICIModel.NameAttribute, new AllCIIDsSelection(), false, layerSet, trans, timeThreshold);
            var foundCIIDs = ciNamesFromNameAttributes.Where(a => a.Value.Attribute.Value.Value2String().Equals(CIName)).Select(a => a.Key).ToHashSet();
            var cis = await ciModel.GetCompactCIs(layerSet, trans, timeThreshold, foundCIIDs);
            return cis;
        }

        public async Task<IEnumerable<CompactCI>> SimpleSearch(string searchString, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            var finalSS = searchString.Trim();
            var foundCIIDs = new HashSet<Guid>();

            var ls = await layerModel.BuildLayerSet(trans);

            if (Guid.TryParse(finalSS, out var guid))
            {
                foundCIIDs = (await ciModel.GetCIIDs(trans)).Where(ciid => ciid.Equals(guid)).ToHashSet(); // TODO: performance improvement
            }
            else if (finalSS.Length > 0)
            {
                // TODO: performance improvements, TODO: use ciModel.getCINames() instead?
                var ciNamesFromNameAttributes = await attributeModel.FindMergedAttributesByFullName(ICIModel.NameAttribute, new AllCIIDsSelection(), false, ls, trans, atTime);
                foundCIIDs = ciNamesFromNameAttributes.Where(kv => kv.Value.Attribute.Value.FullTextSearch(finalSS, System.Globalization.CompareOptions.IgnoreCase))
                    .Select(kv => kv.Key).ToHashSet();
            }
            else
            {
                foundCIIDs = (await ciModel.GetCIIDs(trans)).ToHashSet();
            }

            var cis = await ciModel.GetCompactCIs(ls, trans, atTime, foundCIIDs);

            // HACK, properly sort unnamed CIs
            return cis.OrderBy(t => t.Name ?? "ZZZZZZZZZZZ").Take(500);
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
            else if(finalSS.Length > 0)
            {
                searchAllCIsBasedOnSearchString = false;
                // TODO: performance improvements, TODO: use ciModel.getCINames() instead?
                var ciNamesFromNameAttributes = await attributeModel.FindMergedAttributesByFullName(ICIModel.NameAttribute, new AllCIIDsSelection(), false, layerSet, trans, atTime);
                foundCIIDs = ciNamesFromNameAttributes.Where(kv => kv.Value.Attribute.Value.FullTextSearch(finalSS, System.Globalization.CompareOptions.IgnoreCase))
                    .Select(kv => kv.Key).ToHashSet();
            }
            else 
            {
                foundCIIDs = (await ciModel.GetCIIDs(trans)).ToHashSet();
            }

            var resultIsReducedByETs = false;
            foreach(var etName in withEffectiveTraits)
            {
                var ciFilter = (searchAllCIsBasedOnSearchString && !resultIsReducedByETs) ? (Func<Guid, bool>)null : (ciid) => foundCIIDs.Contains(ciid);
                var ets = await traitModel.CalculateEffectiveTraitSetsForTraitName(etName, layerSet, trans, atTime, ciFilter);
                if (ets == null)
                { // searching for a non-existing trait -> make result empty and bail
                    foundCIIDs = new HashSet<Guid>();
                    break;
                }
                var cisFulfillingTraitRequirement = ets.Select(et => et.UnderlyingCI.ID);
                foundCIIDs = cisFulfillingTraitRequirement.ToHashSet(); // reduce the number of cis to the ones that fulfill this trait requirement
                resultIsReducedByETs = true;
            }

            var cis = await ciModel.GetCompactCIs(layerSet, trans, atTime, foundCIIDs);

            // HACK, properly sort unnamed CIs
            return cis.OrderBy(t => t.Name ?? "ZZZZZZZZZZZ").Take(500);
        }
    }
}
