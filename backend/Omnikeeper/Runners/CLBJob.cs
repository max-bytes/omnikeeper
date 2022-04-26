using Autofac;
using Microsoft.Extensions.Logging;
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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Runners
{
    [DisallowConcurrentExecution]
    public class CLBJob : IJob
    {
        public CLBJob(IEnumerable<IComputeLayerBrain> existingComputeLayerBrains, CLConfigV1Model clConfigModel,
            IMetaConfigurationModel metaConfigurationModel, ILifetimeScope parentLifetimeScope,
            IChangesetModel changesetModel, ScopedLifetimeAccessor scopedLifetimeAccessor, CLBLastRunCache clbLastRunCache,
            ILayerDataModel layerDataModel, ILoggerFactory loggerFactory, IModelContextBuilder modelContextBuilder)
        {
            this.existingComputeLayerBrains = existingComputeLayerBrains.ToDictionary(l => l.Name);
            this.clConfigModel = clConfigModel;
            this.metaConfigurationModel = metaConfigurationModel;
            this.changesetModel = changesetModel;
            this.lifetimeScope = parentLifetimeScope;
            this.scopedLifetimeAccessor = scopedLifetimeAccessor;
            this.clbLastRunCache = clbLastRunCache; // TODO: check if that works in HA scenario
            this.layerDataModel = layerDataModel;
            this.loggerFactory = loggerFactory;
            this.modelContextBuilder = modelContextBuilder;
            this.baseLogger = loggerFactory.CreateLogger<CLBJob>();
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
                        var clLogger = loggerFactory.CreateLogger($"CLB_{l.CLConfigID}");

                        // find clConfig for layer
                        if (!clConfigs.TryGetValue(l.CLConfigID, out var clConfig))
                        {
                            clLogger.LogError($"Could not find cl config with ID {l.CLConfigID}");
                        }
                        else
                        {
                            if (!existingComputeLayerBrains.TryGetValue(clConfig.CLBrainReference, out var clb))
                            {
                                clLogger.LogError($"Could not find compute layer brain with name {clConfig.CLBrainReference}");
                            }
                            else
                            {
                                var lastRunKey = $"{clb.Name}{l.CLConfigID}";
                                DateTimeOffset? lastRun = null;
                                if (clbLastRunCache.TryGetValue(lastRunKey, out var lr))
                                    lastRun = lr;

                                if (await clb.CanSkipRun(lastRun, clConfig.CLBrainConfig, clLogger, modelContextBuilder))
                                {
                                    clLogger.LogDebug($"Skipping run of CLB {clb.Name} on layer {l.LayerID}");
                                }
                                else
                                {
                                    // create a lifetime scope per clb invocation (similar to a HTTP request lifetime)
                                    await using (var scope = lifetimeScope.BeginLifetimeScope(Autofac.Core.Lifetime.MatchingScopeLifetimeTags.RequestLifetimeScopeTag, builder =>
                                    {
                                        builder.Register(builder => new CLBContext(clb)).InstancePerLifetimeScope();
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

                                            var changesetProxy = new ChangesetProxy(user.InDatabase, TimeThreshold.BuildLatest(), changesetModel);

                                            clLogger.LogInformation($"Running CLB {clb.Name} on layer {l.LayerID}");
                                            Stopwatch stopWatch = new Stopwatch();
                                            stopWatch.Start();
                                            var layer = Layer.Build(l.LayerID); // HACK, TODO: either pass layer-ID or layer-data, not Layer object
                                            await clb.Run(layer, clConfig.CLBrainConfig, changesetProxy, modelContextBuilder, clLogger);
                                            stopWatch.Stop();
                                            TimeSpan ts = stopWatch.Elapsed;
                                            string elapsedTime = string.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
                                            clLogger.LogInformation($"Done in {elapsedTime}");

                                            clbLastRunCache.UpdateCache(lastRunKey, changesetProxy.TimeThreshold.Time);
                                        }
                                        finally
                                        {
                                            scopedLifetimeAccessor.ResetLifetimeScope();
                                        }
                                    }
                                }
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

        private readonly IDictionary<string, IComputeLayerBrain> existingComputeLayerBrains;
        private readonly CLConfigV1Model clConfigModel;
        private readonly IMetaConfigurationModel metaConfigurationModel;
        private readonly ILifetimeScope lifetimeScope;
        private readonly IChangesetModel changesetModel;
        private readonly ScopedLifetimeAccessor scopedLifetimeAccessor;
        private readonly CLBLastRunCache clbLastRunCache;
        private readonly ILayerDataModel layerDataModel;
        private readonly ILoggerFactory loggerFactory;
        private readonly IModelContextBuilder modelContextBuilder;
        private readonly ILogger<CLBJob> baseLogger;
    }

    public class CLBLastRunCache
    {
        private readonly IDictionary<string, DateTimeOffset> cache = new Dictionary<string, DateTimeOffset>();

        public void UpdateCache(string key, DateTimeOffset latestChange)
        {
            cache[key] = latestChange;
        }

        public void RemoveFromCache(string key)
        {
            cache.Remove(key);
        }

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out DateTimeOffset v)
        {
            return cache.TryGetValue(key, out v);
        }
    }
}
