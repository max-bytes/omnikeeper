using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Model
{
    public class CLBIssueContextSource : IIssueContextSource
    {
        private readonly IMetaConfigurationModel metaConfigurationModel;
        private readonly ILayerDataModel layerDataModel;
        private readonly CLConfigV1Model clConfigModel;

        public CLBIssueContextSource(IMetaConfigurationModel metaConfigurationModel, ILayerDataModel layerDataModel, CLConfigV1Model clConfigModel)
        {
            this.metaConfigurationModel = metaConfigurationModel;
            this.layerDataModel = layerDataModel;
            this.clConfigModel = clConfigModel;
        }

        // NOTE: implementation must follow CLBJob implementation/spec of IssueContexts there
        public async Task<IList<(string type, string context)>> GetIssueContexts(IModelContext trans, TimeThreshold timeThreshold)
        {
            var activeLayers = await layerDataModel.GetLayerData(AnchorStateFilter.ActiveAndDeprecated, trans, timeThreshold);
            var layersWithCLBs = activeLayers.Where(l => l.CLConfigID != "");

            var ret = new List<(string type, string context)>();
            if (!layersWithCLBs.IsEmpty())
            {
                var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(trans);
                var clConfigs = await clConfigModel.GetByDataID(AllCIIDsSelection.Instance, metaConfiguration.ConfigLayerset, trans, timeThreshold);

                foreach (var l in layersWithCLBs)
                    if (clConfigs.TryGetValue(l.CLConfigID, out var clConfig))
                        ret.Add(("ComputeLayerBrain", $"{clConfig.ID}@{l.LayerID}"));
            }
            return ret;
        }
    }
}
