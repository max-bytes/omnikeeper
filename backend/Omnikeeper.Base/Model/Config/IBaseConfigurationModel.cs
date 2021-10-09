using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.Config;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model.Config
{
    public interface IBaseConfigurationModel
    {
        Task<BaseConfigurationV2> GetConfigOrDefault(LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans);
        Task<BaseConfigurationV2> SetConfig(BaseConfigurationV2 config, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans);
    }
}
