using Hangfire.Dashboard;

namespace Omnikeeper.Startup
{
    // in a docker-based environment, we need a custom authorization filter for the hangfire dashboard because non-localhost access is blocked by default
    public class HangFireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            return true; // TODO: proper auth
        }
    }
}
