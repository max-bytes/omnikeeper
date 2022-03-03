using Autofac;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Service;

namespace Omnikeeper.Utils.Decorators
{
    public class UsageTrackingAuthRolePermissionChecker : IAuthRolePermissionChecker
    {
        private readonly IAuthRolePermissionChecker @base;
        private readonly ScopedLifetimeAccessor scopedLifetimeAccessor;

        public UsageTrackingAuthRolePermissionChecker(IAuthRolePermissionChecker @base, ScopedLifetimeAccessor scopedLifetimeAccessor)
        {
            this.@base = @base;
            this.scopedLifetimeAccessor = scopedLifetimeAccessor;
        }

        public bool DoesAuthRoleGivePermission(AuthRole ar, string permission)
        {
            var does = @base.DoesAuthRoleGivePermission(ar, permission);

            var usageTracker = scopedLifetimeAccessor.GetLifetimeScope()?.Resolve<IScopedUsageTracker>();
            usageTracker?.TrackUseAuthRole(ar.ID);

            return does;
        }
    }
}
