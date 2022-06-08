﻿using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Entity.Issue;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface IIssueAccumulator
    {
        bool TryAdd(string group, string ID, string message, params Guid[] affectedCIs);
    }

    public class IssueAccumulator : IIssueAccumulator
    {
        public string Type { get; }
        public string Context { get; }

        public IDictionary<(string type, string context, string group, string id), Issue> Issues { get; }

        public IssueAccumulator(string type, string context)
        {
            Issues = new Dictionary<(string type, string context, string group, string id), Issue>();
            Type = type;
            Context = context;
        }

        public bool TryAdd(string group, string ID, string message, params Guid[] affectedCIs)
        {
            var issue = new Issue(Type, Context, group, ID, message, affectedCIs);
            return Issues.TryAdd((Type, Context, group, issue.ID), issue);
        }
    }

    public interface IIssuePersister
    {
        Task<bool> Persist(IssueAccumulator from, IModelContext trans, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy);
    }

    public class IssuePersister : IIssuePersister
    {
        private readonly GenericTraitEntityModel<Issue, (string type, string context, string group, string id)> model;
        private readonly IAttributeModel attributeModel;
        private readonly IMetaConfigurationModel metaConfigurationModel;

        public IssuePersister(GenericTraitEntityModel<Issue, (string type, string context, string group, string id)> model, IAttributeModel attributeModel, IMetaConfigurationModel metaConfigurationModel)
        {
            this.model = model;
            this.attributeModel = attributeModel;
            this.metaConfigurationModel = metaConfigurationModel;
        }

        public async Task<bool> Persist(IssueAccumulator from, IModelContext trans, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy)
        {
            var config = await metaConfigurationModel.GetConfigOrDefault(trans);
            var traitAttributeFilter = new AttributeFilter[]
            {
                new AttributeFilter("issue.type", AttributeScalarTextFilter.Build(null, from.Type)),
                new AttributeFilter("issue.context", AttributeScalarTextFilter.Build(null, from.Context))
            };

            var matchingCIIDs = await TraitEntityHelper.GetMatchingCIIDsByAttributeFilters(AllCIIDsSelection.Instance, attributeModel, traitAttributeFilter, config.IssueLayerset, trans, changesetProxy.TimeThreshold);
            var ciSelection = SpecificCIIDsSelection.Build(matchingCIIDs);
            var r = await model.BulkReplace(ciSelection, from.Issues, config.IssueLayerset, config.IssueWriteLayer, dataOrigin, changesetProxy, trans, MaskHandlingForRemovalApplyNoMask.Instance);
            return r;
        }
    }
}
