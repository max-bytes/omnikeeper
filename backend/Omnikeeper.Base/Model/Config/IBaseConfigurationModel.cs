using Omnikeeper.Base.Entity.Config;
using Omnikeeper.Base.Utils.ModelContext;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model.Config
{
    public interface IBaseConfigurationModel
    {
        Task<BaseConfigurationV1> GetConfig(IModelContext trans);
        Task<BaseConfigurationV1> GetConfigOrDefault(IModelContext trans);
        Task<BaseConfigurationV1> SetConfig(BaseConfigurationV1 config, IModelContext trans);

        Task<bool> IsLayerPartOfBaseConfiguration(string layerID, IModelContext trans);
    }
}
