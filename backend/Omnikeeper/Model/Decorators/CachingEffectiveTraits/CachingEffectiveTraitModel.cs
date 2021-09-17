using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Model.Decorators.CachingEffectiveTraits
{
    // NOTE: when a trait ITSELF changes (its definition), we need to purge the cache for that trait
    // unfortunately, there is not really a good hook that we can latch onto that fires whenever a trait definition changes
    // workaround: whenever something in the base configuration layer(s) changes, purge everything

    // idea: for traits/layer combinations that include > N % of CIs already, it might make sense not to cache, because it's faster to just get it from the database directly

    // NOTE: newly created CIs should actually also be put into the cache, because otherwise they don't get picked up and are not returned when selecting the empty trait
    // but, because empty CIs are not much use and very likely will be filled with attributes/relations anyway, we don't add them right away

    public class CachingEffectiveTraitModel : IEffectiveTraitModel
    {
        private readonly IEffectiveTraitModel baseModel;
        private readonly ICIModel ciModel;
        private readonly EffectiveTraitCache cache;
        private readonly IOnlineAccessProxy onlineAccessProxy;

        public CachingEffectiveTraitModel(IEffectiveTraitModel baseModel, ICIModel ciModel, EffectiveTraitCache cache, IOnlineAccessProxy onlineAccessProxy)
        {
            this.baseModel = baseModel;
            this.ciModel = ciModel;
            this.cache = cache;
            this.onlineAccessProxy = onlineAccessProxy;
        }

        public async Task<EffectiveTrait?> GetEffectiveTraitForCI(MergedCI ci, ITrait trait, LayerSet layers, IModelContext trans, TimeThreshold atTime)
        {
            // cannot cache well
            return await baseModel.GetEffectiveTraitForCI(ci, trait, layers, trans, atTime);
        }

        public async Task<IEnumerable<EffectiveTrait>> GetEffectiveTraitsForCI(IEnumerable<ITrait> traits, MergedCI ci, LayerSet layers, IModelContext trans, TimeThreshold atTime)
        {
            // cannot cache well
            return await baseModel.GetEffectiveTraitsForCI(traits, ci, layers, trans, atTime);
        }

        public async Task<IEnumerable<MergedCI>> FilterCIsWithTrait(IEnumerable<MergedCI> cis, ITrait trait, LayerSet layers, IModelContext trans, TimeThreshold atTime)
        {
            // cannot cache well
            return await baseModel.FilterCIsWithTrait(cis, trait, layers, trans, atTime);
        }

        // caching of effective traits works like this:
        // the cache dictionary stores a superset of the cis that have a trait
        // changes to cis (attributes, relations) lead to adds to this dictionary, not removes
        // that way, it is guaranteed that querying the EffectiveTraitModel with this ci-selection yields a superset of the required traits
        // afterwards, calculate actual traits and update cache -> remove items that do not in fact have trait, even though they were in cache

        public async Task<IDictionary<Guid, (MergedCI ci, EffectiveTrait et)>> GetEffectiveTraitsForTrait(ITrait trait, LayerSet layerSet, ICIIDSelection ciidSelection, IModelContext trans, TimeThreshold atTime)
        {
            if (!atTime.IsLatest)
            {
                return await baseModel.GetEffectiveTraitsForTrait(trait, layerSet, ciidSelection, trans, atTime);
            }

            if (await onlineAccessProxy.ContainsOnlineInboundLayer(layerSet, trans))
            {
                return await baseModel.GetEffectiveTraitsForTrait(trait, layerSet, ciidSelection, trans, atTime);
            }

            if (cache.GetCIIDsHavingTrait(trait.ID, layerSet, out var ciids))
            { // cache hit
                // do an intersection of incoming ciidselection and ciids in cache, fetch only those
                // because the cache has a superset of all cis that actually have the trait, this is guaranteed to return exactly all relevant cis
                var ciidIntersection = ciidSelection.Intersect(SpecificCIIDsSelection.Build(ciids));

                var intersectedETs = await baseModel.GetEffectiveTraitsForTrait(trait, layerSet, ciidIntersection, trans, atTime);

                // update cache, remove the ciids from cache that do not in fact have the trait
                if (intersectedETs.Count() != await ciidIntersection.CountAsync(async () => await ciModel.GetCIIDs(trans)))
                { // if the returned size is the same as the queried one, we don't have to update our cache at all
                    var notHavingTrait = ciidIntersection.Except(SpecificCIIDsSelection.Build(intersectedETs.Keys.ToHashSet()));
                    var ciidsToRemove = await notHavingTrait.GetCIIDsAsync(async () => await ciModel.GetCIIDs(trans));
                    cache.RemoveCIIDs(ciidsToRemove, trait.ID, layerSet);
                }

                return intersectedETs;
            }
            else
            { // cache miss
                // we fetch with an ciidselection of ALL, so we can properly fill the cache
                var allETs = await baseModel.GetEffectiveTraitsForTrait(trait, layerSet, new AllCIIDsSelection(), trans, atTime);

                // update cache with full set of ciids that fulfill trait
                cache.FullUpdateTrait(trait.ID, layerSet, allETs.Keys.ToHashSet());

                // because we fetch ALL ciids, filter the result again before returning
                return ciidSelection switch
                {
                    AllCIIDsSelection _ => allETs,
                    NoCIIDsSelection _ => new Dictionary<Guid, (MergedCI ci, EffectiveTrait et)>(),
                    _ => allETs.Where(et => ciidSelection.Contains(et.Key)).ToDictionary(kv => kv.Key, kv => kv.Value)
                };
            }
        }

        public async Task<IEnumerable<MergedCI>> GetMergedCIsWithTrait(ITrait trait, LayerSet layerSet, ICIIDSelection ciidSelection, IModelContext trans, TimeThreshold atTime)
        {
            if (!atTime.IsLatest)
            {
                return await baseModel.GetMergedCIsWithTrait(trait, layerSet, ciidSelection, trans, atTime);
            }

            if (await onlineAccessProxy.ContainsOnlineInboundLayer(layerSet, trans))
            {
                return await baseModel.GetMergedCIsWithTrait(trait, layerSet, ciidSelection, trans, atTime);
            }

            if (cache.GetCIIDsHavingTrait(trait.ID, layerSet, out var ciids))
            { // cache hit
                // do an intersection of incoming ciidselection and ciids in cache, fetch only those
                // because the cache has a superset of all cis that actually have the trait, this is guaranteed to return exactly all relevant cis
                var ciidIntersection = ciidSelection.Intersect(SpecificCIIDsSelection.Build(ciids));

                var intersectedMergedCIs = await baseModel.GetMergedCIsWithTrait(trait, layerSet, ciidIntersection, trans, atTime);

                // update cache, remove the ciids from cache that do not in fact have the trait
                if (intersectedMergedCIs.Count() != await ciidIntersection.CountAsync(async () => await ciModel.GetCIIDs(trans)))
                { // if the returned size is the same as the queried one, we don't have to update our cache at all
                    var notHavingTrait = ciidIntersection.Except(SpecificCIIDsSelection.Build(intersectedMergedCIs.Select(t => t.ID).ToHashSet()));
                    var ciidsToRemove = await notHavingTrait.GetCIIDsAsync(async () => await ciModel.GetCIIDs(trans));
                    cache.RemoveCIIDs(ciidsToRemove, trait.ID, layerSet);
                }

                return intersectedMergedCIs;

            }
            else
            { // cache miss
                // we fetch with a ciidselection of ALL, so we can properly fill the cache
                var allCIs = await baseModel.GetMergedCIsWithTrait(trait, layerSet, new AllCIIDsSelection(), trans, atTime);

                // update cache with full set of ciids that fulfill trait
                cache.FullUpdateTrait(trait.ID, layerSet, allCIs.Select(t => t.ID).ToHashSet());

                // because we fetched ALL ciids, filter the result again before returning
                return ciidSelection switch
                {
                    AllCIIDsSelection _ => allCIs,
                    NoCIIDsSelection _ => new MergedCI[0],
                    _ => allCIs.Where(ci => ciidSelection.Contains(ci.ID))
                };
            }
        }

        public async Task<IDictionary<Guid, (MergedCI ci, EffectiveTrait et)>> GetEffectiveTraitsWithTraitAttributeValue(ITrait trait, string traitAttributeIdentifier, IAttributeValue value, LayerSet layerSet, ICIIDSelection ciidSelection, IModelContext trans, TimeThreshold atTime)
        {
            // cannot cache well
            return await baseModel.GetEffectiveTraitsWithTraitAttributeValue(trait, traitAttributeIdentifier, value, layerSet, ciidSelection, trans, atTime);
        }
    }
}
