using Autofac;
using Hangfire;
using Hangfire.Server;
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
using Omnikeeper.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Runners
{
    public class CLBRunner
    {
        public CLBRunner(IEnumerable<IComputeLayerBrain> existingComputeLayerBrains, GenericTraitEntityModel<CLConfigV1, string> clConfigModel,
            IMetaConfigurationModel metaConfigurationModel, ILifetimeScope parentLifetimeScope,
            IChangesetModel changesetModel, ScopedLifetimeAccessor scopedLifetimeAccessor, CLBLastRunCache clbLastRunCache,
            ILayerDataModel layerDataModel, ILogger<CLBRunner> logger, IModelContextBuilder modelContextBuilder)
        {
            this.existingComputeLayerBrains = existingComputeLayerBrains.ToDictionary(l => l.Name);
            this.clConfigModel = clConfigModel;
            this.metaConfigurationModel = metaConfigurationModel;
            this.changesetModel = changesetModel;
            this.lifetimeScope = parentLifetimeScope;
            this.scopedLifetimeAccessor = scopedLifetimeAccessor;
            this.clbLastRunCache = clbLastRunCache; // TODO: check if that works in HA scenario
            this.layerDataModel = layerDataModel;
            this.logger = logger;
            this.modelContextBuilder = modelContextBuilder;
        }

        [MaximumConcurrentExecutions(1, timeoutInSeconds: 120)]
        [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        [SkipWhenPreviousJobIsRunning]
        public void Run(PerformContext? context)
        {
            using (HangfireConsoleLogger.InContext(context))
            {
                RunAsync().GetAwaiter().GetResult();
            }
        }

        public async Task RunAsync()
        {
            logger.LogInformation("Start");

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
                        logger.LogError($"Could not find cl config with ID {l.CLConfigID}");
                    }
                    else
                    {
                        if (!existingComputeLayerBrains.TryGetValue(clConfig.CLBrainReference, out var clb))
                        {
                            logger.LogError($"Could not find compute layer brain with name {clConfig.CLBrainReference}");
                        }
                        else
                        {
                            var lastRunKey = $"{clb.Name}{l.CLConfigID}";
                            DateTimeOffset? lastRun = null;
                            if (clbLastRunCache.TryGetValue(lastRunKey, out var lr))
                                lastRun = lr;

                            if (await clb.CanSkipRun(lastRun, clConfig.CLBrainConfig, logger, modelContextBuilder))
                            {
                                logger.LogInformation($"Skipping run of CLB {clb.Name} on layer {l.LayerID}");
                            }
                            else
                            {
                                // create a lifetime scope per clb invocation (similar to a HTTP request lifetime)
                                await using (var scope = lifetimeScope.BeginLifetimeScope(builder =>
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

                                        logger.LogInformation($"Running CLB {clb.Name} on layer {l.LayerID}");
                                        Stopwatch stopWatch = new Stopwatch();
                                        stopWatch.Start();
                                        var layer = Layer.Build(l.LayerID); // HACK, TODO: either pass layer-ID or layer-data, not Layer object
                                        await clb.Run(layer, clConfig.CLBrainConfig, changesetProxy, modelContextBuilder, logger);
                                        stopWatch.Stop();
                                        TimeSpan ts = stopWatch.Elapsed;
                                        string elapsedTime = string.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
                                        logger.LogInformation($"Done in {elapsedTime}");

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

            logger.LogInformation("Finished");
        }

        private readonly IDictionary<string, IComputeLayerBrain> existingComputeLayerBrains;
        private readonly GenericTraitEntityModel<CLConfigV1, string> clConfigModel;
        private readonly IMetaConfigurationModel metaConfigurationModel;
        private readonly ILifetimeScope lifetimeScope;
        private readonly IChangesetModel changesetModel;
        private readonly ScopedLifetimeAccessor scopedLifetimeAccessor;
        private readonly CLBLastRunCache clbLastRunCache;
        private readonly ILayerDataModel layerDataModel;
        private readonly ILogger<CLBRunner> logger;
        private readonly IModelContextBuilder modelContextBuilder;
    }
}
