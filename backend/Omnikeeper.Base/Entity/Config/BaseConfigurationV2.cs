using Omnikeeper.Base.Utils;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Omnikeeper.Base.Entity.Config
{
    [TraitEntity("__meta.config.base", TraitOriginType.Core)]
    public class BaseConfigurationV2 : TraitEntity
    {
        public readonly static TimeSpan InfiniteArchiveDataThreshold = TimeSpan.FromTicks(long.MaxValue);

        [TraitAttribute("archive_data_threshold", "base_config.archive_data_threshold")]
        public readonly long archiveDataThresholdTicks;

        // TODO: add regex or other check for quartz compatible cronjob syntax
        [TraitAttribute("clb_runner_interval", "base_config.clb_runner_interval")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        public readonly string clbRunnerInterval;

        [TraitAttribute("marked_for_deletion_runner_interval", "base_config.marked_for_deletion_runner_interval")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        public readonly string markedForDeletionRunnerInterval;

        [TraitAttribute("external_id_manager_runner_interval", "base_config.external_id_manager_runner_interval")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        public readonly string externalIDManagerRunnerInterval;

        [TraitAttribute("archive_old_data_runner_interval", "base_config.archive_old_data_runner_interval")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        public readonly string archiveOldDataRunnerInterval;

        [TraitAttribute("name", "__name", optional: true)]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        public readonly string Name;

        public TimeSpan ArchiveDataThreshold => TimeSpan.FromTicks(archiveDataThresholdTicks);
        public string CLBRunnerInterval => clbRunnerInterval;
        public string MarkedForDeletionRunnerInterval => markedForDeletionRunnerInterval;
        public string ExternalIDManagerRunnerInterval => externalIDManagerRunnerInterval;
        public string ArchiveOldDataRunnerInterval => archiveOldDataRunnerInterval;

        public static SystemTextJSONSerializer<BaseConfigurationV2> Serializer = new SystemTextJSONSerializer<BaseConfigurationV2>(new JsonSerializerOptions() { });

        public BaseConfigurationV2()
        {
            this.archiveDataThresholdTicks = 0L;
            this.clbRunnerInterval = "";
            this.markedForDeletionRunnerInterval = "";
            this.externalIDManagerRunnerInterval = "";
            this.archiveOldDataRunnerInterval = "";
            this.Name = "";
        }

        [JsonConstructor]
        public BaseConfigurationV2(TimeSpan archiveDataThreshold, string clbRunnerInterval, string markedForDeletionRunnerInterval, string externalIDManagerRunnerInterval, string archiveOldDataRunnerInterval)
        {
            this.archiveDataThresholdTicks = archiveDataThreshold.Ticks;
            this.clbRunnerInterval = clbRunnerInterval;
            this.markedForDeletionRunnerInterval = markedForDeletionRunnerInterval;
            this.externalIDManagerRunnerInterval = externalIDManagerRunnerInterval;
            this.archiveOldDataRunnerInterval = archiveOldDataRunnerInterval;
            this.Name = "Base-Configuration";
        }
    }
}
