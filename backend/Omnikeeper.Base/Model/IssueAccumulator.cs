using Omnikeeper.Base.Entity.Issue;
using Omnikeeper.Base.GraphQL;
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
        string Type { get; }
        string Context { get; }
        IDictionary<(string type, string context, string group, string id), Issue> Issues { get; }
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
        Task<bool> Persist(IIssueAccumulator from, IModelContext trans, IChangesetProxy changesetProxy);
    }

    public class IssuePersister : IIssuePersister
    {
        private readonly GenericTraitEntityModel<Issue, (string type, string context, string group, string id)> model;
        private readonly IMetaConfigurationModel metaConfigurationModel;
        private readonly IDataLoaderService dataLoaderService;

        public IssuePersister(GenericTraitEntityModel<Issue, (string type, string context, string group, string id)> model, IMetaConfigurationModel metaConfigurationModel, IDataLoaderService dataLoaderService)
        {
            this.model = model;
            this.metaConfigurationModel = metaConfigurationModel;
            this.dataLoaderService = dataLoaderService;
        }

        public async Task<bool> Persist(IIssueAccumulator from, IModelContext trans, IChangesetProxy changesetProxy)
        {
            var config = await metaConfigurationModel.GetConfigOrDefault(trans);
            var traitAttributeFilter = new AttributeFilter[]
            {
                new AttributeFilter("okissue.type", AttributeScalarTextFilter.Build(null, from.Type, null)),
                new AttributeFilter("okissue.context", AttributeScalarTextFilter.Build(null, from.Context, null))
            };

            var relevantCISelection = await TraitEntityHelper.GetMatchingCIIDsByAttributeFilters(AllCIIDsSelection.Instance, traitAttributeFilter, config.IssueLayerset, trans, changesetProxy.TimeThreshold, dataLoaderService).GetResultAsync();
            var r = await model.BulkReplace(relevantCISelection, from.Issues, config.IssueLayerset, config.IssueWriteLayer, changesetProxy, trans, MaskHandlingForRemovalApplyNoMask.Instance);
            return r;
        }
    }
}
