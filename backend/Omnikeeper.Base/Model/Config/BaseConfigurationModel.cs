using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.Config;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model.Config
{
    public class BaseConfigurationModel : SingletonTraitEntityModel<BaseConfigurationV2>, IBaseConfigurationModel
    {
        public BaseConfigurationModel(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel)
            : base(effectiveTraitModel, ciModel, attributeModel, relationModel)
        {
        }

        public async Task<BaseConfigurationV2> GetConfigOrDefault(LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            var baseConfig = await TryToGet(layerSet, timeThreshold, trans);
            if (baseConfig == default)
            {
                return new BaseConfigurationV2(
                    TimeSpan.FromDays(90),
                    "*/5 * * * * ?",
                    "*/5 * * * * ?",
                    "* * * * * ?",
                    "0 0 1 * * ?"
                );
            }
            else
            {
                return baseConfig.Item2;
            }
        }

        public async Task<BaseConfigurationV2> SetConfig(BaseConfigurationV2 config, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans)
        {
            var (dc, _) = await InsertOrUpdate(config, "Base-Config", layerSet, writeLayerID, dataOrigin, changesetProxy, trans, MaskHandlingForRemovalApplyNoMask.Instance);

            return dc;
        }
    }
}
