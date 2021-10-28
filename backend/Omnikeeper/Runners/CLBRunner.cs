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
            IMetaConfigurationModel metaConfigurationModel, ICurrentUserInDatabaseService currentUserService,
            IChangesetModel changesetModel, IUserInDatabaseModel userModel, CLBContextAccessor clbContextAccessor,
            ILayerModel layerModel, ILogger<CLBRunner> logger, IModelContextBuilder modelContextBuilder)
        {
            this.existingComputeLayerBrains = existingComputeLayerBrains.ToDictionary(l => l.Name);
            this.clConfigModel = clConfigModel;
            this.metaConfigurationModel = metaConfigurationModel;
            this.currentUserService = currentUserService;
            this.changesetModel = changesetModel;
            this.userModel = userModel;
            this.clbContextAccessor = clbContextAccessor;
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
            var activeLayers = await layerModel.GetLayers(AnchorStateFilter.ActiveAndDeprecated, trans);
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
                            clbContextAccessor.SetCLBContext(new CLBContext(clb));

                            using var transUpsertUser = modelContextBuilder.BuildDeferred();
                            var user = await currentUserService.CreateAndGetCurrentUser(transUpsertUser);
                            transUpsertUser.Commit();

                            var changesetProxy = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);

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
                                clbContextAccessor.ClearCLBContext();
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
        private readonly ICurrentUserInDatabaseService currentUserService;
        private readonly IChangesetModel changesetModel;
        private readonly IUserInDatabaseModel userModel;
        private readonly CLBContextAccessor clbContextAccessor;
        private readonly ILayerModel layerModel;
        private readonly ILogger<CLBRunner> logger;
        private readonly IModelContextBuilder modelContextBuilder;
    }
}
