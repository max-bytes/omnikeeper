using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Model
{
    public class CISearchModel : ICISearchModel
    {
        private readonly IAttributeModel attributeModel;
        private readonly ICIModel ciModel;
        private readonly IEffectiveTraitModel traitModel;
        private readonly ITraitsProvider traitsProvider;
        private readonly ILogger<CISearchModel> logger;

        public CISearchModel(IAttributeModel attributeModel, ICIModel ciModel, IEffectiveTraitModel traitModel, ITraitsProvider traitsProvider, ILogger<CISearchModel> logger)
        {
            this.attributeModel = attributeModel;
            this.ciModel = ciModel;
            this.traitModel = traitModel;
            this.traitsProvider = traitsProvider;
            this.logger = logger;
        }

        public async Task<IEnumerable<CompactCI>> FindCompactCIsWithName(string CIName, LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold)
        {
            // TODO: performance improvements
            var ciNamesFromNameAttributes = await attributeModel.GetMergedCINames(new AllCIIDsSelection(), layerSet, trans, timeThreshold);
            var foundCIIDs = ciNamesFromNameAttributes.Where(a => a.Value.Equals(CIName)).Select(a => a.Key).ToHashSet();
            if (foundCIIDs.IsEmpty()) return ImmutableArray<CompactCI>.Empty;
            var cis = await ciModel.GetCompactCIs(SpecificCIIDsSelection.Build(foundCIIDs), layerSet, trans, timeThreshold);
            return cis;
        }

        public async Task<IEnumerable<CompactCI>> AdvancedSearchForCompactCIs(string searchString, string[] withEffectiveTraits, string[] withoutEffectiveTraits, LayerSet layerSet, IModelContext trans, TimeThreshold atTime)
        {
            var cis = await _AdvancedSearchForCompactCIs(searchString, withEffectiveTraits, withoutEffectiveTraits, layerSet, trans, atTime);

            // HACK, properly sort unnamed CIs
            return cis.OrderBy(t => t.Name ?? "ZZZZZZZZZZZ");
        }

        private async Task<IEnumerable<CompactCI>> _AdvancedSearchForCompactCIs(string searchString, string[] withEffectiveTraits, string[] withoutEffectiveTraits, LayerSet layerSet, IModelContext trans, TimeThreshold atTime)
        {
            var finalSS = searchString.Trim();
            ICIIDSelection ciSelection;

            if (Guid.TryParse(finalSS, out var guid))
            {
                if (await ciModel.CIIDExists(guid, trans))
                    ciSelection = SpecificCIIDsSelection.Build(guid);
                else
                    return new CompactCI[0];
            }
            else if (finalSS.Length > 0)
            {
                var ciNames = await attributeModel.GetMergedCINames(new AllCIIDsSelection(), layerSet, trans, atTime);
                var foundCIIDs = ciNames.Where(kv =>
                {
                    return CultureInfo.InvariantCulture.CompareInfo.IndexOf(kv.Value, searchString, CompareOptions.IgnoreCase) >= 0;
                }).Select(kv => kv.Key).ToHashSet();
                if (foundCIIDs.IsEmpty())
                    return new CompactCI[0];
                ciSelection = SpecificCIIDsSelection.Build(foundCIIDs);
            }
            else
            {
                ciSelection = new AllCIIDsSelection();
            }

            if (!withEffectiveTraits.IsEmpty() || !withoutEffectiveTraits.IsEmpty())
            {
                var mergedCIs = await SearchForMergedCIsByTraits(ciSelection, withEffectiveTraits, withoutEffectiveTraits, layerSet, trans, atTime);

                return mergedCIs.Select(ci => CompactCI.BuildFromMergedCI(ci));
            }
            else
            {
                var cis = await ciModel.GetCompactCIs(ciSelection, layerSet, trans, atTime);
                return cis;
            }
        }

        // TODO: most (all?) users of this method only require CompactCIs anway... we could make this potentially more performant if we could work with CompactCIs instead
        public async Task<IEnumerable<MergedCI>> SearchForMergedCIsByTraits(ICIIDSelection ciidSelection, string[] withEffectiveTraits, string[] withoutEffectiveTraits, LayerSet layerSet, IModelContext trans, TimeThreshold atTime)
        {
            var activeTraits = await traitsProvider.GetActiveTraits(trans, atTime);
            //var requiredTraits = withEffectiveTraits.Select(et => activeTraits.GetOrWithClass(et, null)).Where(at => at != null);

            IEnumerable<ITrait> requiredTraits = activeTraits.Values.Where(t => withEffectiveTraits.Contains(t.ID)).ToList();
            IEnumerable<ITrait> requiredNonTraits = activeTraits.Values.Where(t => withoutEffectiveTraits.Contains(t.ID)).ToList();
            if (requiredTraits.Count() < withEffectiveTraits.Length)
                throw new Exception($"Encountered unknown trait(s): {string.Join(",", withEffectiveTraits.Except(requiredTraits.Select(t => t.ID)))}");
            if (requiredNonTraits.Count() < withoutEffectiveTraits.Length)
                throw new Exception($"Encountered unknown trait(s): {string.Join(",", withoutEffectiveTraits.Except(requiredNonTraits.Select(t => t.ID)))}");

            if (ReduceTraitRequirements(ref requiredTraits, ref requiredNonTraits))
                return ImmutableList<MergedCI>.Empty; // bail completely

            IEnumerable<MergedCI>? workCIs = null;
            foreach (var requiredTrait in requiredTraits)
            {
                if (workCIs == null)
                {
                    workCIs = await traitModel.GetMergedCIsWithTrait(requiredTrait, layerSet, ciidSelection, trans, atTime);
                }
                else
                {
                    workCIs = await traitModel.FilterCIsWithTrait(workCIs, requiredTrait, layerSet, trans, atTime);
                }
            }

            foreach (var requiredNonTrait in requiredNonTraits)
            {
                if (workCIs == null)
                {
                    if (requiredNonTrait.ID == TraitEmpty.StaticID)
                    {
                        // treat empty trait special, because its simply GetMergedCIs with includeEmptyCIs: false
                        workCIs = await ciModel.GetMergedCIs(ciidSelection, layerSet, includeEmptyCIs: false, trans, atTime);
                    }
                    else
                    {
                        // can't optimize this case well to use cache:
                        // at first, we fetch the mergedCIs with the first requiredNonTrait
                        // then we "invert" the ciid-selection and get the mergedCIs for that selection
                        var excludedCIs = await traitModel.GetMergedCIsWithTrait(requiredNonTrait, layerSet, ciidSelection, trans, atTime);
                        // TODO: implement traitModel.GetMergedCIIDsWithTrait() and use that -> that would allow us to use the cache (if present) and hit the database less
                        // we only need the CIIDs anyway here

                        var workCIIDSelection = ciidSelection.Except(SpecificCIIDsSelection.Build(excludedCIs.Select(ci => ci.ID).ToHashSet()));
                        // NOTE: we must keep includeEmptyCIs true here
                        var includeEmptyCIs = true;
                        workCIs = await ciModel.GetMergedCIs(workCIIDSelection, layerSet, includeEmptyCIs, trans, atTime); 
                    }
                }
                else
                {

                    var cisToFilterOut = await traitModel.FilterCIsWithTrait(workCIs, requiredNonTrait, layerSet, trans, atTime);

                    // HACK: this relies on the order of cisToFilterOut to be the same as the passed in workCIs
                    var reduced = new List<MergedCI>();
                    foreach (var ci in workCIs)
                    {
                        var ciToFilterOut = cisToFilterOut.FirstOrDefault();
                        if (ciToFilterOut == null)
                            reduced.Add(ci);
                        else if (ciToFilterOut != ci)
                            reduced.Add(ci);
                        else
                            cisToFilterOut = cisToFilterOut.Skip(1);
                    }
                    workCIs = reduced;
                }
            }

            return workCIs ?? await ciModel.GetMergedCIs(ciidSelection, layerSet, true, trans, atTime);
        }


        private bool ReduceTraitRequirements(ref IEnumerable<ITrait> requiredTraits, ref IEnumerable<ITrait> requiredNonTraits)
        {
            // reduce/prefilter traits by their dependencies. For example: when trait host is forbidden, but trait host_linux is required, we can bail as that can not produce anything
            // second example: trait host is required AND trait host_linux is required, we can skip checking trait host because host_linux checks that anyway
            var filteredRequiredTraits = new HashSet<string>();
            var filteredRequiredNonTraits = new HashSet<string>();
            foreach (var rt in requiredTraits)
            {
                foreach (var pt in rt.AncestorTraits)
                {
                    if (requiredNonTraits.Any(rn2 => rn2.ID.Equals(pt))) // a parent trait is a non-required trait -> bail completely
                    {
                        return true;
                    }
                    if (requiredTraits.Any(rt2 => rt2.ID.Equals(pt))) // a parent trait is also a required trait, remove parent from requiredTraits
                    {
                        filteredRequiredTraits.Add(pt);
                    }
                }
            }
            foreach (var rt in requiredNonTraits)
            {
                foreach (var pt in rt.AncestorTraits)
                {
                    if (requiredNonTraits.Any(rt2 => rt2.ID.Equals(pt))) // a parent trait is also a non-required trait, remove from nonRequiredTraits
                    {
                        filteredRequiredNonTraits.Add(pt);
                    }
                }
            }
            requiredTraits = requiredTraits.Where(rt => !filteredRequiredTraits.Contains(rt.ID));
            requiredNonTraits = requiredNonTraits.Where(rt => !filteredRequiredNonTraits.Contains(rt.ID));

            // handle empty trait special: if its required, checking other traits makes no sense and we can remove checking for other traits, both required and non-required
            // if its non-required, and there are other traits that are required, we can remove it from the non-required traits
            // if its non-required and there are no other traits that are required, we put it last in the non-required traits, so that the code that does the resolving has an easier time
            var requiredEmptyTrait = requiredTraits.FirstOrDefault(t => t.ID == TraitEmpty.StaticID);
            var requiredNonEmptyTrait = requiredNonTraits.FirstOrDefault(t => t.ID == TraitEmpty.StaticID);
            if (requiredEmptyTrait != null)
            {
                requiredTraits = new List<ITrait>() { requiredEmptyTrait };
                requiredNonTraits = new List<ITrait>();
            } else if (requiredNonEmptyTrait != null)
            {
                if (!requiredTraits.IsEmpty())
                {
                    requiredNonTraits = requiredNonTraits.Where(t => t.ID != TraitEmpty.StaticID);
                } 
            }

            // NOTE: depending on which traits are required and non-required, checking them in different orders can have a big impact on performance
            // it makes sense to check for traits that reduce the working set the most first, because then later checks have it easier;
            // consider developing a heuristic for checking which traits reduce the working set the most and check for those first
            // this goes for both required and non-required traits
            // the heuristic we choose for now... length of the trait's ID
            // this is a really weird heuristic at first but it makes some sense given that the shorter a trait's ID is, the more likely it is very broad and generic
            // Also, we put the nonrequired empty trait last, if it is set (see above why)
            requiredTraits = requiredTraits.OrderByDescending(t => t.ID.Length);
            requiredNonTraits = requiredNonTraits.OrderByDescending(t => (t.ID == TraitEmpty.StaticID) ? -1 : t.ID.Length);

            return false;
        }

    }
}
