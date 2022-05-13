using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Entity;
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
        void TrackUseAttribute(string attributeName, string layerID, UsageStatsOperation operation);
        void TrackUseRelationPredicate(string predicateID, string layerID, UsageStatsOperation operation);
    }

    public class ScopedUsageTracker : IScopedUsageTracker, IDisposable
    {
        private readonly ILogger<ScopedUsageTracker> logger;
        private readonly ICurrentUserService currentUserService;
        private readonly IUsageDataAccumulator usageDataAccumulator;
        private readonly ISet<(string elementType, string elementName, string layerID, UsageStatsOperation operation)> usages = new HashSet<(string elementType, string elementName, string layerID, UsageStatsOperation operation)>();

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
            TrackUse(ElementTypeTrait, traitID, layerID, UsageStatsOperation.Use);
        }

        public void TrackUseAuthRole(string authRoleID)
        {
            TrackUse(ElementTypeAuthRole, authRoleID, "", UsageStatsOperation.Use);
        }

        public void TrackUseGenerator(string generatorID, string layerID)
        {
            TrackUse(ElementTypeGenerator, generatorID, layerID, UsageStatsOperation.Use);
        }
        public void TrackUseAttribute(string attributeName, string layerID, UsageStatsOperation operation)
        {
            if (operation != UsageStatsOperation.Write && operation != UsageStatsOperation.Read)
                throw new Exception("Invalid operation for attribute use");
            TrackUse(ElementTypeAttributeName, attributeName, layerID, operation);
        }

        public void TrackUseRelationPredicate(string predicateID, string layerID, UsageStatsOperation operation)
        {
            if (operation != UsageStatsOperation.Write && operation != UsageStatsOperation.Read)
                throw new Exception("Invalid operation for relation-predicate use");
            TrackUse(ElementTypeRelationPredicateID, predicateID, layerID, operation);
        }

        private void TrackUse(string elementType, string elementName, string layerID, UsageStatsOperation operation)
        {
            usages.Add((elementType, elementName, layerID, operation));
        }

        public void Dispose()
        {
            if (usages.Count > 0)
            {
                var username = currentUserService.GetCurrentUsername();

                foreach (var (elementType, elementName, layerID, operation) in usages)
                {
                    logger.LogTrace($"Usage tracked: type: {elementType}, name: {elementName}, user: {username}, layerID: {layerID}, operation: {operation}");
                }

                var timestamp = DateTimeOffset.Now;
                usageDataAccumulator.Add(username, timestamp, usages);
            }
        }
    }
}
