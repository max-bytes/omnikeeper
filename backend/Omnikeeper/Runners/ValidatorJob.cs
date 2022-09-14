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
                var timeThreshold = TimeThreshold.BuildLatest();
                var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(trans);
                var validationContexts = await validatorContextModel.GetByCIID(AllCIIDsSelection.Instance, metaConfiguration.ConfigLayerset, trans, timeThreshold);
                var latestChangesToValidationContexts = await validatorContextModel.GetLatestRelevantChangesetPerTraitEntity(AllCIIDsSelection.Instance, false, true, metaConfiguration.ConfigLayerset, trans, timeThreshold);

                foreach (var kv in validationContexts)
                {
                    try
                    {
                        if (!latestChangesToValidationContexts.TryGetValue(kv.Key, out var latestChange))
                            throw new Exception($"Could not find latest change for validation context {kv.Value.ID}");
                        await TriggerValidation(kv.Value, latestChange.Timestamp);
                    } 
                    catch (Exception ex)
                    {
                        baseLogger.LogError(ex, $"Could not schedule single Validator Job {kv.Value.ID}");
                    }
                }

                baseLogger.LogTrace("Finished");
            }
            catch (Exception e)
            {
                baseLogger.LogError(e, "Error running validator job");
            }
        }

        private async Task TriggerValidation(ValidatorContextV1 context, DateTimeOffset contextActuality)
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
                job.JobDataMap.Add("context_ActualityMS", contextActuality.ToUnixTimeMilliseconds());

                ITrigger trigger = TriggerBuilder.Create().WithIdentity(triggerKey).WithPriority(-10).StartNow().Build();
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
        private readonly ValidatorProcessingCache processingCache;
        private readonly IIssuePersister issuePersister;
        private readonly ICalculateUnprocessedChangesetsService calculateUnprocessedChangesetsService;
        private readonly ILoggerFactory loggerFactory;
        private readonly IDictionary<string, IValidator> existingValidators;

        public ValidatorSingleJob(IEnumerable<IValidator> existingValidators, ILifetimeScope lifetimeScope, IChangesetModel changesetModel, ILogger<ValidatorSingleJob> genericLogger,
            ScopedLifetimeAccessor scopedLifetimeAccessor, IModelContextBuilder modelContextBuilder, ILoggerFactory loggerFactory, ValidatorProcessingCache processingCache,
             IIssuePersister issuePersister, ICalculateUnprocessedChangesetsService calculateUnprocessedChangesetsService)
        {
            this.existingValidators = existingValidators.ToDictionary(l => l.Name);
            this.lifetimeScope = lifetimeScope;
            this.changesetModel = changesetModel;
            this.genericLogger = genericLogger;
            this.scopedLifetimeAccessor = scopedLifetimeAccessor;
            this.modelContextBuilder = modelContextBuilder;
            this.loggerFactory = loggerFactory;
            this.processingCache = processingCache;
            this.issuePersister = issuePersister;
            this.calculateUnprocessedChangesetsService = calculateUnprocessedChangesetsService;
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
            DateTimeOffset contextActuality;
            try
            {
                var context_ActualityMS = d.GetLong("context_ActualityMS");
                contextActuality = DateTimeOffset.FromUnixTimeMilliseconds(context_ActualityMS);
            } catch (Exception)
            {
                genericLogger.LogError("Error passing context_LatestChangeMS through JobDataMap");
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

            var dependentLayerIDs = await validator.GetDependentLayerIDs(config, clLogger, modelContextBuilder);
            if (dependentLayerIDs.IsEmpty())
                throw new Exception("Validator must have a non-empty set of dependent layer IDs");

            // calculate unprocessed changesets
            var (processedChangesets, processedContextActuality) = processingCache.TryGetProcessedChangesets(context_ID);
            using var trans = modelContextBuilder.BuildImmediate();
            var (unprocessedChangesets, latestSeenChangesets) = await calculateUnprocessedChangesetsService.CalculateUnprocessedChangesets(processedChangesets, dependentLayerIDs, timeThreshold, trans);
            if (unprocessedChangesets.All(kv => kv.Value != null && kv.Value.IsEmpty()))
            {
                var cannotSkipBecauseOfUpdatedContext = processedContextActuality != null && processedContextActuality < contextActuality;
                if (cannotSkipBecauseOfUpdatedContext)
                {
                    clLogger.LogDebug($"Cannot skip run of validator {context_ID} because context was updated");
                } else
                {
                    clLogger.LogDebug($"Skipping run of validator {context_ID} because no unprocessed changesets exist for dependent layers");
                    return; // <- all dependent layers have not changed since last run, we can skip
                }
            }
            else
            {
                if (clLogger.IsEnabled(LogLevel.Debug))
                {
                    var layersWhereAllChangesetsAreUnprocessed = unprocessedChangesets.Where(kv => kv.Value == null).ToList();
                    if (!layersWhereAllChangesetsAreUnprocessed.IsEmpty())
                        clLogger.LogDebug($"Cannot skip run of validator {context_ID} because of unprocessed changesets in layers: {string.Join(",", layersWhereAllChangesetsAreUnprocessed.Select(kv => kv.Key))}");
                    var layersWhereSingleChangesetsAreUnprocessed = unprocessedChangesets.Where(kv => kv.Value != null && !kv.Value.IsEmpty()).ToList();
                    if (!layersWhereSingleChangesetsAreUnprocessed.IsEmpty())
                        clLogger.LogDebug($"Cannot skip run of validator {context_ID} because of unprocessed changesets: {string.Join(",", layersWhereSingleChangesetsAreUnprocessed.SelectMany(kv => kv.Value!.Select(c => c.ID)))}");
                }
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
                    var t = new StopTimer();
                    var successful = await validator.Run(unprocessedChangesets, config, modelContextBuilder, timeThreshold, clLogger, issueAccumulator);

                    if (successful)
                    {
                        processingCache.UpdateProcessedChangesets(context_ID, latestSeenChangesets, contextActuality);
                    }

                    if (!successful)
                    {
                        issueAccumulator.TryAdd("validator_run_result", "", "Run of Validator failed");
                    }

                    using var transUpdateIssues = modelContextBuilder.BuildDeferred();
                    var changesetProxy = new ChangesetProxy(user.InDatabase, timeThreshold, changesetModel, new DataOriginV1(DataOriginType.ComputeLayer));
                    clLogger.LogInformation($"Run produced {issueAccumulator.Issues.Count} issues in total");
                    await issuePersister.Persist(issueAccumulator, transUpdateIssues, changesetProxy);
                    transUpdateIssues.Commit();

                    t.Stop((ts, elapsedTime) => clLogger.LogInformation($"Done in {elapsedTime}; result: {(successful ? "success" : "failure")}"));
                }
                finally
                {
                    scopedLifetimeAccessor.ResetLifetimeScope();
                }
            }
        }
    }
}
