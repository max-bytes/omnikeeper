using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
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
        private readonly IAttributeModel attributeModel;
        private readonly ICIModel ciModel;
        private readonly ReactiveGenericTraitEntityModel<TargetHost> targetHostModel;

        public ReactiveTestCLB(ReactiveRunService reactiveRunService, GenericTraitEntityModel<TargetHost> targetHostModel, IAttributeModel attributeModel, ICIModel ciModel)
        {
            this.reactiveRunService = reactiveRunService;
            this.attributeModel = attributeModel;
            this.ciModel = ciModel;
            this.targetHostModel = new ReactiveGenericTraitEntityModel<TargetHost>(targetHostModel);
        }

        public string Name => GetType().Name!;

        public ISet<string> GetDependentLayerIDs(string targetLayerID, JsonDocument config, ILogger logger) => new HashSet<string>() { "tsa_cmdb" };

        public IObservable<(bool result, ReactiveRunData runData)> BuildPipeline(IObservable<ReactiveRunData> run, ILogger logger)
        {
            var sourceLayerIDCMDB = "tsa_cmdb"; // TODO
            var targetLayerID = "tmp"; // TODO

            var changedCIIDs = reactiveRunService.ChangedCIIDsObs(run);
            var changedCIIDs2 = reactiveRunService.ChangedCIIDsObs(run); // second time, testing model semaphore

            //changedCIIDs.Zip(changedCIIDs2).Do(t =>
            //{
            //    var ciids1 = t.First;
            //    var ciids2 = t.Second;
            //    logger.LogInformation(ciids1.ToString());
            //    logger.LogInformation(ciids2.ToString());
            //});

            //var runTimes = run.Scan(new List<TimeThreshold>(), (list, d) =>
            //{
            //    list.Add(d.ChangesetProxy.TimeThreshold);
            //    return list;
            //});

            var newAndChangedTargetHosts = targetHostModel.GetNewAndChangedByCIID(changedCIIDs.Zip(run), new LayerSet(sourceLayerIDCMDB));

            //var targetHosts = targetHostModel.GetAllByCIID(newAndChangedTargetHosts);

            //throw new Exception("!"); // TODO: testing

            var tmp = changedCIIDs.Zip(changedCIIDs2, newAndChangedTargetHosts, run);

            var final = tmp.Select(async t =>
            {
                var ciids1 = t.First;
                var ciids2 = t.Second;
                var newAndChangedTargetHosts = t.Third.newAndChanged;
                var relevantCIs = t.Third.ciSelection;
                var runData = t.Fourth;
                logger.LogInformation(ciids1.ToString());
                logger.LogInformation(ciids2.ToString());
                logger.LogInformation(newAndChangedTargetHosts.Count.ToString());
                //logger.LogInformation(string.Join(",", targetHosts.Select(t => t.Value.ID)));

                var uppercaseHostnameFragments = new List<BulkCIAttributeDataCIAndAttributeNameScope.Fragment>();
                foreach(var targetHost in newAndChangedTargetHosts)
                {
                    uppercaseHostnameFragments.Add(new BulkCIAttributeDataCIAndAttributeNameScope.Fragment(targetHost.Key, "uppercase_hostname", new AttributeScalarValueText(targetHost.Value.Hostname?.ToUpperInvariant() ?? "NOT SET")));
                }
                var relevantCIsSet = (await relevantCIs.GetCIIDsAsync(async () => await ciModel.GetCIIDs(runData.Trans))).ToHashSet(); // TODO: remove
                await attributeModel.BulkReplaceAttributes(
                    new BulkCIAttributeDataCIAndAttributeNameScope(targetLayerID, uppercaseHostnameFragments, relevantCIsSet, new HashSet<string>() { "uppercase_hostname" }),
                    //new BulkCIAttributeDataLayerScope(targetLayerID, uppercaseHostnameFragments),
                    runData.ChangesetProxy,
                    runData.Trans,
                    MaskHandlingForRemovalApplyNoMask.Instance,
                    OtherLayersValueHandlingForceWrite.Instance);

                logger.LogInformation($"Finished inner; thread: {Thread.CurrentThread.ManagedThreadId}");

                //throw new Exception("!"); // TODO: testing

                return (result: true, runData: runData);
            })
                .Concat();
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
