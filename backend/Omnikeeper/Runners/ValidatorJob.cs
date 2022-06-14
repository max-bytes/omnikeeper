using Autofac;
using Autofac.Features.Indexed;
using Microsoft.Extensions.Logging;
using Omnikeeper.Base.CLB;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Service;
using Quartz;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Omnikeeper.Runners
{
    [DisallowConcurrentExecution]
    public class ValidatorJob : IJob
    {
        public ValidatorJob(ILogger<ValidatorJob> baseLogger, ValidatorContextV1Model validatorContextModel, IMetaConfigurationModel metaConfigurationModel, IIndex<string, IScheduler> schedulers,
            IModelContextBuilder modelContextBuilder)
        {
            this.metaConfigurationModel = metaConfigurationModel;
            this.scheduler = schedulers["localScheduler"];
            this.modelContextBuilder = modelContextBuilder;
            this.baseLogger = baseLogger;
            this.validatorContextModel = validatorContextModel;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                baseLogger.LogTrace("Start");

                var trans = modelContextBuilder.BuildImmediate();
                var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(trans);
                var validationContexts = await validatorContextModel.GetByDataID(AllCIIDsSelection.Instance, metaConfiguration.ConfigLayerset, trans, TimeThreshold.BuildLatest());

                foreach (var kv in validationContexts)
                {
                    try
                    {
                        await TriggerValidation(kv.Value);
                    } 
                    catch (Exception ex)
                    {
                        baseLogger.LogError(ex, $"Could not schedule single Validator Job {kv.Key}");
                    }
                }

                baseLogger.LogTrace("Finished");
            }
            catch (Exception e)
            {
                baseLogger.LogError(e, "Error running validator job");
            }
        }

        private async Task TriggerValidation(ValidatorContextV1 context)
        {
            var jobKey = new JobKey($"job@{context.ID}", "validator");
            var triggerKey = new TriggerKey($"trigger@{context.ID}", "validator");

            // delete job (and triggers) that ran into an error, see https://stackoverflow.com/questions/32273540/recover-from-trigger-error-state-after-job-constructor-threw-an-exception
            if (await scheduler.GetTriggerState(triggerKey) == TriggerState.Error)
                await scheduler.DeleteJob(jobKey);

            if (!await scheduler.CheckExists(jobKey))
            { // only trigger if we are not already running a job with that ID (which would mean that the previous run is still running)
                IJobDetail job = JobBuilder.Create<ValidatorSingleJob>().WithIdentity(jobKey).Build();
                job.JobDataMap.Add("context_ID", context.ID);
                job.JobDataMap.Add("context_Config", context.Config.RootElement.GetRawText()); // HACK: we pass the config by string, because Quartz likes those more than objects
                job.JobDataMap.Add("context_ValidatorReference", context.ValidatorReference);

                ITrigger trigger = TriggerBuilder.Create().WithIdentity(triggerKey).StartNow().Build();
                await scheduler.ScheduleJob(job, trigger);
            }
        }

        private readonly IMetaConfigurationModel metaConfigurationModel;
        private readonly IScheduler scheduler;
        private readonly IModelContextBuilder modelContextBuilder;
        private readonly ILogger<ValidatorJob> baseLogger;
        private readonly ValidatorContextV1Model validatorContextModel;
    }

    [DisallowConcurrentExecution]
    public class ValidatorSingleJob : IJob
    {
        private readonly ILifetimeScope lifetimeScope;
        private readonly IChangesetModel changesetModel;
        private readonly ILogger<ValidatorSingleJob> genericLogger;
        private readonly ScopedLifetimeAccessor scopedLifetimeAccessor;
        private readonly IModelContextBuilder modelContextBuilder;
        private readonly ValidatorProcessedChangesetsCache processedChangesetsCache;
        private readonly IIssuePersister issuePersister;
        private readonly ILoggerFactory loggerFactory;
        private readonly IDictionary<string, IValidator> existingValidators;

        public ValidatorSingleJob(IEnumerable<IValidator> existingValidators, ILifetimeScope lifetimeScope, IChangesetModel changesetModel, ILogger<ValidatorSingleJob> genericLogger,
            ScopedLifetimeAccessor scopedLifetimeAccessor, IModelContextBuilder modelContextBuilder, ILoggerFactory loggerFactory, ValidatorProcessedChangesetsCache processedChangesetsCache,
             IIssuePersister issuePersister)
        {
            this.existingValidators = existingValidators.ToDictionary(l => l.Name);
            this.lifetimeScope = lifetimeScope;
            this.changesetModel = changesetModel;
            this.genericLogger = genericLogger;
            this.scopedLifetimeAccessor = scopedLifetimeAccessor;
            this.modelContextBuilder = modelContextBuilder;
            this.loggerFactory = loggerFactory;
            this.processedChangesetsCache = processedChangesetsCache;
            this.issuePersister = issuePersister;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var d = context.MergedJobDataMap;

            var context_ID = d.GetString("context_ID");
            if (context_ID == null)
            {
                genericLogger.LogError("Error passing context_ID through JobDataMap");
                return;
            }
            var context_Config = d.GetString("context_Config");
            if (context_Config == null)
            {
                genericLogger.LogError("Error passing context_Config through JobDataMap");
                return;
            }
            using var config = JsonDocument.Parse(context_Config);
            var context_ValidatorReference = d.GetString("context_ValidatorReference");
            if (context_ValidatorReference == null)
            {
                genericLogger.LogError("Error passing context_ValidatorReference through JobDataMap");
                return;
            }

            var clLogger = loggerFactory.CreateLogger($"Validator_{context_ID}");

            if (!existingValidators.TryGetValue(context_ValidatorReference, out var validator))
            {
                clLogger.LogError($"Could not find validator with name {context_ValidatorReference}");
                return;
            }

            clLogger.LogDebug($"Trying to run {validator.Name}...");

            // NOTE: to avoid race conditions, we use a single timeThreshold and base everything off of this
            var timeThreshold = TimeThreshold.BuildLatest();

            // calculate unprocessed changesets
            var processedChangesets = processedChangesetsCache.TryGetValue(context_ID);
            var unprocessedChangesets = new Dictionary<string, IReadOnlyList<Changeset>?>(); // null value means all changesets
            var latestSeenChangesets = new Dictionary<string, Guid>();
            var dependentLayerIDs = validator.GetDependentLayerIDs(config, clLogger);
            if (dependentLayerIDs.IsEmpty())
                throw new Exception("Validator must have a non-empty set of dependent layer IDs");
            using var trans = modelContextBuilder.BuildImmediate();
            foreach (var dependentLayerID in dependentLayerIDs)
            {
                if (processedChangesets != null && processedChangesets.TryGetValue(dependentLayerID, out var lastProcessedChangesetID))
                {
                    var up = await changesetModel.GetChangesetsAfter(lastProcessedChangesetID, new string[] { dependentLayerID }, trans, timeThreshold);
                    var latestID = up.FirstOrDefault()?.ID ?? lastProcessedChangesetID;
                    unprocessedChangesets.Add(dependentLayerID, up);
                    latestSeenChangesets.Add(dependentLayerID, latestID);
                } else
                {
                    unprocessedChangesets.Add(dependentLayerID, null);
                    var latest = await changesetModel.GetLatestChangesetForLayer(dependentLayerID, trans, timeThreshold);
                    if (latest != null)
                        latestSeenChangesets.Add(dependentLayerID, latest.ID);
                }
            }
            if (unprocessedChangesets.All(kv => kv.Value != null && kv.Value.IsEmpty()))
            {
                clLogger.LogDebug($"Skipping run of validator {context_ID}");
                return; // <- all dependent layers have not changed since last run, we can skip
            } else
            {
                var layersWhereAllChangesetsAreUnprocessed = unprocessedChangesets.Where(kv => kv.Value == null).ToList();
                if (!layersWhereAllChangesetsAreUnprocessed.IsEmpty())
                    clLogger.LogDebug($"Cannot skip run of validator {context_ID} because of unprocessed changesets in layers: {string.Join(",", layersWhereAllChangesetsAreUnprocessed.Select(kv => kv.Key))}");
                var layersWhereSingleChangesetsAreUnprocessed = unprocessedChangesets.Where(kv => kv.Value != null && !kv.Value.IsEmpty()).ToList();
                if (!layersWhereSingleChangesetsAreUnprocessed.IsEmpty())
                    clLogger.LogDebug($"Cannot skip run of validator {context_ID} because of unprocessed changesets: {string.Join(",", layersWhereSingleChangesetsAreUnprocessed.SelectMany(kv => kv.Value!.Select(c => c.ID)))}");
            }

            // create a lifetime scope per validator invocation (similar to a HTTP request lifetime)
            var username = $"__validator.{context_ID}"; // construct username
            await using (var scope = lifetimeScope.BeginLifetimeScope(Autofac.Core.Lifetime.MatchingScopeLifetimeTags.RequestLifetimeScopeTag, builder =>
            {
                // TODO: use proper UserService, not re-use CLB
                builder.RegisterType<CurrentAuthorizedCLBUserService>().As<ICurrentUserService>().WithParameter("username", username).InstancePerLifetimeScope();
            }))
            {
                scopedLifetimeAccessor.SetLifetimeScope(scope);

                try
                {
                    using var transUpsertUser = modelContextBuilder.BuildDeferred();
                    var currentUserService = scope.Resolve<ICurrentUserService>();
                    var user = await currentUserService.GetCurrentUser(transUpsertUser);
                    transUpsertUser.Commit();

                    var issueAccumulator = new IssueAccumulator("Validator", context_ID);

                    clLogger.LogInformation($"Running Validator {context_ID}");
                    var stopWatch = new Stopwatch();
                    stopWatch.Start();
                    var successful = await validator.Run(unprocessedChangesets, config, modelContextBuilder, timeThreshold, clLogger, issueAccumulator);
                    stopWatch.Stop();
                    TimeSpan ts = stopWatch.Elapsed;
                    string elapsedTime = string.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
                    clLogger.LogInformation($"Done in {elapsedTime}; result: {(successful ? "success" : "failure")}");

                    if (successful)
                    {
                        processedChangesetsCache.UpdateCache(context_ID, latestSeenChangesets);
                    }

                    if (!successful)
                    {
                        issueAccumulator.TryAdd("validator_run_result", "", "Run of Validator failed");
                    }

                    using var transUpdateIssues = modelContextBuilder.BuildDeferred();
                    var changesetProxy = new ChangesetProxy(user.InDatabase, timeThreshold, changesetModel);
                    await issuePersister.Persist(issueAccumulator, transUpdateIssues, new DataOriginV1(DataOriginType.ComputeLayer), changesetProxy);
                    transUpdateIssues.Commit();
                }
                finally
                {
                    scopedLifetimeAccessor.ResetLifetimeScope();
                }
            }
        }
    }
}
