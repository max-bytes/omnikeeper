using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Model;
using System;
using System.Collections.Generic;

namespace Omnikeeper.Base.Service
{
    public interface IScopedUsageTracker
    {
        void TrackUseLayer(string layerID);
        void TrackUseTrait(string traitID);
        void TrackUseAuthRole(string authRoleID);
        void TrackUseGenerator(string generatorID);

        void TrackUse(string elementType, string elementName);
    }

    public class ScopedUsageTracker : IScopedUsageTracker, IDisposable
    {
        private readonly ILogger<ScopedUsageTracker> logger;
        private readonly ICurrentUserService currentUserService;
        private readonly IUsageDataAccumulator usageDataAccumulator;
        private readonly ISet<(string elementType, string elementName)> usages = new HashSet<(string elementType, string elementName)>();

        public ScopedUsageTracker(ILogger<ScopedUsageTracker> logger, ICurrentUserService currentUserService, IUsageDataAccumulator usageDataAccumulator)
        {
            this.logger = logger;
            this.currentUserService = currentUserService;
            this.usageDataAccumulator = usageDataAccumulator;
        }

        public const string ElementTypeTrait = "trait";
        public const string ElementTypeAuthRole = "auth-role";
        public const string ElementTypeLayer = "layer";
        public const string ElementTypeGenerator = "generator";

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

        public void TrackUseGenerator(string generatorID)
        {
            TrackUse(ElementTypeGenerator, generatorID);
        }

        public void TrackUse(string elementType, string elementName)
        {
            usages.Add((elementType, elementName));
        }

        public void Dispose()
        {
            if (usages.Count > 0)
            {
                var username = currentUserService.GetCurrentUsername();

                foreach (var (elementType, elementName) in usages)
                {
                    logger.LogTrace($"Usage tracked: type: {elementType}, name: {elementName}, user: {username}");
                }

                var timestamp = DateTimeOffset.Now;
                usageDataAccumulator.Add(username, timestamp, usages);
            }
        }
    }
}
