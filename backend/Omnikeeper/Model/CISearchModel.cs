using Npgsql;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
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
        private readonly ITraitsProvider traitsProvider;

        public CISearchModel(IAttributeModel attributeModel, ICIModel ciModel, IEffectiveTraitModel traitModel, ILayerModel layerModel, ITraitsProvider traitsProvider)
        {
            this.attributeModel = attributeModel;
            this.ciModel = ciModel;
            this.traitModel = traitModel;
            this.layerModel = layerModel;
            this.traitsProvider = traitsProvider;
        }

        public async Task<IEnumerable<CompactCI>> FindCIsWithName(string CIName, LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold)
        {
            // TODO: performance improvements, TODO: use ciModel.getCINames() instead?
            var ciNamesFromNameAttributes = await attributeModel.FindMergedAttributesByFullName(ICIModel.NameAttribute, new AllCIIDsSelection(), layerSet, trans, timeThreshold);
            var foundCIIDs = ciNamesFromNameAttributes.Where(a => a.Value.Attribute.Value.Value2String().Equals(CIName)).Select(a => a.Key).ToHashSet();
            if (foundCIIDs.IsEmpty()) return ImmutableArray<CompactCI>.Empty;
            var cis = await ciModel.GetCompactCIs(SpecificCIIDsSelection.Build(foundCIIDs), layerSet, trans, timeThreshold);
            return cis;
        }

        public async Task<IEnumerable<CompactCI>> SimpleSearch(string searchString, IModelContext trans, TimeThreshold atTime)
        {
            var finalSS = searchString.Trim();

            var layers = await layerModel.GetLayers(trans); // TODO: this is not a proper ordering, this can produce ANY ordering
            var ls = await layerModel.BuildLayerSet(layers.Select(l => l.Name).ToArray(), trans);

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

        public async Task<IEnumerable<CompactCI>> AdvancedSearch(string searchString, string[] withEffectiveTraits, LayerSet layerSet, IModelContext trans, TimeThreshold atTime)
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

            var activeTraitSet = await traitsProvider.GetActiveTraitSet(trans, atTime);
            var selectedTraits = activeTraitSet.Traits.Values.Where(t => withEffectiveTraits.Contains(t.Name));
            if (selectedTraits.Count() < withEffectiveTraits.Length)
            {
                // we could not find all traits
                return ImmutableArray<CompactCI>.Empty;
            }
            if (foundCIIDs.IsEmpty())
                return ImmutableArray<CompactCI>.Empty;

            var resultIsReducedByETs = false;
            foreach (var et in selectedTraits)
            {
                ICIIDSelection ciidSelection = new AllCIIDsSelection();
                if (searchAllCIsBasedOnSearchString && !resultIsReducedByETs) 
                    ciidSelection = SpecificCIIDsSelection.Build(foundCIIDs);
                // TODO: replace with something less performance intensive, that only fetches the CIIDs (and also cached)
                var cisFulfillingTraitRequirement = await traitModel.GetMergedCIsWithTrait(et, layerSet, ciidSelection, trans, atTime);
                foundCIIDs = cisFulfillingTraitRequirement.Select(ci => ci.ID).ToHashSet(); // reduce the number of cis to the ones that fulfill this trait requirement
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
