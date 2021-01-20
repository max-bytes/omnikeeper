using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Utils.ModelContext;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface IOIAContextModel
    {
        Task<IEnumerable<OIAContext>> GetContexts(bool useFallbackConfig, IModelContext trans);
        Task<OIAContext> GetContextByName(string name, IModelContext trans);
        Task<OIAContext> Create(string name, IOnlineInboundAdapter.IConfig config, IModelContext trans);
        Task<OIAContext> Update(long id, string name, IOnlineInboundAdapter.IConfig config, IModelContext trans);
        Task<OIAContext> Delete(long iD, IModelContext transaction);
    }
}
