using Autofac;
using Hangfire;
using Hangfire.Server;
using Microsoft.Extensions.Logging;
using Omnikeeper.Base.CLB;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Service;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Omnikeeper.Base.Model.TraitBased;

namespace Omnikeeper.Runners
{
    public class CLBRunner
    {
        public CLBRunner(IEnumerable<IComputeLayerBrain> existingComputeLayerBrains, GenericTraitEntityModel<CLConfigV1, string> clConfigModel,
            IMetaConfigurationModel metaConfigurationModel, ILifetimeScope parentLifetimeScope,
            IChangesetModel changesetModel, ScopedLifetimeAccessor scopedLifetimeAccessor,
            ILayerModel layerModel, ILogger<CLBRunner> logger, IModelContextBuilder modelContextBuilder)
        {
            this.existingComputeLayerBrains = existingComputeLayerBrains.ToDictionary(l => l.Name);
            this.clConfigModel = clConfigModel;
            this.metaConfigurationModel = metaConfigurationModel;
            this.lifetimeScope = parentLifetimeScope;
            this.changesetModel = changesetModel;
            this.scopedLifetimeAccessor = scopedLifetimeAccessor;
            this.layerModel = layerModel;
            this.logger = logger;
            this.modelContextBuilder = modelContextBuilder;
        }

        [MaximumConcurrentExecutions(1, timeoutInSeconds: 120)]
        [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
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
            var activeLayers = await layerModel.GetLayers(AnchorStateFilter.ActiveAndDeprecated, trans, TimeThreshold.BuildLatest());
            var layersWithCLBs = activeLayers.Where(l => l.CLConfigID != "");

            if (!layersWithCLBs.IsEmpty()) {
                var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(trans);
                var clConfigs = await clConfigModel.GetAllByDataID(metaConfiguration.ConfigLayerset, trans, TimeThreshold.BuildLatest());

                foreach (var l in layersWithCLBs)
                {
                    // find clConfig for layer
                    if (!clConfigs.TryGetValue(l.CLConfigID, out var clConfig)) 
                    {
                        logger.LogError($"Could not find cl config with ID {l.CLConfigID}");
                    } else {
                        if (!existingComputeLayerBrains.TryGetValue(clConfig.CLBrainReference, out var clb))
                        {
                            logger.LogError($"Could not find compute layer brain with name {clConfig.CLBrainReference}");
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

                                using var transUpsertUser = modelContextBuilder.BuildDeferred();
                                var currentUserService = scope.Resolve<ICurrentUserService>();
                                var user = await currentUserService.GetCurrentUser(transUpsertUser);
                                transUpsertUser.Commit();

                                var changesetProxy = new ChangesetProxy(user.InDatabase, TimeThreshold.BuildLatest(), changesetModel);

                                try
                                {
                                    logger.LogInformation($"Running CLB {clb.Name} on layer {l.ID}");
                                    Stopwatch stopWatch = new Stopwatch();
                                    stopWatch.Start();
                                    await clb.Run(l, clConfig.CLBrainConfig, changesetProxy, modelContextBuilder, logger);
                                    stopWatch.Stop();
                                    TimeSpan ts = stopWatch.Elapsed;
                                    string elapsedTime = string.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
                                    logger.LogInformation($"Done in {elapsedTime}");
                                } finally
                                {
                                    scopedLifetimeAccessor.ResetLifetimeScope();
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
        private readonly ILayerModel layerModel;
        private readonly ILogger<CLBRunner> logger;
        private readonly IModelContextBuilder modelContextBuilder;
    }
}
