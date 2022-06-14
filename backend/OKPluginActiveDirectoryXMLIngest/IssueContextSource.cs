using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OKPluginActiveDirectoryXMLIngest
{
    public class IssueContextSource : IIssueContextSource
    {
        private readonly ILayerModel layerModel;

        public IssueContextSource(ILayerModel layerModel)
        {
            this.layerModel = layerModel;
        }

        // NOTE: implementation must follow ActiveDirectoryXMLIngestController implementation/spec of IssueContexts there
        public async Task<IList<(string type, string context)>> GetIssueContexts(IModelContext trans, TimeThreshold timeThreshold)
        {
            var ret = new List<(string type, string context)>();
            var layers = await layerModel.GetLayers(trans);

            foreach (var l in layers)
                ret.Add(("DataIngest", $"ActiveDirectoryXMLIngest_{l.ID}"));
            return ret;
        }
    }
}
