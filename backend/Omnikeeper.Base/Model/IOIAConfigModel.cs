using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Inbound;
using Npgsql;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface IOIAConfigModel
    {
        Task<IEnumerable<OIAConfig>> GetConfigs(bool useFallbackConfig, NpgsqlTransaction trans);
        Task<OIAConfig> GetConfigByName(string name, NpgsqlTransaction trans);
        Task<OIAConfig> Create(string name, IOnlineInboundAdapter.IConfig config, NpgsqlTransaction trans);
        Task<OIAConfig> Update(long id, string name, IOnlineInboundAdapter.IConfig config, NpgsqlTransaction trans);
        Task<OIAConfig> Delete(long iD, NpgsqlTransaction transaction);
    }
}
