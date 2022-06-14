using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Service
{
    public interface IIssueContextSource
    {
        Task<IList<(string type, string context)>> GetIssueContexts(IModelContext trans, TimeThreshold timeThreshold);
    }
}
