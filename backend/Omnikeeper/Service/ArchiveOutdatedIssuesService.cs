using Autofac;
using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Entity.Issue;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Service
{
    public class ArchiveOutdatedIssuesService : IArchiveOutdatedIssuesService
    {
        private readonly GenericTraitEntityModel<Issue> model;
        private readonly IMetaConfigurationModel metaConfigurationModel;
        private readonly IEnumerable<IIssueContextSource> issueContextSources;
        private readonly ILifetimeScope parentLifetimeScope;
        private readonly IChangesetModel changesetModel;
        private readonly ScopedLifetimeAccessor scopedLifetimeAccessor;

        public ArchiveOutdatedIssuesService(GenericTraitEntityModel<Issue> model, IMetaConfigurationModel metaConfigurationModel, IEnumerable<IIssueContextSource> issueContextSources, ILifetimeScope parentLifetimeScope, IChangesetModel changesetModel, ScopedLifetimeAccessor scopedLifetimeAccessor)
        {
            this.model = model;
            this.metaConfigurationModel = metaConfigurationModel;
            this.issueContextSources = issueContextSources;
            this.parentLifetimeScope = parentLifetimeScope;
            this.changesetModel = changesetModel;
            this.scopedLifetimeAccessor = scopedLifetimeAccessor;
        }

        public async Task<int> ArchiveOutdatedIssues(IModelContextBuilder modelContextBuilder, ILogger logger)
        {
            using var transI = modelContextBuilder.BuildImmediate();
            var timeThreshold = TimeThreshold.BuildLatest();
            var metaConfig = await metaConfigurationModel.GetConfigOrDefault(transI);

            // collect issue contexts
            var validContexts = new HashSet<(string type, string context)>();
            foreach(var source in issueContextSources)
            {
                var c = await source.GetIssueContexts(transI, timeThreshold);
                foreach (var cc in c)
                    validContexts.Add(cc);
            }

            // get all issues
            var allIssues = await model.GetByCIID(AllCIIDsSelection.Instance, metaConfig.IssueLayerset, transI, timeThreshold);

            var toRemove = new HashSet<Guid>();
            foreach (var kv in allIssues)
            {
                if (!validContexts.Contains((kv.Value.Type, kv.Value.Context)))
                    toRemove.Add(kv.Key);
            }

            if (!toRemove.IsEmpty())
            {
                // remove
                try
                {
                    // create a lifetime scope per invocation (similar to a HTTP request lifetime)
                    await using (var scope = parentLifetimeScope.BeginLifetimeScope(Autofac.Core.Lifetime.MatchingScopeLifetimeTags.RequestLifetimeScopeTag, builder =>
                    {
                        // HACK: re-using user from MarkedForDeletionService
                        builder.RegisterType<CurrentAuthorizedMarkedForDeletionUserService>().As<ICurrentUserService>().InstancePerLifetimeScope();
                    }))
                    {
                        scopedLifetimeAccessor.SetLifetimeScope(scope);

                        try
                        {
                            using var transUpsertUser = modelContextBuilder.BuildDeferred();
                            var currentUserService = scope.Resolve<ICurrentUserService>();
                            var user = await currentUserService.GetCurrentUser(transUpsertUser);
                            transUpsertUser.Commit();

                            var changesetProxy = new ChangesetProxy(user.InDatabase, timeThreshold, changesetModel, new DataOriginV1(DataOriginType.Manual));

                            using (var transD = modelContextBuilder.BuildDeferred())
                            {
                                var success = await model.TryToDelete(SpecificCIIDsSelection.Build(toRemove), metaConfig.IssueLayerset, metaConfig.IssueWriteLayer, changesetProxy, transD, MaskHandlingForRemovalApplyNoMask.Instance);
                                if (success)
                                {
                                    transD.Commit();
                                    return toRemove.Count;
                                } else
                                {
                                    transD.Rollback();
                                    return 0;
                                }
                            }
                        }
                        finally
                        {
                            scopedLifetimeAccessor.ResetLifetimeScope();
                        }
                    }
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error archiving outdated issues");
                    return 0;
                }
            } else
            {
                return 0;
            }
        }
    }
}
