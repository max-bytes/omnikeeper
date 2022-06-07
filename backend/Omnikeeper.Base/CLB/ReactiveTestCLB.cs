using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Incremental;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Omnikeeper.Base.CLB
{
    public class ReactiveTestCLB : CLBBase
    {
        private readonly ICIModel ciModel;
        private readonly IAttributeModel attributeModel;
        private readonly ITraitsProvider traitsProvider;
        private readonly IEffectiveTraitModel effectiveTraitModel;
        private readonly IChangesetModel changesetModel;

        private static IncrementalStore store = new IncrementalStore(); // TODO: make cluster-aware or pin job to node

        public ReactiveTestCLB(ICIModel ciModel, IAttributeModel attributeModel,
            ITraitsProvider traitsProvider, IEffectiveTraitModel effectiveTraitModel, IChangesetModel changesetModel)
        {
            this.ciModel = ciModel;
            this.attributeModel = attributeModel;
            this.traitsProvider = traitsProvider;
            this.effectiveTraitModel = effectiveTraitModel;
            this.changesetModel = changesetModel;
        }
        public override ISet<string>? GetDependentLayerIDs(JsonDocument config, ILogger logger) => new HashSet<string>() { "tsa_cmdb" };

        public override async Task<bool> Run(string targetLayerID, IReadOnlyDictionary<string, IReadOnlyList<Changeset>?> unprocessedChangesets, 
            JsonDocument config, IChangesetProxy changesetProxy, IModelContext trans, ILogger logger, IIssueAccumulator issueAccumulator)
        {
            var incrementalCISelectionModel = new IncrementalCISelectionModel(changesetModel);
            var incrementalCIModel = new IncrementalCIModel(ciModel, attributeModel, store);
            var incrementalEffectiveTraitModel = new IncrementalEffectiveTraitModel(effectiveTraitModel, store);
            var incrementalHosts = new IncrementalGenericTraitEntity<TargetHost>(store);

            var timeThreshold = changesetProxy.TimeThreshold;

            var cmdbHostTrait = await traitsProvider.GetActiveTrait("tsa_cmdb.host", trans, timeThreshold);
            if (cmdbHostTrait == null) throw new Exception();
            var ciSelection = await incrementalCISelectionModel.InitOrUpdate(unprocessedChangesets, new LayerSet("tsa_cmdb"), trans);
            var cis = await incrementalCIModel.InitOrUpdate(ciSelection, new LayerSet("tsa_cmdb"), AllAttributeSelection.Instance, "foo", trans, timeThreshold);
            var ets = await incrementalEffectiveTraitModel.InitOrUpdate(cis, cmdbHostTrait, new LayerSet("tsa_cmdb"), "foo", trans, timeThreshold);
            var entities = incrementalHosts.InitOrUpdate(ets, new LayerSet("tsa_cmdb"), "foo", trans, timeThreshold);

            logger.LogInformation($"#entities: {entities.All.Count}, #changed: {entities.Updated.Count}, #removed: {entities.Removed.Count()}");

            return true;
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
