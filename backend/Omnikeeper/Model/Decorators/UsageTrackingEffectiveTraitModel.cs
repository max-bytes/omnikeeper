using Autofac;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Service;
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

        public async Task<IEnumerable<MergedCI>> FilterCIsWithoutTrait(IEnumerable<MergedCI> cis, ITrait trait, LayerSet layers, IModelContext trans, TimeThreshold atTime)
        {
            TrackTraitUsage(trait);

            return await baseModel.FilterCIsWithoutTrait(cis, trait, layers, trans, atTime);
        }

        public async Task<IEnumerable<MergedCI>> FilterCIsWithTrait(IEnumerable<MergedCI> cis, ITrait trait, LayerSet layers, IModelContext trans, TimeThreshold atTime)
        {
            TrackTraitUsage(trait);

            return await baseModel.FilterCIsWithTrait(cis, trait, layers, trans, atTime);
        }

        public async Task<IDictionary<Guid, EffectiveTrait>> GetEffectiveTraitsForTrait(ITrait trait, IEnumerable<MergedCI> cis, LayerSet layerSet, IModelContext trans, TimeThreshold atTime)
        {
            TrackTraitUsage(trait);

            return await baseModel.GetEffectiveTraitsForTrait(trait, cis, layerSet, trans, atTime);
        }

        private void TrackTraitUsage(ITrait trait)
        {
            if (trait.Origin.Type == TraitOriginType.Core)
            { // not interested in recording usage of Core traits
                return;
            }

            var usageTracker = scopedLifetimeAccessor.GetLifetimeScope()?.Resolve<IScopedUsageTracker>();
            if (usageTracker != null)
                usageTracker.TrackUseTrait(trait.ID);
        }
    }
}
