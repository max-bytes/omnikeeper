using Autofac;
using Hangfire;
using Hangfire.Server;
using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Service;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Omnikeeper.Runners
{
    public class MarkedForDeletionRunner
    {
        private readonly ILayerModel layerModel;
        private readonly ILayerDataModel layerDataModel;
        private readonly IModelContextBuilder modelContextBuilder;
        private readonly ILifetimeScope parentLifetimeScope;
        private readonly IChangesetModel changesetModel;
        private readonly ScopedLifetimeAccessor scopedLifetimeAccessor;
        private readonly ILogger<MarkedForDeletionRunner> logger;

        public MarkedForDeletionRunner(ILayerModel layerModel, IModelContextBuilder modelContextBuilder, ILifetimeScope parentLifetimeScope,
            IChangesetModel changesetModel, ScopedLifetimeAccessor scopedLifetimeAccessor, ILogger<MarkedForDeletionRunner> logger, ILayerDataModel layerDataModel)
        {
            this.layerModel = layerModel;
            this.modelContextBuilder = modelContextBuilder;
            this.parentLifetimeScope = parentLifetimeScope;
            this.changesetModel = changesetModel;
            this.scopedLifetimeAccessor = scopedLifetimeAccessor;
            this.logger = logger;
            this.layerDataModel = layerDataModel;
        }

        public async Task RunAsync()
        {
            // create a lifetime scope per invocation (similar to a HTTP request lifetime)
            await using (var scope = parentLifetimeScope.BeginLifetimeScope(builder =>
            {
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

                    var changesetProxy = new ChangesetProxy(user.InDatabase, TimeThreshold.BuildLatest(), changesetModel);

                    var stopWatch = new Stopwatch();
                    stopWatch.Start();
                    logger.LogInformation("Start");

                    // try to delete marked layers
                    var toDeleteLayers = await layerDataModel.GetLayerData(AnchorStateFilter.MarkedForDeletion, modelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
                    foreach (var d in toDeleteLayers)
                    {
                        using var trans = modelContextBuilder.BuildDeferred();
                        var wasDeleted = await layerModel.TryToDelete(d.LayerID, trans);
                        if (wasDeleted)
                        {
                            logger.LogInformation($"Deleted layer {d.LayerID}");

                            // optionally try to delete layer-data as well
                            try
                            {
                                await layerDataModel.TryToDelete(d.LayerID, new DataOriginV1(DataOriginType.Manual), changesetProxy, trans);
                            }
                            catch (Exception) { }

                            trans.Commit();
                        }
                        else
                        {
                            logger.LogDebug($"Could not delete layer {d.LayerID}");
                        }
                    }

                    stopWatch.Stop();
                    TimeSpan ts = stopWatch.Elapsed;
                    string elapsedTime = string.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
                    logger.LogInformation($"Finished in {elapsedTime}");
                }
                finally
                {
                    scopedLifetimeAccessor.ResetLifetimeScope();
                }
            }
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
    }
}
