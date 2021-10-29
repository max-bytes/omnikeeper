//using Castle.DynamicProxy;
//using Omnikeeper.Base.Entity;
//using Omnikeeper.Base.Service;
//using Omnikeeper.Base.Utils.ModelContext;
//using System.Linq;

//namespace Omnikeeper.Model.Interceptors
//{
//    public class EffectiveTraitModelUsageTrackingInterceptor : IInterceptor
//    {
//        private readonly ICurrentUserInDatabaseService currentUserService;
//        private readonly IModelContextBuilder modelContextBuilder;
//        private readonly IUsageTrackingService usageTrackingService;

//        public EffectiveTraitModelUsageTrackingInterceptor(ICurrentUserInDatabaseService currentUserService, IModelContextBuilder modelContextBuilder, IUsageTrackingService usageTrackingService)
//        {
//            this.currentUserService = currentUserService;
//            this.modelContextBuilder = modelContextBuilder;
//            this.usageTrackingService = usageTrackingService;
//        }

//        public async void Intercept(IInvocation invocation)
//        {
//            invocation.Proceed();

//            // we simply assume that, when a argument ITrait is present, that this means the trait is "used"
//            var trait = invocation.Arguments.FirstOrDefault(a => a is ITrait) as ITrait;
//            if (trait != null)
//            {
//                if (trait.Origin.Type == TraitOriginType.Core)
//                { // not interested in recording usage of Core traits
//                    return;
//                }

//                var user = await currentUserService.CreateAndGetCurrentUser(modelContextBuilder.BuildImmediate());

//                usageTrackingService.TrackUseTrait(trait.ID, user.Username);
//            }

//        }
//    }
//}
