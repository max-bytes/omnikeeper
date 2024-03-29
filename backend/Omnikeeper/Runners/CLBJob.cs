﻿using Autofac;
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
    public class CLBJob : IJob
    {
        public CLBJob(CLConfigV1Model clConfigModel, ILogger<CLBJob> baseLogger, IMetaConfigurationModel metaConfigurationModel, IIndex<string, IScheduler> schedulers,
            ILayerDataModel layerDataModel, IModelContextBuilder modelContextBuilder)
        {
            this.clConfigModel = clConfigModel;
            this.metaConfigurationModel = metaConfigurationModel;
            this.scheduler = schedulers["localScheduler"];
            this.layerDataModel = layerDataModel;
            this.modelContextBuilder = modelContextBuilder;
            this.baseLogger = baseLogger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                baseLogger.LogTrace("Start");

                var timeThreshold = TimeThreshold.BuildLatest();
                var trans = modelContextBuilder.BuildImmediate();
                var activeLayers = await layerDataModel.GetLayerData(AnchorStateFilter.ActiveAndDeprecated, trans, timeThreshold);
                var layersWithCLBs = activeLayers.Where(l => l.CLConfigID != "");

                if (!layersWithCLBs.IsEmpty())
                {
                    var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(trans);
                    var clConfigs = await clConfigModel.GetByCIID(AllCIIDsSelection.Instance, metaConfiguration.ConfigLayerset, trans, timeThreshold);
                    var clConfigLookup = clConfigs.ToLookup(kv => kv.Value.ID, c => (ciid: c.Key, config: c.Value));
                    var latestChangesToCLConfigs = await clConfigModel.GetLatestRelevantChangesetPerTraitEntity(AllCIIDsSelection.Instance, false, true, metaConfiguration.ConfigLayerset, trans, timeThreshold);

                    foreach (var l in layersWithCLBs)
                    {
                        // find clConfig for layer
                        var foundCLConfigs = clConfigLookup[l.CLConfigID];
                        if (foundCLConfigs.IsEmpty())
                        {
                            baseLogger.LogError($"Could not find cl config with ID {l.CLConfigID}");
                        } else if (foundCLConfigs.Count() > 1)
                        {
                            baseLogger.LogError($"Found more than 1 cl config with ID {l.CLConfigID}");
                        }
                        else
                        {
                            var clConfig = foundCLConfigs.First();
                            try
                            {
                                if (!latestChangesToCLConfigs.TryGetValue(clConfig.ciid, out var latestChange))
                                    throw new Exception($"Could not find latest change for CL config {clConfig.config.ID} on layer {l.LayerID}");
                                await TriggerCLB(clConfig.config, latestChange.Timestamp, l);
                            } 
                            catch (Exception ex)
                            {
                                baseLogger.LogError(ex, $"Could not schedule single CLB Job {clConfig.config.ID} on layer {l.LayerID}");
                            }
                        }
                    }
                }

                baseLogger.LogTrace("Finished");
            }
            catch (Exception e)
            {
                baseLogger.LogError(e, "Error running clb job");
            }
        }

        private async Task TriggerCLB(CLConfigV1 clConfig, DateTimeOffset configActuality, LayerData layerData)
        {
            var jobKey = new JobKey($"{clConfig.ID}@{layerData.LayerID}", "clb");
            var triggerKey = new TriggerKey($"trigger@{clConfig.ID}@{layerData.LayerID}", "clb");

            // delete job (and triggers) that ran into an error, see https://stackoverflow.com/questions/32273540/recover-from-trigger-error-state-after-job-constructor-threw-an-exception
            if (await scheduler.GetTriggerState(triggerKey) == TriggerState.Error)
                await scheduler.DeleteJob(jobKey);

            if (!await scheduler.CheckExists(jobKey))
            { // only trigger if we are not already running a job with that ID (which would mean that the previous run is still running)
                IJobDetail job = JobBuilder.Create<CLBSingleJob>().WithIdentity(jobKey).Build();
                job.JobDataMap.Add("clConfig_ID", clConfig.ID);
                job.JobDataMap.Add("clConfig_CLBrainConfig", clConfig.CLBrainConfig.RootElement.GetRawText()); // HACK: we pass the config by string, because Quartz likes those more than objects
                job.JobDataMap.Add("clConfig_CLBrainReference", clConfig.CLBrainReference);
                job.JobDataMap.Add("layerID", layerData.LayerID);
                job.JobDataMap.Add("config_ActualityMS", configActuality.ToUnixTimeMilliseconds());

                ITrigger trigger = TriggerBuilder.Create().WithIdentity(triggerKey).WithPriority(10).StartNow().Build();
                await scheduler.ScheduleJob(job, trigger);
            }
        }

        private readonly CLConfigV1Model clConfigModel;
        private readonly IMetaConfigurationModel metaConfigurationModel;
        private readonly IScheduler scheduler;
        private readonly ILayerDataModel layerDataModel;
        private readonly IModelContextBuilder modelContextBuilder;
        private readonly ILogger<CLBJob> baseLogger;
    }

    [DisallowConcurrentExecution]
    public class CLBSingleJob : IJob
    {
        private readonly ILifetimeScope lifetimeScope;
        private readonly IChangesetModel changesetModel;
        private readonly ILogger<CLBSingleJob> genericLogger;
        private readonly ScopedLifetimeAccessor scopedLifetimeAccessor;
        private readonly IModelContextBuilder modelContextBuilder;
        private readonly CLBProcessingCache clbProcessedChangesetsCache;
        private readonly IIssuePersister issuePersister;
        private readonly ICalculateUnprocessedChangesetsService calculateUnprocessedChangesetsService;
        private readonly ILoggerFactory loggerFactory;
        private readonly IDictionary<string, IComputeLayerBrain> existingCLBs;
        private readonly ISet<string> existingRCLBs;

        public CLBSingleJob(IEnumerable<IComputeLayerBrain> existingCLBs, IEnumerable<IReactiveCLB> existingRCLBs, ILifetimeScope lifetimeScope, IChangesetModel changesetModel, ILogger<CLBSingleJob> genericLogger,
            ScopedLifetimeAccessor scopedLifetimeAccessor, IModelContextBuilder modelContextBuilder, ILoggerFactory loggerFactory, CLBProcessingCache clbProcessedChangesetsCache,
             IIssuePersister issuePersister, ICalculateUnprocessedChangesetsService calculateUnprocessedChangesetsService)
        {
            this.existingCLBs = existingCLBs.ToDictionary(l => l.Name);
            this.existingRCLBs = existingRCLBs.Select(l => l.Name).ToHashSet();
            this.lifetimeScope = lifetimeScope;
            this.changesetModel = changesetModel;
            this.genericLogger = genericLogger;
            this.scopedLifetimeAccessor = scopedLifetimeAccessor;
            this.modelContextBuilder = modelContextBuilder;
            this.loggerFactory = loggerFactory;
            this.clbProcessedChangesetsCache = clbProcessedChangesetsCache;
            this.issuePersister = issuePersister;
            this.calculateUnprocessedChangesetsService = calculateUnprocessedChangesetsService;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var d = context.MergedJobDataMap;

            var clConfig_ID = d.GetString("clConfig_ID");
            if (clConfig_ID == null)
            {
                genericLogger.LogError("Error passing clConfig_ID through JobDataMap");
                return;
            }
            var clConfig_CLBrainConfig = d.GetString("clConfig_CLBrainConfig");
            if (clConfig_CLBrainConfig == null)
            {
                genericLogger.LogError("Error passing clConfig_CLBrainConfig through JobDataMap");
                return;
            }
            using var clBrainConfig = JsonDocument.Parse(clConfig_CLBrainConfig);
            var clConfig_CLBrainReference = d.GetString("clConfig_CLBrainReference");
            if (clConfig_CLBrainReference == null)
            {
                genericLogger.LogError("Error passing clConfig_CLBrainReference through JobDataMap");
                return;
            }

            var layerID = d.GetString("layerID");
            if (layerID == null)
            {
                genericLogger.LogError("Error passing layerID through JobDataMap");
                return;
            }

            DateTimeOffset configActuality;
            try
            {
                configActuality = DateTimeOffset.FromUnixTimeMilliseconds(d.GetLong("config_ActualityMS"));
            }
            catch (Exception)
            {
                genericLogger.LogError("Error passing config_ActualityMS through JobDataMap");
                return;
            }

            var clLogger = loggerFactory.CreateLogger($"CLB_{clConfig_ID}@{layerID}");

            if (!existingCLBs.TryGetValue(clConfig_CLBrainReference, out var clb))
            {
                if (!existingRCLBs.Contains(clConfig_CLBrainReference)) // NOTE: we test whether the referenced CLB is a reactive CLB and if so, ignore it
                    clLogger.LogError($"Could not find compute layer brain with name {clConfig_CLBrainReference}");
                return;
            }

            clLogger.LogDebug($"Trying to run {clb.Name} on layer {layerID}...");

            // NOTE: to avoid race conditions, we use a single timeThreshold and base everything off of this
            var timeThreshold = TimeThreshold.BuildLatest();

            var dependentLayerIDs = clb.GetDependentLayerIDs(layerID, clBrainConfig, clLogger);

            // calculate unprocessed changesets
            var (processedChangesets, processedConfigActuality) = clbProcessedChangesetsCache.TryGetValue(clConfig_ID, layerID);
            using var trans = modelContextBuilder.BuildImmediate();
            var (unprocessedChangesets, latestSeenChangesets) = await calculateUnprocessedChangesetsService.CalculateUnprocessedChangesets(processedChangesets, dependentLayerIDs, timeThreshold, trans);
            if (unprocessedChangesets.All(kv => kv.Value != null && kv.Value.IsEmpty()))
            {
                var cannotSkipBecauseOfUpdatedConfig = processedConfigActuality != null && processedConfigActuality < configActuality;
                if (cannotSkipBecauseOfUpdatedConfig)
                {
                    clLogger.LogDebug($"Cannot skip run of CLB {clb.Name} on layer {layerID} because CL config was updated");
                }
                else
                {
                    clLogger.LogDebug($"Skipping run of CLB {clb.Name} on layer {layerID} because no unprocessed changesets exist for dependent layers");
                    return; // <- all dependent layers have not changed since last run, we can skip
                }
            }
            else
            {
                if (clLogger.IsEnabled(LogLevel.Debug))
                {
                    var layersWhereAllChangesetsAreUnprocessed = unprocessedChangesets.Where(kv => kv.Value == null).ToList();
                    if (!layersWhereAllChangesetsAreUnprocessed.IsEmpty())
                        clLogger.LogDebug($"Cannot skip run of CLB {clb.Name} on layer {layerID} because of unprocessed changesets in layers: {string.Join(",", layersWhereAllChangesetsAreUnprocessed.Select(kv => kv.Key))}");
                    var layersWhereSingleChangesetsAreUnprocessed = unprocessedChangesets.Where(kv => kv.Value != null && !kv.Value.IsEmpty()).ToList();
                    if (!layersWhereSingleChangesetsAreUnprocessed.IsEmpty())
                        clLogger.LogDebug($"Cannot skip run of CLB {clb.Name} on layer {layerID} because of unprocessed changesets: {string.Join(",", layersWhereSingleChangesetsAreUnprocessed.SelectMany(kv => kv.Value!.Select(c => c.ID)))}");
                }
            }

            // create a lifetime scope per clb invocation (similar to a HTTP request lifetime)
            var username = $"__cl.{clConfig_ID}@{layerID}"; // construct username
            await using (var scope = lifetimeScope.BeginLifetimeScope(Autofac.Core.Lifetime.MatchingScopeLifetimeTags.RequestLifetimeScopeTag, builder =>
            {
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

                    var changesetProxy = new ChangesetProxy(user.InDatabase, timeThreshold, changesetModel, new DataOriginV1(DataOriginType.ComputeLayer));

                    var issueAccumulator = new IssueAccumulator("ComputeLayerBrain", $"{clConfig_ID}@{layerID}");

                    clLogger.LogInformation($"Running CLB {clb.Name} on layer {layerID}");
                    var t = new StopTimer();
                    var successful = await clb.Run(layerID, unprocessedChangesets, clBrainConfig, changesetProxy, modelContextBuilder, clLogger, issueAccumulator);

                    if (successful)
                    {
                        clbProcessedChangesetsCache.UpdateCache(clConfig_ID, layerID, latestSeenChangesets, configActuality);
                    }

                    if (!successful)
                    {
                        issueAccumulator.TryAdd("clb_run_result", "", "Run of CLB failed");
                    }

                    using var transUpdateIssues = modelContextBuilder.BuildDeferred();
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
