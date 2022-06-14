using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OKPluginGenericJSONIngest
{
    public class IssueContextSource : IIssueContextSource
    {
        private readonly IMetaConfigurationModel metaConfigurationModel;
        private readonly ContextModel contextModel;

        public IssueContextSource(IMetaConfigurationModel metaConfigurationModel, ContextModel contextModel)
        {
            this.metaConfigurationModel = metaConfigurationModel;
            this.contextModel = contextModel;
        }

        // NOTE: implementation must follow GenericJsonIngestService implementation/spec of IssueContexts there
        public async Task<IList<(string type, string context)>> GetIssueContexts(IModelContext trans, TimeThreshold timeThreshold)
        {
            var ret = new List<(string type, string context)>();
            var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(trans);
            var contexts = await contextModel.GetByCIID(AllCIIDsSelection.Instance, metaConfiguration.ConfigLayerset, trans, timeThreshold);

            foreach (var c in contexts)
                ret.Add(("DataIngest", $"GenericJsonIngest_{c.Value.ID}"));
            return ret;
        }
    }
}
