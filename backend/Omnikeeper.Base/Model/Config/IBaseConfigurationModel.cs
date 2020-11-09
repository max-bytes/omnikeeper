using Npgsql;
using Omnikeeper.Base.Entity.Config;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model.Config
{
    public interface IBaseConfigurationModel
    {
        Task<BaseConfigurationV1> GetConfig(NpgsqlTransaction trans);
        Task<BaseConfigurationV1> GetConfigOrDefault(NpgsqlTransaction trans);
        Task<BaseConfigurationV1> SetConfig(BaseConfigurationV1 config, NpgsqlTransaction trans);
    }
}
