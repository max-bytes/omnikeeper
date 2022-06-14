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

                var trans = modelContextBuilder.BuildImmediate();
                var activeLayers = await layerDataModel.GetLayerData(AnchorStateFilter.ActiveAndDeprecated, trans, TimeThreshold.BuildLatest());
                var layersWithCLBs = activeLayers.Where(l => l.CLConfigID != "");

                if (!layersWithCLBs.IsEmpty())
                {
                    var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(trans);
                    var clConfigs = await clConfigModel.GetByDataID(AllCIIDsSelection.Instance, metaConfiguration.ConfigLayerset, trans, TimeThreshold.BuildLatest());

                    foreach (var l in layersWithCLBs)
                    {
                        // find clConfig for layer
                        if (!clConfigs.TryGetValue(l.CLConfigID, out var clConfig))
                        {
                            baseLogger.LogError($"Could not find cl config with ID {l.CLConfigID}");
                        }
                        else
                        {
                            try
                            {
                                await TriggerCLB(clConfig, l);
                            } 
                            catch (Exception ex)
                            {
                                baseLogger.LogError(ex, $"Could not schedule single CLB Job {clConfig.ID} on layer {l.LayerID}");
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

        private async Task TriggerCLB(CLConfigV1 clConfig, LayerData layerData)
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

                ITrigger trigger = TriggerBuilder.Create().WithIdentity(triggerKey).StartNow().Build();
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
        private readonly CLBProcessedChangesetsCache clbProcessedChangesetsCache;
        private readonly IIssuePersister issuePersister;
        private readonly ILoggerFactory loggerFactory;
        private readonly IDictionary<string, IComputeLayerBrain> existingComputeLayerBrains;

        public CLBSingleJob(IEnumerable<IComputeLayerBrain> existingComputeLayerBrains, ILifetimeScope lifetimeScope, IChangesetModel changesetModel, ILogger<CLBSingleJob> genericLogger,
            ScopedLifetimeAccessor scopedLifetimeAccessor, IModelContextBuilder modelContextBuilder, ILoggerFactory loggerFactory, CLBProcessedChangesetsCache clbProcessedChangesetsCache,
             IIssuePersister issuePersister)
        {
            this.existingComputeLayerBrains = existingComputeLayerBrains.ToDictionary(l => l.Name);
            this.lifetimeScope = lifetimeScope;
            this.changesetModel = changesetModel;
            this.genericLogger = genericLogger;
            this.scopedLifetimeAccessor = scopedLifetimeAccessor;
            this.modelContextBuilder = modelContextBuilder;
            this.loggerFactory = loggerFactory;
            this.clbProcessedChangesetsCache = clbProcessedChangesetsCache;
            this.issuePersister = issuePersister;
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

            var clLogger = loggerFactory.CreateLogger($"CLB_{clConfig_ID}@{layerID}");

            if (!existingComputeLayerBrains.TryGetValue(clConfig_CLBrainReference, out var clb))
            {
                clLogger.LogError($"Could not find compute layer brain with name {clConfig_CLBrainReference}");
                return;
            }

            clLogger.LogDebug($"Trying to run {clb.Name} on layer {layerID}...");

            // NOTE: to avoid race conditions, we use a single timeThreshold and base everything off of this
            var timeThreshold = TimeThreshold.BuildLatest();

            // calculate unprocessed changesets
            var transI = modelContextBuilder.BuildImmediate();
            var processedChangesets = clbProcessedChangesetsCache.TryGetValue(clConfig_ID, layerID);
            transI.Dispose();
            var unprocessedChangesets = new Dictionary<string, IReadOnlyList<Changeset>?>(); // null value means all changesets
            var latestSeenChangesets = new Dictionary<string, Guid>();
            var dependentLayerIDs = clb.GetDependentLayerIDs(layerID, clBrainConfig, clLogger);
            if (dependentLayerIDs != null)
            {
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
            }
            if (!unprocessedChangesets.IsEmpty())
            {
                if (unprocessedChangesets.All(kv => kv.Value != null && kv.Value.IsEmpty()))
                {
                    clLogger.LogDebug($"Skipping run of CLB {clb.Name} on layer {layerID}");
                    return; // <- all dependent layer have not changed since last run, we can skip
                } else
                {
                    var layersWhereAllChangesetsAreUnprocessed = unprocessedChangesets.Where(kv => kv.Value == null).ToList();
                    if (!layersWhereAllChangesetsAreUnprocessed.IsEmpty())
                        clLogger.LogDebug($"Cannot skip run of CLB {clb.Name} on layer {layerID} because of unprocessed changesets in layers: {string.Join(",", layersWhereAllChangesetsAreUnprocessed.Select(kv => kv.Key))}");
                    var layersWhereSingleChangesetsAreUnprocessed = unprocessedChangesets.Where(kv => kv.Value != null && !kv.Value.IsEmpty()).ToList();
                    if (!layersWhereSingleChangesetsAreUnprocessed.IsEmpty())
                        clLogger.LogDebug($"Cannot skip run of CLB {clb.Name} on layer {layerID} because of unprocessed changesets: {string.Join(",", layersWhereAllChangesetsAreUnprocessed.SelectMany(kv => kv.Value.Select(c => c.ID)))}");
                }
            } else
            { // TODO: handle case when layer contains no changesets at all separately -> skip
                clLogger.LogDebug($"Cannot skip run of CLB {clb.Name} on layer {layerID} because CLB does not specify dependent layers");
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

                    var changesetProxy = new ChangesetProxy(user.InDatabase, timeThreshold, changesetModel);

                    var issueAccumulator = new IssueAccumulator("ComputeLayerBrain", $"{clConfig_ID}@{layerID}");

                    clLogger.LogInformation($"Running CLB {clb.Name} on layer {layerID}");
                    var stopWatch = new Stopwatch();
                    stopWatch.Start();
                    var successful = await clb.Run(layerID, unprocessedChangesets, clBrainConfig, changesetProxy, modelContextBuilder, clLogger, issueAccumulator);
                    stopWatch.Stop();
                    TimeSpan ts = stopWatch.Elapsed;
                    string elapsedTime = string.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
                    clLogger.LogInformation($"Done in {elapsedTime}; result: {(successful ? "success" : "failure")}");

                    if (successful)
                    {
                        clbProcessedChangesetsCache.UpdateCache(clConfig_ID, layerID, latestSeenChangesets);
                    }

                    if (!successful)
                    {
                        issueAccumulator.TryAdd("clb_run_result", "", "Run of CLB failed");
                    }

                    using var transUpdateIssues = modelContextBuilder.BuildDeferred();
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
