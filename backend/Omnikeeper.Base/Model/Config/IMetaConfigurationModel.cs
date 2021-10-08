using Omnikeeper.Base.Entity.Config;
using Omnikeeper.Base.Utils.ModelContext;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model.Config
{
    public interface IMetaConfigurationModel
    {
        Task<MetaConfiguration> GetConfig(IModelContext trans);
        Task<MetaConfiguration> GetConfigOrDefault(IModelContext trans);
        Task<MetaConfiguration> SetConfig(MetaConfiguration config, IModelContext trans);

        Task<bool> IsLayerPartOfMetaConfiguration(string layerID, IModelContext trans);
    }
}
