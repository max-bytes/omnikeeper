using Microsoft.Extensions.Logging;

namespace Omnikeeper.Base.Service
{
    public interface IUsageTrackingService
    {
        void TrackUseTrait(string elementName, string username);
        void TrackUseAuthRole(string elementName, string username);

        void TrackUse(string elementType, string elementName, string username);
    }

    public class UsageTrackingService : IUsageTrackingService
    {
        private readonly ILogger<UsageTrackingService> logger;

        public UsageTrackingService(ILogger<UsageTrackingService> logger)
        {
            this.logger = logger;
        }

        public const string ElementTypeTrait = "trait";
        public const string ElementTypeAuthRole = "auth-role";

        public void TrackUseTrait(string elementName, string username)
        {
            TrackUse(ElementTypeTrait, elementName, username);
        }

        public void TrackUseAuthRole(string elementName, string username)
        {
            TrackUse(ElementTypeAuthRole, elementName, username);
        }

        public void TrackUse(string elementType, string elementName, string username)
        {
            logger.LogTrace($"Usage tracked: type: {elementType}, name: {elementName}, user: {username}");

            // TODO: do more
        }
    }
}
