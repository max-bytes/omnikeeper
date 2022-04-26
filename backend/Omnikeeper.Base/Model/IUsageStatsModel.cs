using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils.ModelContext;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface IUsageStatsModel
    {
        Task<IEnumerable<UsageStatElement>> GetElements(System.DateTimeOffset from, System.DateTimeOffset to, IModelContext trans);
    }
}
