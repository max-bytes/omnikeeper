using Autofac;
using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Service;
using Quartz;
using System;
using System.Threading.Tasks;

namespace Omnikeeper.Runners
{
    [DisallowConcurrentExecution]
    public class MarkedForDeletionJob : IJob
    {
        private readonly ILayerModel layerModel;
        private readonly ILayerDataModel layerDataModel;
        private readonly IModelContextBuilder modelContextBuilder;
        private readonly ILifetimeScope parentLifetimeScope;
        private readonly IChangesetModel changesetModel;
        private readonly ScopedLifetimeAccessor scopedLifetimeAccessor;
        private readonly ILogger<MarkedForDeletionJob> logger;

        public MarkedForDeletionJob(ILayerModel layerModel, IModelContextBuilder modelContextBuilder, ILifetimeScope parentLifetimeScope,
            IChangesetModel changesetModel, ScopedLifetimeAccessor scopedLifetimeAccessor, ILogger<MarkedForDeletionJob> logger, ILayerDataModel layerDataModel)
        {
            this.layerModel = layerModel;
            this.modelContextBuilder = modelContextBuilder;
            this.parentLifetimeScope = parentLifetimeScope;
            this.changesetModel = changesetModel;
            this.scopedLifetimeAccessor = scopedLifetimeAccessor;
            this.logger = logger;
            this.layerDataModel = layerDataModel;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                var t = new StopTimer();
                logger.LogTrace("Start");

                // try to delete marked layers
                var toDeleteLayers = await layerDataModel.GetLayerData(AnchorStateFilter.MarkedForDeletion, modelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
                if (!toDeleteLayers.IsEmpty())
                {
                    // create a lifetime scope per invocation (similar to a HTTP request lifetime)
                    await using (var scope = parentLifetimeScope.BeginLifetimeScope(Autofac.Core.Lifetime.MatchingScopeLifetimeTags.RequestLifetimeScopeTag, builder =>
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

                            var changesetProxy = new ChangesetProxy(user.InDatabase, TimeThreshold.BuildLatest(), changesetModel, new DataOriginV1(DataOriginType.Manual));

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
                                        await layerDataModel.TryToDelete(d.LayerID, changesetProxy, trans);
                                    }
                                    catch (Exception) { }

                                    trans.Commit();
                                }
                                else
                                {
                                    logger.LogDebug($"Could not delete layer {d.LayerID}");
                                }
                            }
                        }
                        finally
                        {
                            scopedLifetimeAccessor.ResetLifetimeScope();
                        }
                    }
                    t.Stop((ts, elapsedTime) => logger.LogTrace($"Finished in {elapsedTime}"));
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error running marked-for-deletion job");
            }
        }
    }
}
