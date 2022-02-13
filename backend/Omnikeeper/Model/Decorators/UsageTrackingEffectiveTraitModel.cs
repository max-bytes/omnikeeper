﻿using Autofac;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Model.Decorators
{
    public class UsageTrackingEffectiveTraitModel : IEffectiveTraitModel
    {
        private readonly IEffectiveTraitModel baseModel;
        private readonly ScopedLifetimeAccessor scopedLifetimeAccessor;

        public UsageTrackingEffectiveTraitModel(IEffectiveTraitModel baseModel, ScopedLifetimeAccessor scopedLifetimeAccessor)
        {
            this.baseModel = baseModel;
            this.scopedLifetimeAccessor = scopedLifetimeAccessor;
        }

        public IEnumerable<MergedCI> FilterCIsWithoutTrait(IEnumerable<MergedCI> cis, ITrait trait, LayerSet layers, IModelContext trans, TimeThreshold atTime)
        {
            TrackTraitUsage(trait);

            return baseModel.FilterCIsWithoutTrait(cis, trait, layers, trans, atTime);
        }

        public IEnumerable<MergedCI> FilterCIsWithTrait(IEnumerable<MergedCI> cis, ITrait trait, LayerSet layers, IModelContext trans, TimeThreshold atTime)
        {
            TrackTraitUsage(trait);

            return baseModel.FilterCIsWithTrait(cis, trait, layers, trans, atTime);
        }

        public IEnumerable<MergedCI> FilterCIsWithTraitSOP(IEnumerable<MergedCI> cis, (ITrait trait, bool negated)[][] traitSOP, LayerSet layers, IModelContext trans, TimeThreshold atTime)
        {
            foreach (var traitP in traitSOP)
                foreach (var (trait, _) in traitP)
                    TrackTraitUsage(trait);

            return baseModel.FilterCIsWithTraitSOP(cis, traitSOP, layers, trans, atTime);
        }

        public async Task<IDictionary<Guid, EffectiveTrait>> GetEffectiveTraitsForTrait(ITrait trait, IEnumerable<MergedCI> cis, LayerSet layerSet, IModelContext trans, TimeThreshold atTime)
        {
            TrackTraitUsage(trait);

            return await baseModel.GetEffectiveTraitsForTrait(trait, cis, layerSet, trans, atTime);
        }

        private void TrackTraitUsage(ITrait trait)
        {
            if (trait.ID.StartsWith("__meta"))
            { // not interested in recording usage of meta traits
                return;
            }

            var usageTracker = scopedLifetimeAccessor.GetLifetimeScope()?.Resolve<IScopedUsageTracker>();
            if (usageTracker != null)
                usageTracker.TrackUseTrait(trait.ID);
        }
    }
}
