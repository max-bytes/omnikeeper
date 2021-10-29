using Castle.DynamicProxy;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Model.Decorators
{
    public class UsageTrackingEffectiveTraitModel : IEffectiveTraitModel
    {
        private readonly IEffectiveTraitModel baseModel;
        private readonly ICurrentUserInDatabaseService currentUserService;
        private readonly IModelContextBuilder modelContextBuilder;
        private readonly IUsageTrackingService usageTrackingService;

        public UsageTrackingEffectiveTraitModel(IEffectiveTraitModel baseModel, ICurrentUserInDatabaseService currentUserService, IModelContextBuilder modelContextBuilder, IUsageTrackingService usageTrackingService)
        {
            this.baseModel = baseModel;
            this.currentUserService = currentUserService;
            this.modelContextBuilder = modelContextBuilder;
            this.usageTrackingService = usageTrackingService;
        }

        public async Task<IEnumerable<MergedCI>> FilterCIsWithoutTrait(IEnumerable<MergedCI> cis, ITrait trait, LayerSet layers, IModelContext trans, TimeThreshold atTime)
        {
            await TrackTraitUsage(trait);

            return await baseModel.FilterCIsWithoutTrait(cis, trait, layers, trans, atTime);
        }

        public async Task<IEnumerable<MergedCI>> FilterCIsWithTrait(IEnumerable<MergedCI> cis, ITrait trait, LayerSet layers, IModelContext trans, TimeThreshold atTime)
        {
            await TrackTraitUsage(trait);

            return await baseModel.FilterCIsWithTrait(cis, trait, layers, trans, atTime);
        }

        public async Task<IDictionary<Guid, EffectiveTrait>> GetEffectiveTraitsForTrait(ITrait trait, IEnumerable<MergedCI> cis, LayerSet layerSet, IModelContext trans, TimeThreshold atTime)
        {
            await TrackTraitUsage(trait);

            return await baseModel.GetEffectiveTraitsForTrait(trait, cis, layerSet, trans, atTime);
        }

        private async Task TrackTraitUsage(ITrait trait)
        {
            if (trait.Origin.Type == TraitOriginType.Core)
            { // not interested in recording usage of Core traits
                return;
            }

            var user = await currentUserService.CreateAndGetCurrentUser(modelContextBuilder.BuildImmediate());
            usageTrackingService.TrackUseTrait(trait.ID, user.Username);
        }
    }
}
