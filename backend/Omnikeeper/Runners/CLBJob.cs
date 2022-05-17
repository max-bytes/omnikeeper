using Autofac;
using Autofac.Features.Indexed;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using Omnikeeper.Base.CLB;
using Omnikeeper.Base.Entity;
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
            this.scheduler = schedulers["distributedScheduler"];
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
                    var clConfigs = await clConfigModel.GetAllByDataID(metaConfiguration.ConfigLayerset, trans, TimeThreshold.BuildLatest());

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
            var jobKey = new JobKey($"{clConfig.ID}@{layerData.LayerID}");

            if (!await scheduler.CheckExists(jobKey))
            { // only trigger if we are not already running a job with that ID (which would mean that the previous run is still running)
                IJobDetail job = JobBuilder.Create<CLBSingleJob>().WithIdentity(jobKey).Build();
                job.JobDataMap.Add("clConfig_ID", clConfig.ID);
                job.JobDataMap.Add("clConfig_CLBrainConfig", clConfig.CLBrainConfig.RootElement.GetRawText()); // HACK: we pass the config by string, because Quartz likes those more than objects
                job.JobDataMap.Add("clConfig_CLBrainReference", clConfig.CLBrainReference);
                job.JobDataMap.Add("layerID", layerData.LayerID);

                ITrigger trigger = TriggerBuilder.Create().StartNow().Build();
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
        private readonly CLBLastRunCache clbLastRunCache;
        private readonly ILoggerFactory loggerFactory;
        private readonly IDictionary<string, IComputeLayerBrain> existingComputeLayerBrains;

        public CLBSingleJob(IEnumerable<IComputeLayerBrain> existingComputeLayerBrains, ILifetimeScope lifetimeScope, IChangesetModel changesetModel, ILogger<CLBSingleJob> genericLogger,
            ScopedLifetimeAccessor scopedLifetimeAccessor, IModelContextBuilder modelContextBuilder, CLBLastRunCache clbLastRunCache, ILoggerFactory loggerFactory)
        {
            this.existingComputeLayerBrains = existingComputeLayerBrains.ToDictionary(l => l.Name);
            this.lifetimeScope = lifetimeScope;
            this.changesetModel = changesetModel;
            this.genericLogger = genericLogger;
            this.scopedLifetimeAccessor = scopedLifetimeAccessor;
            this.modelContextBuilder = modelContextBuilder;
            this.clbLastRunCache = clbLastRunCache;
            this.loggerFactory = loggerFactory;
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

            var lastRunKey = $"{clConfig_ID}{layerID}";
            var transI = modelContextBuilder.BuildImmediate();
            DateTimeOffset? lastRun = await clbLastRunCache.TryGetValue(lastRunKey, transI);
            transI.Dispose();

            if (await clb.CanSkipRun(lastRun, clBrainConfig, clLogger, modelContextBuilder))
            {
                clLogger.LogDebug($"Skipping run of CLB {clb.Name} on layer {layerID}");
                return;
            }

            // create a lifetime scope per clb invocation (similar to a HTTP request lifetime)
            var username = $"__cl.{clConfig_ID}@{layerID}"; // construct username
            await using (var scope = lifetimeScope.BeginLifetimeScope(Autofac.Core.Lifetime.MatchingScopeLifetimeTags.RequestLifetimeScopeTag, builder =>
            {
                builder.Register(builder => new CLBContext(username)).InstancePerLifetimeScope();
                builder.RegisterType<CurrentAuthorizedCLBUserService>().As<ICurrentUserService>().InstancePerLifetimeScope();
            }))
            {
                scopedLifetimeAccessor.SetLifetimeScope(scope);

                try
                {
                    using var transUpsertUser = modelContextBuilder.BuildDeferred();
                    var currentUserService = scope.Resolve<ICurrentUserService>();
                    var user = await currentUserService.GetCurrentUser(transUpsertUser);
                    transUpsertUser.Commit();

                    // TODO: do we not introduce a race-condition here? We use the changeset's proxies time to set our lastRunCache entry
                    // is there a way for a dependent layer to get updated with a timestamp that's BEFORE the timestamp set by lastRunCache?
                    // in that case, the CLB would miss the updated data and not run again
                    // TODO: possible solution: get timestamp of latest change in any of the dependent layers before running clb (=at the moment of decision whether to run CLB)
                    // store in cache under the name "consideredLayerDataUpUntil", use this for comparison (in addition or instead?)
                    var changesetProxy = new ChangesetProxy(user.InDatabase, TimeThreshold.BuildLatest(), changesetModel);

                    clLogger.LogInformation($"Running CLB {clb.Name} on layer {layerID}");
                    var stopWatch = new Stopwatch();
                    stopWatch.Start();
                    var layer = Layer.Build(layerID); // HACK, TODO: either pass layer-ID or layer-data, not Layer object
                    var successful = await clb.Run(layer, clBrainConfig, changesetProxy, modelContextBuilder, clLogger);
                    stopWatch.Stop();
                    TimeSpan ts = stopWatch.Elapsed;
                    string elapsedTime = string.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
                    clLogger.LogInformation($"Done in {elapsedTime}; result: {(successful ? "success" : "failure")}");

                    if (successful)
                    {
                        using var transI2 = modelContextBuilder.BuildImmediate();
                        await clbLastRunCache.UpdateCache(lastRunKey, changesetProxy.TimeThreshold.Time, transI2);
                    }
                }
                finally
                {
                    scopedLifetimeAccessor.ResetLifetimeScope();
                }
            }
        }
    }

    public class CLBLastRunCache
    {
        private class CLBLastRunEntry
        {
            public DateTimeOffset LastRun { get; set; }
        }

        public async Task UpdateCache(string key, DateTimeOffset latestChange, IModelContext trans)
        {
            var prefixedKey = $"CLBLastRun_{key}";
            using var command = new NpgsqlCommand(@"
                INSERT INTO config.general (key, config) VALUES (@key, @config) ON CONFLICT (key) DO UPDATE SET config = EXCLUDED.config
            ", trans.DBConnection, trans.DBTransaction);
            command.Parameters.AddWithValue("key", prefixedKey);
            var d = new CLBLastRunEntry() { LastRun = latestChange };
            var json = JsonSerializer.SerializeToDocument(d);
            command.Parameters.Add(new NpgsqlParameter("config", NpgsqlDbType.Json) { Value = json });
            await command.ExecuteScalarAsync();
        }

        public async Task<DateTimeOffset?> TryGetValue(string key, IModelContext trans)
        {
            var prefixedKey = $"CLBLastRun_{key}";
            using var command = new NpgsqlCommand("SELECT config FROM config.general WHERE key = @key LIMIT 1", trans.DBConnection, trans.DBTransaction);
            command.Parameters.AddWithValue("key", prefixedKey);
            using var s = await command.ExecuteReaderAsync();

            if (await s.ReadAsync())
            {
                try
                {
                    var json = s.GetFieldValue<JsonDocument>(0);
                    var d = json.Deserialize<CLBLastRunEntry>();
                    return d?.LastRun;
                }
                catch (Exception)
                {
                    return null;
                }
            } else
            {
                return null;
            }
        }
    }
}
