using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Service
{
    public interface IScopedUsageTracker
    {
        void TrackUseLayer(string layerID);
        void TrackUseTrait(string traitID);
        void TrackUseAuthRole(string authRoleID);

        void TrackUse(string elementType, string elementName);
    }

    public class ScopedUsageTracker : IScopedUsageTracker, IDisposable, IAsyncDisposable
    {
        private readonly ILogger<ScopedUsageTracker> logger;
        private readonly ICurrentUserService currentUserService;
        private readonly IModelContextBuilder modelContextBuilder;
        private readonly ISet<(string elementType, string elementName)> usages = new HashSet<(string elementType, string elementName)>();

        public ScopedUsageTracker(ILogger<ScopedUsageTracker> logger, ICurrentUserService currentUserService, IModelContextBuilder modelContextBuilder)
        {
            this.logger = logger;
            this.currentUserService = currentUserService;
            this.modelContextBuilder = modelContextBuilder;
        }

        public const string ElementTypeTrait = "trait";
        public const string ElementTypeAuthRole = "auth-role";
        public const string ElementTypeLayer = "layer";

        public void TrackUseLayer(string layerID)
        {
            TrackUse(ElementTypeLayer, layerID);
        }

        public void TrackUseTrait(string traitID)
        {
            TrackUse(ElementTypeTrait, traitID);
        }

        public void TrackUseAuthRole(string authRoleID)
        {
            TrackUse(ElementTypeAuthRole, authRoleID);
        }

        public void TrackUse(string elementType, string elementName)
        {
            usages.Add((elementType, elementName));
        }

        public void Dispose()
        {
            DisposeAsync().GetAwaiter().GetResult();
        }

        public async ValueTask DisposeAsync()
        {
            if (usages.Count > 0)
            {
                var user = await currentUserService.GetCurrentUser(modelContextBuilder.BuildImmediate());

                foreach (var (elementType, elementName) in usages)
                {
                    // TODO: do more
                    logger.LogTrace($"Usage tracked: type: {elementType}, name: {elementName}, user: {user.Username}");
                }
            }
        }
    }
}
