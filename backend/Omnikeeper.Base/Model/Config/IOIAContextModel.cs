using Npgsql;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Inbound;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface IOIAContextModel
    {
        Task<IEnumerable<OIAContext>> GetContexts(bool useFallbackConfig, NpgsqlTransaction trans);
        Task<OIAContext> GetContextByName(string name, NpgsqlTransaction trans);
        Task<OIAContext> Create(string name, IOnlineInboundAdapter.IConfig config, NpgsqlTransaction trans);
        Task<OIAContext> Update(long id, string name, IOnlineInboundAdapter.IConfig config, NpgsqlTransaction trans);
        Task<OIAContext> Delete(long iD, NpgsqlTransaction transaction);
    }
}
