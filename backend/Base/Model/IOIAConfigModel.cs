using Landscape.Base.Entity;
using Landscape.Base.Inbound;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Landscape.Base.Model
{
    public interface IOIAConfigModel
    {
        Task<IEnumerable<OIAConfig>> GetConfigs(NpgsqlTransaction trans);
        Task<OIAConfig> GetConfigByName(string name, NpgsqlTransaction trans);
        Task<OIAConfig> Create(string name, IOnlineInboundAdapter.IConfig config, NpgsqlTransaction trans);
        Task<OIAConfig> Update(long id, string name, IOnlineInboundAdapter.IConfig config, NpgsqlTransaction trans);
        Task<OIAConfig> Delete(long iD, NpgsqlTransaction transaction);
    }
}
