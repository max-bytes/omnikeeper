using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.Config;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model.Config
{
    // refactor into generic version, like GenericTraitEntityModel, but for Singletons
    public class BaseConfigurationModel : SingletonTraitDataConfigBaseModel<BaseConfigurationV2>, IBaseConfigurationModel
    {
        public BaseConfigurationModel(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IBaseAttributeModel baseAttributeModel, IBaseRelationModel baseRelationModel) 
            : base(RecursiveTraitService.FlattenSingleRecursiveTrait(TraitEntityHelper.Class2RecursiveTrait<BaseConfigurationV2>()), effectiveTraitModel, ciModel, baseAttributeModel, baseRelationModel)
        {
        }

        public async Task<BaseConfigurationV2> GetConfigOrDefault(LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            var baseConfig = await TryToGet(layerSet, timeThreshold, trans);
            if (baseConfig == default)
            {
                return new BaseConfigurationV2(
                    TimeSpan.FromDays(90),
                    "*/15 * * * * *",
                    "*/5 * * * * *",
                    "* * * * *",
                    "0 */15 * * * *"
                );
            } else
            {
                return baseConfig.Item2;
            }
        }

        public async Task<BaseConfigurationV2> SetConfig(BaseConfigurationV2 config, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans)
        {
            var (dc, _) = await InsertOrUpdateAttributes(layerSet, writeLayerID, dataOrigin, changesetProxy, trans,
                ("base_config.archive_data_threshold", new AttributeScalarValueInteger(config.ArchiveDataThreshold.Ticks)),
                ("base_config.clb_runner_interval", new AttributeScalarValueText(config.CLBRunnerInterval)),
                ("base_config.marked_for_deletion_runner_interval", new AttributeScalarValueText(config.MarkedForDeletionRunnerInterval)),
                ("base_config.external_id_manager_runner_interval", new AttributeScalarValueText(config.ExternalIDManagerRunnerInterval)),
                ("base_config.archive_old_data_runner_interval", new AttributeScalarValueText(config.ArchiveOldDataRunnerInterval)),
                (ICIModel.NameAttribute, new AttributeScalarValueText("Base-Config"))
            );

            return dc;
        }
    }
}
