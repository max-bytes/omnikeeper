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
using static Landscape.Base.Model.IAttributeModel;

namespace LandscapeRegistry.Model
{
    public class CISearchModel : ICISearchModel
    {
        private readonly IAttributeModel attributeModel;
        private readonly ICIModel ciModel;
        private readonly ITraitModel traitModel;

        public CISearchModel(IAttributeModel attributeModel, ICIModel ciModel, ITraitModel traitModel)
        {
            this.attributeModel = attributeModel;
            this.ciModel = ciModel;
            this.traitModel = traitModel;
        }

        public async Task<IEnumerable<CompactCI>> Search(string searchString, string[] withEffectiveTraits, LayerSet layerSet, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            var finalSS = searchString.Trim();
            var foundCIIDs = new HashSet<Guid>();

            if (withEffectiveTraits.Length <= 0) // if no traits are specified, there cannot be any results -> return early
                return ImmutableArray<CompactCI>.Empty;

            var searchAllCIsBasedOnSearchString = true;
            if (finalSS.Length > 0)
            {
                searchAllCIsBasedOnSearchString = false;
                // TODO: performance improvements, TODO: use ciModel.getCINames() instead?
                var ciNamesFromNameAttributes = await attributeModel.FindMergedAttributesByFullName(CIModel.NameAttribute, new AllCIIDsAttributeSelection(), false, layerSet, trans, atTime);
                foundCIIDs = ciNamesFromNameAttributes.Where(kv => kv.Value.Attribute.Value.FullTextSearch(finalSS, System.Globalization.CompareOptions.IgnoreCase))
                    .Select(kv => kv.Key).ToHashSet();
            }
            else if (Guid.TryParse(finalSS, out var guid))
            {
                searchAllCIsBasedOnSearchString = false;
                foundCIIDs = (await ciModel.GetCIIDs(trans)).Where(ciid => ciid.Equals(guid)).ToHashSet(); // TODO: performance improvement
            } else
            {
                foundCIIDs = (await ciModel.GetCIIDs(trans)).ToHashSet();
            }
                
            //if (withEffectiveTraits.Length > 0)
            //{
                foreach(var etName in withEffectiveTraits)
                {
                    var ciFilter = (searchAllCIsBasedOnSearchString) ? (Func<Guid, bool>)null : (ciid) => foundCIIDs.Contains(ciid);
                    var ets = await traitModel.CalculateEffectiveTraitSetsForTraitName(etName, layerSet, trans, atTime, ciFilter);
                    var cisFulfillingTraitRequirement = ets.Select(et => et.UnderlyingCI.ID);
                    foundCIIDs = cisFulfillingTraitRequirement.ToHashSet(); // reduce the number of cis to the ones that fulfill this trait requirement
                }
            //}

            var cis = await ciModel.GetCompactCIs(layerSet, trans, atTime, foundCIIDs);

            // HACK, properly sort unnamed CIs
            return cis.OrderBy(t => t.Name ?? "ZZZZZZZZZZZ").Take(500);
        }
    }
}
