using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Model;
using System;
using System.Collections.Generic;

namespace Omnikeeper.Base.Service
{
    public interface IScopedUsageTracker
    {
        void TrackUseTrait(string traitID, string layerID);
        void TrackUseAuthRole(string authRoleID);
        void TrackUseGenerator(string generatorID, string layerID);
        void TrackUseAttribute(string attributeName, string layerID);
        void TrackUseRelationPredicate(string predicateID, string layerID);

        void TrackUse(string elementType, string elementName, string layerID);
    }

    public class ScopedUsageTracker : IScopedUsageTracker, IDisposable
    {
        private readonly ILogger<ScopedUsageTracker> logger;
        private readonly ICurrentUserService currentUserService;
        private readonly IUsageDataAccumulator usageDataAccumulator;
        private readonly ISet<(string elementType, string elementName, string layerID)> usages = new HashSet<(string elementType, string elementName, string layerID)>();

        public ScopedUsageTracker(ILogger<ScopedUsageTracker> logger, ICurrentUserService currentUserService, IUsageDataAccumulator usageDataAccumulator)
        {
            this.logger = logger;
            this.currentUserService = currentUserService;
            this.usageDataAccumulator = usageDataAccumulator;
        }

        public const string ElementTypeTrait = "trait";
        public const string ElementTypeAuthRole = "auth-role";
        public const string ElementTypeGenerator = "generator";
        public const string ElementTypeAttributeName = "attribute";
        public const string ElementTypeRelationPredicateID = "relation-predicate";

        public void TrackUseTrait(string traitID, string layerID)
        {
            TrackUse(ElementTypeTrait, traitID, layerID);
        }

        public void TrackUseAuthRole(string authRoleID)
        {
            TrackUse(ElementTypeAuthRole, authRoleID, "");
        }

        public void TrackUseGenerator(string generatorID, string layerID)
        {
            TrackUse(ElementTypeGenerator, generatorID, layerID);
        }
        public void TrackUseAttribute(string attributeName, string layerID)
        {
            TrackUse(ElementTypeAttributeName, attributeName, layerID);
        }

        public void TrackUseRelationPredicate(string predicateID, string layerID)
        {
            TrackUse(ElementTypeRelationPredicateID, predicateID, layerID);
        }

        public void TrackUse(string elementType, string elementName, string layerID)
        {
            usages.Add((elementType, elementName, layerID));
        }

        public void Dispose()
        {
            if (usages.Count > 0)
            {
                var username = currentUserService.GetCurrentUsername();

                foreach (var (elementType, elementName, layerID) in usages)
                {
                    logger.LogTrace($"Usage tracked: type: {elementType}, name: {elementName}, user: {username}, layerID: {layerID}");
                }

                var timestamp = DateTimeOffset.Now;
                usageDataAccumulator.Add(username, timestamp, usages);
            }
        }
    }
}
