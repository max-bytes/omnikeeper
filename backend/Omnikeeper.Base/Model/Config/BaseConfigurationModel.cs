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
    public class BaseConfigurationModel : SingletonTraitDataConfigBaseModel<BaseConfigurationV2>, IBaseConfigurationModel
    {
        public static readonly RecursiveTrait Trait = new RecursiveTrait("__meta.config.base", new TraitOriginV1(TraitOriginType.Core),
            new List<TraitAttribute>() {
                new TraitAttribute("archive_data_threshold", CIAttributeTemplate.BuildFromParams("base_config.archive_data_threshold", AttributeValueType.Integer, false)),
                // TODO: add regex or other check for hangfire compatible cronjob syntax
                new TraitAttribute("clb_runner_interval", CIAttributeTemplate.BuildFromParams("base_config.clb_runner_interval", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
                new TraitAttribute("marked_for_deletion_runner_interval", CIAttributeTemplate.BuildFromParams("base_config.marked_for_deletion_runner_interval", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
                new TraitAttribute("external_id_manager_runner_interval", CIAttributeTemplate.BuildFromParams("base_config.external_id_manager_runner_interval", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
                new TraitAttribute("archive_old_data_runner_interval", CIAttributeTemplate.BuildFromParams("base_config.archive_old_data_runner_interval", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
            },
            new List<TraitAttribute>()
            {
                new TraitAttribute("name", CIAttributeTemplate.BuildFromParams(ICIModel.NameAttribute, AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
            }
        );
        public static readonly GenericTrait TraitFlattened = RecursiveTraitService.FlattenSingleRecursiveTrait(Trait);

        public BaseConfigurationModel(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IBaseAttributeModel baseAttributeModel, IBaseRelationModel baseRelationModel) 
            : base(TraitFlattened, effectiveTraitModel, ciModel, baseAttributeModel, baseRelationModel)
        {
        }

        public async Task<BaseConfigurationV2> GetConfig(LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            return await Get(layerSet, timeThreshold, trans);
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

        protected override BaseConfigurationV2 EffectiveTrait2DC(EffectiveTrait et)
        {
            var archiveDataThresholdTicks = TraitConfigDataUtils.ExtractMandatoryScalarIntegerAttribute(et, "archive_data_threshold");
            var archiveDataThreshold = TimeSpan.FromTicks(archiveDataThresholdTicks);
            var clbRunnerInterval = TraitConfigDataUtils.ExtractMandatoryScalarTextAttribute(et, "clb_runner_interval");
            var markedForDeletionRunnerInterval = TraitConfigDataUtils.ExtractMandatoryScalarTextAttribute(et, "marked_for_deletion_runner_interval");
            var externalIDManagerRunnerInterval = TraitConfigDataUtils.ExtractMandatoryScalarTextAttribute(et, "external_id_manager_runner_interval");
            var archiveOldDataRunnerInterval = TraitConfigDataUtils.ExtractMandatoryScalarTextAttribute(et, "archive_old_data_runner_interval");
            return new BaseConfigurationV2(archiveDataThreshold, clbRunnerInterval, markedForDeletionRunnerInterval, externalIDManagerRunnerInterval, archiveOldDataRunnerInterval);
        }
    }
}
