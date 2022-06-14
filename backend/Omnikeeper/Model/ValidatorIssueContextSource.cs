using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Model
{
    public class ValidatorIssueContextSource : IIssueContextSource
    {
        private readonly IMetaConfigurationModel metaConfigurationModel;
        private readonly ValidatorContextV1Model validatorContextModel;

        public ValidatorIssueContextSource(IMetaConfigurationModel metaConfigurationModel, ValidatorContextV1Model validatorContextModel)
        {
            this.metaConfigurationModel = metaConfigurationModel;
            this.validatorContextModel = validatorContextModel;
        }

        // NOTE: implementation must follow ValidatorJob implementation/spec of IssueContexts there
        public async Task<IList<(string type, string context)>> GetIssueContexts(IModelContext trans, TimeThreshold timeThreshold)
        {
            var ret = new List<(string type, string context)>();
            var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(trans);
            var contexts = await validatorContextModel.GetByCIID(AllCIIDsSelection.Instance, metaConfiguration.ConfigLayerset, trans, timeThreshold);

            foreach (var c in contexts)
                ret.Add(("Validator", c.Value.ID));
            return ret;
        }
    }
}
