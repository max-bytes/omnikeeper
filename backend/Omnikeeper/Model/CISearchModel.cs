using Microsoft.Extensions.Logging;
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
        private readonly ILogger<CISearchModel> logger;

        public CISearchModel(IAttributeModel attributeModel, ICIModel ciModel, IEffectiveTraitModel traitModel, ILayerModel layerModel, ITraitsProvider traitsProvider, ILogger<CISearchModel> logger)
        {
            this.attributeModel = attributeModel;
            this.ciModel = ciModel;
            this.traitModel = traitModel;
            this.layerModel = layerModel;
            this.traitsProvider = traitsProvider;
            this.logger = logger;
        }

        public async Task<IEnumerable<CompactCI>> FindCIsWithName(string CIName, LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold)
        {
            // TODO: performance improvements
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
                // TODO: performance improvements
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

        public async Task<IEnumerable<CompactCI>> AdvancedSearch(string searchString, string[] withEffectiveTraits, string[] withoutEffectiveTraits, LayerSet layerSet, IModelContext trans, TimeThreshold atTime)
        {
            var finalSS = searchString.Trim();
            ICIIDSelection ciSelection;

            if (Guid.TryParse(finalSS, out var guid))
            {
                if (await ciModel.CIIDExists(guid, trans))
                    ciSelection = SpecificCIIDsSelection.Build(guid);
                else
                    return ImmutableArray<CompactCI>.Empty;
            }
            else if (finalSS.Length > 0)
            {
                var ciNamesFromNameAttributes = await attributeModel.FindMergedAttributesByFullName(ICIModel.NameAttribute, new AllCIIDsSelection(), layerSet, trans, atTime);
                var foundCIIDs = ciNamesFromNameAttributes.Where(kv =>
                {
                    if (kv.Value.Attribute.Value is IAttributeValueText t)
                    {
                        return t.FullTextSearch(finalSS, System.Globalization.CompareOptions.IgnoreCase);
                    }
                    return false;
                }).Select(kv => kv.Key).ToHashSet();
                if (foundCIIDs.IsEmpty())
                    return ImmutableArray<CompactCI>.Empty;
                ciSelection = SpecificCIIDsSelection.Build(foundCIIDs);
            }
            else
            {
                 ciSelection = new AllCIIDsSelection();
            }

            if (!withEffectiveTraits.IsEmpty() || !withoutEffectiveTraits.IsEmpty())
            {
                var activeTraitSet = await traitsProvider.GetActiveTraitSet(trans, atTime);
                var requiredTraits = activeTraitSet.Traits.Values.Where(t => withEffectiveTraits.Contains(t.Name));
                var requiredNonTraits = activeTraitSet.Traits.Values.Where(t => withoutEffectiveTraits.Contains(t.Name));
                var mergedCIs = await ciModel.GetMergedCIs(ciSelection, layerSet, false, trans, atTime);

                foreach (var et in requiredTraits)
                {
                    var reduced = new List<MergedCI>();
                    foreach(var ci in mergedCIs)
                    {
                        if (await traitModel.DoesCIHaveTrait(ci, et, trans, atTime))
                            reduced.Add(ci);
                    }
                    mergedCIs = reduced;
                }
                foreach (var et in requiredNonTraits)
                {
                    var reduced = new List<MergedCI>();
                    foreach (var ci in mergedCIs)
                    {
                        if (!(await traitModel.DoesCIHaveTrait(ci, et, trans, atTime)))
                            reduced.Add(ci);
                    }
                    mergedCIs = reduced;
                }

                if (mergedCIs.IsEmpty())
                    return ImmutableArray<CompactCI>.Empty;
                ciSelection = SpecificCIIDsSelection.Build(mergedCIs.Select(ci => ci.ID));
            }

            var cis = await ciModel.GetCompactCIs(ciSelection, layerSet, trans, atTime);

            // HACK, properly sort unnamed CIs
            return cis.OrderBy(t => t.Name ?? "ZZZZZZZZZZZ");
        }
    }
}
