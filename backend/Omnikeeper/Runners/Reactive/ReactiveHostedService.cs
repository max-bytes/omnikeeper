using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Omnikeeper.Runners.Reactive
{
    public class ReactiveHostedService : IHostedService, IDisposable
    {
        private readonly Dictionary<string, IReactiveCLB> existingRCLBs;
        private readonly ISet<string> existingCLBs;
        private readonly ICalculateUnprocessedChangesetsService calculateUnprocessedChangesetsService;
        private readonly ILoggerFactory loggerFactory;
        private readonly IIssuePersister issuePersister;
        private readonly ILayerDataModel layerDataModel;
        private readonly IConfiguration configuration;
        private readonly ScopedLifetimeAccessor scopedLifetimeAccessor;
        private readonly ILifetimeScope rootScope;
        private readonly IChangesetModel changesetModel;
        private readonly IMetaConfigurationModel metaConfigurationModel;
        private readonly CLConfigV1Model clConfigModel;

        private IDisposable? sentinelToken;
        private readonly IList<RunningCLB> runningCLBs;

        public ReactiveHostedService(ICalculateUnprocessedChangesetsService calculateUnprocessedChangesetsService, ILoggerFactory loggerFactory, IIssuePersister issuePersister, ILayerDataModel layerDataModel, IConfiguration configuration,
            IEnumerable<IReactiveCLB> existingRCLBs, IEnumerable<IComputeLayerBrain> existingCLBs, ScopedLifetimeAccessor scopedLifetimeAccessor, ILifetimeScope lifetimeScope, IChangesetModel changesetModel, IMetaConfigurationModel metaConfigurationModel, CLConfigV1Model clConfigModel)
        {
            this.existingRCLBs = existingRCLBs.ToDictionary(l => l.Name);
            this.existingCLBs = existingCLBs.Select(l => l.Name).ToHashSet();
            this.calculateUnprocessedChangesetsService = calculateUnprocessedChangesetsService;
            this.loggerFactory = loggerFactory;
            this.issuePersister = issuePersister;
            this.layerDataModel = layerDataModel;
            this.configuration = configuration;
            this.scopedLifetimeAccessor = scopedLifetimeAccessor;
            rootScope = lifetimeScope;
            this.changesetModel = changesetModel;
            this.metaConfigurationModel = metaConfigurationModel;
            this.clConfigModel = clConfigModel;
            runningCLBs = new List<RunningCLB>();
        }

        public void Dispose()
        {
            sentinelToken?.Dispose();
            sentinelToken = null;
            foreach(var runningCLB in runningCLBs)
                runningCLB.CancellationToken.Dispose();
            runningCLBs.Clear();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (configuration.GetValue("RunComputeLayers", false))
            {
                // sentinel, that (re)starts actual runners
                var sentintelLogger = loggerFactory.CreateLogger("reactive_clb_sentinel");
                sentinelToken = Observable.Create<IList<ActiveCLB>>(async (o) =>
                    {
                        try
                        {
                            var activeCLBs = await CalculateActiveCLBs(sentintelLogger);
                            o.OnNext(activeCLBs);
                        }
                        catch (Exception e)
                        {
                            sentintelLogger.LogError(e, "Error calculating active reactive CLBs");
                        }
                    })
                    .Concat(Observable.Empty<IList<ActiveCLB>>().Delay(TimeSpan.FromMilliseconds(5000)))
                    .Repeat()
                    .Publish().RefCount()
                    .Subscribe(activeCLBs =>
                    {
                        try
                        {
                            // compare active to running CLBs, update running
                            var clbsToStop = new List<RunningCLB>(runningCLBs);
                            var clbsToStart = new List<ActiveCLB>();
                            foreach (var activeCLB in activeCLBs)
                            {
                                var runningCLB = runningCLBs.FirstOrDefault(r => r.LayerData.Equals(activeCLB.LayerData) && r.CLConfig.Equals(activeCLB.CLConfig));
                                if (runningCLB == null)
                                { // active, but not running -> start
                                    clbsToStart.Add(activeCLB);
                                }
                                else
                                { // active and running, nothing to do
                                    clbsToStop.Remove(runningCLB);
                                }
                            }

                            foreach (var clbToStop in clbsToStop)
                            {
                                sentintelLogger.LogInformation($"Stopping outdated reactive CLB; layer-ID: {clbToStop.LayerData.LayerID}, CL-config-ID: {clbToStop.CLConfig.ID}");
                                clbToStop.CancellationToken.Dispose();
                                runningCLBs.Remove(clbToStop);
                            }
                            foreach (var clbToStart in clbsToStart)
                            {
                                try
                                {
                                    sentintelLogger.LogInformation($"Starting new reactive CLB; layer-ID: {clbToStart.LayerData.LayerID}, CL-config-ID: {clbToStart.CLConfig.ID}");

                                    var cancellationToken = StartRunning(clbToStart.CLConfig, clbToStart.LayerData.LayerID, clbToStart.CLB, (e) =>
                                    {
                                        sentintelLogger.LogError(e, "Error running reactive CLB");
                                        var runningCLB = runningCLBs.FirstOrDefault(r => r.LayerData.Equals(clbToStart.LayerData) && r.CLConfig.Equals(clbToStart.CLConfig));
                                        if (runningCLB != null)
                                        {
                                            runningCLB.CancellationToken.Dispose();
                                            runningCLBs.Remove(runningCLB);
                                        }
                                    });
                                    runningCLBs.Add(new RunningCLB(clbToStart.CLConfig, clbToStart.LayerData, cancellationToken));
                                }
                                catch (Exception e)
                                {
                                    sentintelLogger.LogError(e, "Error starting reactive CLB");
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            sentintelLogger.LogError(e, "Error running reactive CLB sentinel");
                        }
                    });
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            sentinelToken?.Dispose();
            sentinelToken = null;
            foreach (var runningCLB in runningCLBs)
                runningCLB.CancellationToken.Dispose();
            runningCLBs.Clear();

            return Task.CompletedTask;
        }

        public record class ActiveCLB(CLConfigV1 CLConfig, LayerData LayerData, IReactiveCLB CLB);
        public record class RunningCLB(CLConfigV1 CLConfig, LayerData LayerData, IDisposable CancellationToken);

        public async Task<IList<ActiveCLB>> CalculateActiveCLBs(ILogger logger)
        {
            var ret = new List<ActiveCLB>();

            await using (var scope = rootScope.BeginLifetimeScope(Autofac.Core.Lifetime.MatchingScopeLifetimeTags.RequestLifetimeScopeTag))
            {
                var modelContextBuilder = scope.Resolve<IModelContextBuilder>();
                logger.LogTrace("Start");

                var timeThreshold = TimeThreshold.BuildLatest();
                var trans = modelContextBuilder.BuildImmediate();
                var activeLayers = await layerDataModel.GetLayerData(AnchorStateFilter.ActiveAndDeprecated, trans, timeThreshold);
                var layersWithCLBs = activeLayers.Where(l => l.CLConfigID != "");

                if (!layersWithCLBs.IsEmpty())
                {
                    var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(trans);
                    var clConfigs = await clConfigModel.GetByCIID(AllCIIDsSelection.Instance, metaConfiguration.ConfigLayerset, trans, timeThreshold);
                    var clConfigLookup = clConfigs.ToLookup(kv => kv.Value.ID, c => (ciid: c.Key, config: c.Value));

                    foreach (var l in layersWithCLBs)
                    {
                        // find clConfig for layer
                        var foundCLConfigs = clConfigLookup[l.CLConfigID];
                        if (foundCLConfigs.IsEmpty())
                        {
                            logger.LogError($"Could not find cl config with ID {l.CLConfigID}");
                        }
                        else if (foundCLConfigs.Count() > 1)
                        {
                            logger.LogError($"Found more than 1 cl config with ID {l.CLConfigID}");
                        }
                        else
                        {
                            var clConfig = foundCLConfigs.First();
                            if (!existingRCLBs.TryGetValue(clConfig.config.CLBrainReference, out var clb))
                            {
                                if (!existingCLBs.Contains(clConfig.config.CLBrainReference)) // NOTE: we test whether the referenced CLB is a regular CLB and if so, ignore it
                                    logger.LogError($"Could not find reactive compute layer brain with name {clConfig.config.CLBrainReference}");
                            } else
                            {
                                ret.Add(new ActiveCLB(clConfig.config, l, clb));
                            }
                        }
                    }
                }

                logger.LogTrace("Finished");
            }

            return ret;
        }

        private IDisposable StartRunning(CLConfigV1 clConfig, string layerID, IReactiveCLB clb, Action<Exception> onFatalError)
        {
            var clLogger = loggerFactory.CreateLogger($"RCLB_{clConfig.ID}@{layerID}");

            //clLogger.LogInformation("Starting on threadId:{0}", Thread.CurrentThread.ManagedThreadId);

            var clbProcessingCache = new CLBProcessingCache();

            IObservable<ReactiveRunData> run = CreateRunObservable(clbProcessingCache, clConfig, layerID, clb, clLogger);

            // NOTE: idea using higher-order observable taken from https://stackoverflow.com/questions/74172099/rx-net-disposing-of-resources-created-during-observable-create
            // TODO: test
            //var final = run.Select(rrd =>
            //    clb.BuildPipeline(run, clLogger)
            //    .Catch((Exception e) =>
            //    {
            //        return (Observable.Return((result: false, runData: rrd)));
            //    })
            //).Concat();
            var final = clb.BuildPipeline(run, clLogger);

            return final.Select(t =>
            {
                return Observable.FromAsync(async () =>
                {
                    var (result, runData) = t;
                    try
                    {
                        //clLogger.LogInformation($"trans ID on commit: {t.runData.Trans.DBTransaction?.GetHashCode()}");

                        if (t.result)
                        {
                            var configActuality = DateTimeOffset.MinValue; // TODO: remove
                            clbProcessingCache.UpdateCache(clConfig.ID, layerID, t.runData.LatestSeenChangesets, configActuality);

                            t.runData.Trans.Commit();
                        }
                        else
                        {
                            t.runData.Trans.Rollback();
                        }

                        var modelContextBuilder = t.runData.Scope.Resolve<IModelContextBuilder>();
                        using var transUpdateIssues = modelContextBuilder.BuildDeferred();
                        clLogger.LogInformation($"Run produced {t.runData.IssueAccumulator.Issues.Count} issues in total");
                        await issuePersister.Persist(t.runData.IssueAccumulator, transUpdateIssues, t.runData.ChangesetProxy);
                        transUpdateIssues.Commit();

                        clLogger.LogInformation($"Finished RCLB run; result: {(result ? "success" : "failure")}");
                    }
                    catch (Exception e)
                    {
                        clLogger.LogError(e, "Error in non-RX part of CLB");
                    }
                    finally
                    {
                        scopedLifetimeAccessor.ResetLifetimeScope();
                        t.runData.Dispose();
                    }
                });
            })
                .Concat()
                .Subscribe((_) => { }, (Exception e) =>
                {
                    onFatalError(e);
                });

            //return final.Subscribe(
            //    async (t) =>
            //    {
            //        var (result, runData) = t;
            //        try
            //        {
            //            //clLogger.LogInformation($"trans ID on commit: {t.runData.Trans.DBTransaction?.GetHashCode()}");

            //            if (t.result)
            //            {
            //                var configActuality = DateTimeOffset.MinValue; // TODO: remove
            //                clbProcessingCache.UpdateCache(clConfig.ID, layerID, t.runData.LatestSeenChangesets, configActuality);

            //                t.runData.Trans.Commit();
            //            }
            //            else
            //            {
            //                t.runData.Trans.Rollback();
            //            }

            //            var modelContextBuilder = t.runData.Scope.Resolve<IModelContextBuilder>();
            //            using var transUpdateIssues = modelContextBuilder.BuildDeferred();
            //            clLogger.LogInformation($"Run produced {t.runData.IssueAccumulator.Issues.Count} issues in total");
            //            await issuePersister.Persist(t.runData.IssueAccumulator, transUpdateIssues, t.runData.ChangesetProxy);
            //            transUpdateIssues.Commit();

            //            clLogger.LogInformation($"Finished RCLB run; result: {(result ? "success" : "failure")}");
            //        } catch (Exception e)
            //        {
            //            clLogger.LogError(e, "Error in non-RX part of CLB");
            //        } finally
            //        {
            //            scopedLifetimeAccessor.ResetLifetimeScope();
            //            t.runData.Dispose();
            //        }
            //    },
            //    (Exception e) =>
            //    {
            //        onFatalError(e);
            //    });
        }

        private IObservable<ReactiveRunData> CreateRunObservable(CLBProcessingCache clbProcessingCache, CLConfigV1 clConfig, string layerID, IReactiveCLB clb, ILogger clLogger)
        {
            var start = Observable.Create<ReactiveRunData>(async (o) =>
            {
                //clLogger.LogInformation($"Running CLBWrapper; thread: {Thread.CurrentThread.ManagedThreadId}");

                //Thread.Sleep(10000);

                //throw new Exception("?");

                // NOTE: to avoid race conditions, we use a single timeThreshold and base everything off of this
                var timeThreshold = TimeThreshold.BuildLatest();

                var canSkipRun = false;

                IReadOnlyDictionary<string, IReadOnlyList<Changeset>?> unprocessedChangesets;
                IReadOnlyDictionary<string, Guid> latestSeenChangesets;

                var username = $"__cl.{clConfig.ID}@{layerID}"; // construct username
                using (var scope = rootScope.BeginLifetimeScope(Autofac.Core.Lifetime.MatchingScopeLifetimeTags.RequestLifetimeScopeTag, builder =>
                {
                    builder.RegisterType<CurrentAuthorizedCLBUserService>().As<ICurrentUserService>().WithParameter("username", username).InstancePerLifetimeScope();
                }))
                {
                    scopedLifetimeAccessor.SetLifetimeScope(scope);
                    var modelContextBuilder = scope.Resolve<IModelContextBuilder>(); // resolve our own ModelContextBuilder, that has a request-lifetime

                    //clLogger.LogInformation($"Running CLBWrapper 2; thread: {Thread.CurrentThread.ManagedThreadId}");

                    var dependentLayerIDs = clb.GetDependentLayerIDs(layerID, clConfig.CLBrainConfig, clLogger);

                    // calculate unprocessed changesets
                    var (processedChangesets, processedConfigActuality) = clbProcessingCache.TryGetValue(clConfig.ID, layerID);
                    using var trans = modelContextBuilder.BuildImmediate();
                    //clLogger.LogInformation($"intermediate CLBWrapper; thread: {Thread.CurrentThread.ManagedThreadId}");
                    (unprocessedChangesets, latestSeenChangesets) = await calculateUnprocessedChangesetsService.CalculateUnprocessedChangesets(processedChangesets, dependentLayerIDs, timeThreshold, trans);
                    //clLogger.LogInformation($"intermediate CLBWrapper; thread: {Thread.CurrentThread.ManagedThreadId}");
                    if (unprocessedChangesets.All(kv => kv.Value != null && kv.Value.IsEmpty()))
                    {
                        clLogger.LogDebug($"Skipping run of CLB {clb.Name} on layer {layerID} because no unprocessed changesets exist for dependent layers");
                        canSkipRun = true;
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

                    scopedLifetimeAccessor.ResetLifetimeScope();
                }

                //canSkipRun = false; // TODO: for testing only

                if (!canSkipRun)
                {
                    // create a scope for the runtime of the CLB, MUST be disposed later
                    var scope = rootScope.BeginLifetimeScope(Autofac.Core.Lifetime.MatchingScopeLifetimeTags.RequestLifetimeScopeTag, builder =>
                    {
                        builder.RegisterType<CurrentAuthorizedCLBUserService>().As<ICurrentUserService>().WithParameter("username", username).InstancePerLifetimeScope();
                    });
                    scopedLifetimeAccessor.SetLifetimeScope(scope);
                    var modelContextBuilder = scope.Resolve<IModelContextBuilder>(); // resolve our own ModelContextBuilder, that has a request-lifetime

                    var issueAccumulator = new IssueAccumulator("ComputeLayerBrain", $"{clConfig.ID}@{layerID}");

                    var transUpsertUser = modelContextBuilder.BuildDeferred();
                    var currentUserService = scope.Resolve<ICurrentUserService>();
                    var user = await currentUserService.GetCurrentUser(transUpsertUser);
                    transUpsertUser.Commit();
                    transUpsertUser.Dispose();

                    var changesetProxy = new ChangesetProxy(user.InDatabase, timeThreshold, changesetModel, new DataOriginV1(DataOriginType.ComputeLayer));
                    var transRun = modelContextBuilder.BuildDeferred();
                    var runData = new ReactiveRunData(unprocessedChangesets, latestSeenChangesets, changesetProxy, transRun, scope, issueAccumulator);

                    o.OnNext(runData);
                }

                o.OnCompleted();

                //clLogger.LogInformation($"Finished CLBWrapper; thread: {Thread.CurrentThread.ManagedThreadId}");

                return Disposable.Empty;// Disposable.Create(() => clLogger.LogInformation("Observer has unsubscribed"));
            })
                .Concat(Observable.Empty<ReactiveRunData>().Delay(TimeSpan.FromSeconds(2)))
                .Repeat() // Resubscribe indefinitely after source completes
                .Publish().RefCount() // see http://northhorizon.net/2011/sharing-in-rx/
                ;
            return start;
        }
    }
}