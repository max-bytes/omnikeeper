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
                var mergedCIs = await SearchForMergedCIsByTraits(ciSelection, NamedAttributesSelection.Build(ICIModel.NameAttribute), withEffectiveTraits, withoutEffectiveTraits, layerSet, trans, atTime);

                return mergedCIs.Select(ci => CompactCI.BuildFromMergedCI(ci));
            }
            else
            {
                var cis = await ciModel.GetCompactCIs(ciSelection, layerSet, trans, atTime);
                return cis;
            }
        }

        // NOTE: the attributeSelection is supposed to determine what gets RETURNED, not what attributes are checked against when testing for trait memberships
        // NOTE: for internal reasons, this method may return more attributes than requested (when attributeSelection != All, because it needs to check for traits it fetches more attributes)
        public async Task<IEnumerable<MergedCI>> SearchForMergedCIsByTraits(ICIIDSelection ciidSelection, IAttributeSelection attributeSelection, string[] withEffectiveTraits, string[] withoutEffectiveTraits, LayerSet layerSet, IModelContext trans, TimeThreshold atTime)
        {
            var activeTraits = await traitsProvider.GetActiveTraits(trans, atTime);

            IEnumerable<ITrait> requiredTraits = activeTraits.Values.Where(t => withEffectiveTraits.Contains(t.ID)).ToList();
            IEnumerable<ITrait> requiredNonTraits = activeTraits.Values.Where(t => withoutEffectiveTraits.Contains(t.ID)).ToList();
            if (requiredTraits.Count() < withEffectiveTraits.Length)
                throw new Exception($"Encountered unknown trait(s): {string.Join(",", withEffectiveTraits.Except(requiredTraits.Select(t => t.ID)))}");
            if (requiredNonTraits.Count() < withoutEffectiveTraits.Length)
                throw new Exception($"Encountered unknown trait(s): {string.Join(",", withoutEffectiveTraits.Except(requiredNonTraits.Select(t => t.ID)))}");

            if (ReduceTraitRequirements(ref requiredTraits, ref requiredNonTraits, out var emptyTraitIsRequired, out var emptyTraitIsNonRequired))
                return ImmutableList<MergedCI>.Empty; // bail completely

            // special case: empty trait is required
            if (emptyTraitIsRequired)
            {
                // TODO: better performance possible if we get empty CIIDs and exclude those?
                var nonEmptyCIIDs = await attributeModel.GetCIIDsWithAttributes(ciidSelection,layerSet.LayerIDs, trans, atTime);
                var emptyCIIDSelection = ciidSelection.Except(SpecificCIIDsSelection.Build(nonEmptyCIIDs));
                var emptyCIIDs = await emptyCIIDSelection.GetCIIDsAsync(async () => await ciModel.GetCIIDs(trans));
                return emptyCIIDs.Select(ciid => new MergedCI(ciid, null, layerSet, atTime, ImmutableDictionary<string, MergedCIAttribute>.Empty));
            }

            // reduce attribute selection, where possible
            var relevantAttributesForTraits = requiredTraits.SelectMany(t => t.RequiredAttributes.Select(ra => ra.AttributeTemplate.Name)).Concat(
                requiredNonTraits.SelectMany(t => t.RequiredAttributes.Select(ra => ra.AttributeTemplate.Name))
                ).ToHashSet();

            var finalAttributeSelection = attributeSelection switch
            {
                AllAttributeSelection _ => attributeSelection, // keep All
                NamedAttributesSelection n => NamedAttributesSelection.Build(relevantAttributesForTraits.Union(n.AttributeNames).ToHashSet()), // union
                RegexAttributeSelection r => throw new NotImplementedException(),
                _ => throw new NotImplementedException(),
            };

            // special case: no traits are required or non-required, except maybe the empty trait
            if (requiredTraits.IsEmpty() && requiredNonTraits.IsEmpty())
            {
                if (emptyTraitIsNonRequired)
                {
                    // TODO: better performance possible if we get empty CIIDs and exclude those?
                    var nonEmptyCIIDs = await attributeModel.GetCIIDsWithAttributes(ciidSelection, layerSet.LayerIDs, trans, atTime);
                    return await ciModel.GetMergedCIs(SpecificCIIDsSelection.Build(nonEmptyCIIDs), layerSet, includeEmptyCIs: true, finalAttributeSelection, trans, atTime);
                } else
                {
                    return await ciModel.GetMergedCIs(ciidSelection, layerSet, includeEmptyCIs: true, finalAttributeSelection, trans, atTime);
                }
            }

            var workCIs = await ciModel.GetMergedCIs(ciidSelection, layerSet, includeEmptyCIs: !emptyTraitIsNonRequired || requiredTraits.IsEmpty(), finalAttributeSelection, trans, atTime);
            foreach (var requiredTrait in requiredTraits)
            {
                workCIs = await traitModel.FilterCIsWithTrait(workCIs, requiredTrait, layerSet, trans, atTime);
            }

            foreach (var requiredNonTrait in requiredNonTraits)
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

            return workCIs;
        }


        private bool ReduceTraitRequirements(ref IEnumerable<ITrait> requiredTraits, ref IEnumerable<ITrait> requiredNonTraits, out bool emptyTraitIsRequired, out bool emptyTraitIsNonRequired)
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
                        emptyTraitIsRequired = false;
                        emptyTraitIsNonRequired = false;
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

            // handle empty trait special: if its required, checking other traits makes no sense and we can remove checking for other traits, both required and non-required and return with the corresponding flag set
            // if its non-required, we remove it and return with the corresponding flag set
            var requiredEmptyTrait = requiredTraits.FirstOrDefault(t => t.ID == TraitEmpty.StaticID);
            var requiredNonEmptyTrait = requiredNonTraits.FirstOrDefault(t => t.ID == TraitEmpty.StaticID);
            if (requiredEmptyTrait != null)
            {
                var areOtherTraitsRequired = requiredTraits.Count() > 1;
                requiredTraits = new List<ITrait>() { };
                requiredNonTraits = new List<ITrait>();
                emptyTraitIsRequired = true;
                emptyTraitIsNonRequired = false;
                return areOtherTraitsRequired; // if the empty trait is required AND other traits are required -> bail, impossible to produce any CIs
            } else if (requiredNonEmptyTrait != null)
            {
                emptyTraitIsRequired = false;
                emptyTraitIsNonRequired = true;
                requiredNonTraits = requiredNonTraits.Where(t => t.ID != TraitEmpty.StaticID);
                return false;
            }

            // NOTE: depending on which traits are required and non-required, checking them in different orders can have a big impact on performance
            // it makes sense to check for traits that reduce the working set the most first, because then later checks have it easier;
            // consider developing a heuristic for checking which traits reduce the working set the most and check for those first
            // this goes for both required and non-required traits
            // the heuristic we choose for now... length of the trait's ID
            // this is a really weird heuristic at first but it makes some sense given that the shorter a trait's ID is, the more likely it is very broad and generic
            emptyTraitIsRequired = false;
            emptyTraitIsNonRequired = false;
            requiredTraits = requiredTraits.OrderByDescending(t => t.ID.Length);
            requiredNonTraits = requiredNonTraits.OrderByDescending(t => t.ID.Length);

            return false;
        }

    }
}
