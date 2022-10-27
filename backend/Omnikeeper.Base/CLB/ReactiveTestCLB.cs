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
        private readonly ReactiveGenericTraitEntityModel<TargetHost> targetHostModel;

        public ReactiveTestCLB(ReactiveRunService reactiveRunService, GenericTraitEntityModel<TargetHost> targetHostModel, IAttributeModel attributeModel)
        {
            this.reactiveRunService = reactiveRunService;
            this.attributeModel = attributeModel;
            this.targetHostModel = new ReactiveGenericTraitEntityModel<TargetHost>(targetHostModel);
        }

        public string Name => GetType().Name!;

        public ISet<string> GetDependentLayerIDs(string targetLayerID, JsonDocument config, ILogger logger) => new HashSet<string>() { "tsa_cmdb" };

        public IObservable<(bool result, ReactiveRunData runData)> BuildPipeline(IObservable<ReactiveRunData> run, string targetLayerID, JsonDocument clbConfig, ILogger logger)
        {
            var sourceLayerIDCMDB = "tsa_cmdb"; // TODO

            var changedCIIDs = reactiveRunService.ChangedCIIDsObs(run);

            var newAndChangedTargetHosts = targetHostModel.GetNewAndChangedByCIID(changedCIIDs.Zip(run), new LayerSet(sourceLayerIDCMDB));

            //var targetHosts = targetHostModel.GetAllByCIID(newAndChangedTargetHosts);

            var final = changedCIIDs
                .Zip(newAndChangedTargetHosts, run)
                .Select(async t =>
                {
                    var (relevantCIs, newAndChangedTargetHosts, runData) = t;

                    var uppercaseHostnameFragments = new List<BulkCIAttributeDataCIAndAttributeNameScope.Fragment>();
                    foreach(var targetHost in newAndChangedTargetHosts)
                    {
                        uppercaseHostnameFragments.Add(new BulkCIAttributeDataCIAndAttributeNameScope.Fragment(targetHost.Key, "uppercase_hostname", new AttributeScalarValueText(targetHost.Value.Hostname?.ToUpperInvariant() ?? "NOT SET")));
                    }
                    var writtenAttributes = await attributeModel.BulkReplaceAttributes(
                        new BulkCIAttributeDataCIAndAttributeNameScope(targetLayerID, uppercaseHostnameFragments, relevantCIs, AllAttributeSelection.Instance),
                        runData.ChangesetProxy,
                        runData.Trans,
                        MaskHandlingForRemovalApplyNoMask.Instance,
                        OtherLayersValueHandlingForceWrite.Instance);

                    logger.LogInformation($"Finished inner; written {writtenAttributes} attributes, thread: {Thread.CurrentThread.ManagedThreadId}");

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
