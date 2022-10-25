using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading;

namespace Omnikeeper.Base.CLB
{
    public class ReactiveTestCLB : IReactiveCLB
    {
        private readonly ReactiveRunService reactiveRunService;
        private readonly ReactiveGenericTraitEntityModel<TargetHost> targetHostModel;

        public ReactiveTestCLB(ReactiveRunService reactiveRunService, GenericTraitEntityModel<TargetHost> targetHostModel)
        {
            this.reactiveRunService = reactiveRunService;
            this.targetHostModel = new ReactiveGenericTraitEntityModel<TargetHost>(targetHostModel);
        }

        public string Name => GetType().Name!;

        public ISet<string> GetDependentLayerIDs(string targetLayerID, JsonDocument config, ILogger logger) => new HashSet<string>() { "tmp" };

        public IObservable<(bool result, ReactiveRunData runData)> BuildPipeline(IObservable<ReactiveRunData> run, ILogger logger)
        {
            var changedCIIDs = reactiveRunService.ChangedCIIDsObs(run);
            var changedCIIDs2 = reactiveRunService.ChangedCIIDsObs(run); // second time, testing model semaphore

            //changedCIIDs.Zip(changedCIIDs2).Do(t =>
            //{
            //    var ciids1 = t.First;
            //    var ciids2 = t.Second;
            //    logger.LogInformation(ciids1.ToString());
            //    logger.LogInformation(ciids2.ToString());
            //});

            var runTimes = run.Scan(new List<TimeThreshold>(), (list, d) =>
            {
                list.Add(d.ChangesetProxy.TimeThreshold);
                return list;
            });

            var newAndChangedTargetHosts = targetHostModel.GetNewAndChangedByCIID(changedCIIDs.Zip(run), new LayerSet("tmp"));

            // TODO: move into method
            var targetHosts = newAndChangedTargetHosts.Scan((IDictionary<Guid, TargetHost>)new Dictionary<Guid, TargetHost>(), (dict, tuple) =>
            {
                var (newAndChanged, ciSelection) = tuple;
                switch (ciSelection)
                {
                    case AllCIIDsSelection _:
                        return newAndChanged;
                    case NoCIIDsSelection _:
                        return dict;
                    case SpecificCIIDsSelection s:
                        foreach (var ciid in s.CIIDs)
                            if (newAndChanged.TryGetValue(ciid, out var entity))
                                dict[ciid] = entity;
                            else
                                dict.Remove(ciid);
                        return dict;
                    case AllCIIDsExceptSelection e:
                        throw new NotImplementedException(); // TODO: think about and implement
                    default:
                        throw new Exception("Unknown CIIDSelection encountered");
                }
            });

            //throw new Exception("!"); // TODO: testing

            var tmp = changedCIIDs.Zip(changedCIIDs2, runTimes, targetHosts, run);

            var final = tmp.Select(t =>
            {
                logger.LogInformation($"Starting inner; thread: {Thread.CurrentThread.ManagedThreadId}");
                var ciids1 = t.First;
                var ciids2 = t.Second;
                var runTimes = t.Third;
                var targetHosts = t.Fourth;
                var runData = t.Fifth;
                logger.LogInformation(ciids1.ToString());
                logger.LogInformation(ciids2.ToString());
                logger.LogInformation(string.Join(",", runTimes.Select(l => l.ToString())));
                logger.LogInformation(targetHosts.Count.ToString());
                logger.LogInformation(string.Join(",", targetHosts.Select(t => t.Value.ID)));
                //Thread.Sleep(10000);
                logger.LogInformation($"Finished inner; thread: {Thread.CurrentThread.ManagedThreadId}");

                //throw new Exception("!"); // TODO: testing

                return (result: true, runData: runData);
            });
            return final;
        }
    }


    // just for testing purposes
    [TraitEntity("test.host", TraitOriginType.Plugin)]
    public class TargetHost : TraitEntity
    {
        [TraitAttribute("cmdb_id", "cmdb.host.id")]
        [TraitEntityID]
        public string ID;

        [TraitAttribute("hostname", "hostname", optional: true)]
        public string? Hostname;

        [TraitAttribute("location", "cmdb.host.location", optional: true)]
        public string? Location;

        [TraitAttribute("os", "cmdb.host.os", optional: true)]
        public string? OS;

        [TraitAttribute("platform", "cmdb.host.platform", optional: true)]
        public string? Platform;

        [TraitAttribute("status", "cmdb.host.status", optional: true)]
        public string? Status;

        [TraitAttribute("environment", "cmdb.host.environment", optional: true)]
        public string? Environment;

        public TargetHost()
        {
            ID = "";
            Hostname = null;
            Location = null;
            OS = null;
            Platform = null;
            Status = null;
            Environment = null;
        }
    }
}
