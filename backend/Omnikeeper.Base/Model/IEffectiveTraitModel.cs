using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface IEffectiveTraitModel
    {
        IEnumerable<MergedCI> FilterCIsWithTraitSOP(IEnumerable<MergedCI> cis, (ITrait trait, bool negated)[][] traitSOP, LayerSet layers);

        Task<IDictionary<Guid, EffectiveTrait>> GetEffectiveTraitsForTrait(ITrait trait, IEnumerable<MergedCI> cis, LayerSet layerSet, IModelContext trans, TimeThreshold atTime);
    }

    public static class EffectiveTraitModelExtensions
    {
        public static IEnumerable<MergedCI> FilterMergedCIsByTraits(this IEffectiveTraitModel model, IEnumerable<MergedCI> cis, IEnumerable<ITrait> withEffectiveTraits, IEnumerable<ITrait> withoutEffectiveTraits, LayerSet layerSet)
        {
            if (withEffectiveTraits.IsEmpty() && withoutEffectiveTraits.IsEmpty())
                return model.FilterCIsWithTraitSOP(cis, Array.Empty<(ITrait trait, bool negated)[]>(), layerSet);
            var traitSOP = new (ITrait trait, bool negated)[][]
            {
                withEffectiveTraits.Select(t => (t, false)).Concat(withoutEffectiveTraits.Select(t => (t, true))).ToArray()
            };
            return model.FilterCIsWithTraitSOP(cis, traitSOP, layerSet);
        }

        public static IEnumerable<MergedCI> FilterCIsWithTrait(this IEffectiveTraitModel model, IEnumerable<MergedCI> cis, ITrait trait, LayerSet layers)
        {
            if (layers.IsEmpty && trait is not TraitEmpty)
                return ImmutableList<MergedCI>.Empty; // return empty, an empty layer list can never produce any traits (except for the empty trait)

            var traitSOP = new (ITrait trait, bool negated)[][] { new (ITrait trait, bool negated)[] {(trait, false)} };
            return model.FilterCIsWithTraitSOP(cis, traitSOP, layers);
        }

        public static bool ReduceTraitRequirements(this IEffectiveTraitModel model, ref IEnumerable<ITrait> requiredTraits, ref IEnumerable<ITrait> requiredNonTraits, out bool emptyTraitIsRequired, out bool emptyTraitIsNonRequired)
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
            }
            else if (requiredNonEmptyTrait != null)
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
