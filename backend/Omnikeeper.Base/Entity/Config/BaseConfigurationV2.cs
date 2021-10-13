using Newtonsoft.Json;
using Omnikeeper.Base.Utils;
using ProtoBuf;
using System;

namespace Omnikeeper.Base.Entity.Config
{
    [TraitEntity("__meta.config.base", TraitOriginType.Core)]
    public class BaseConfigurationV2 : TraitEntity
    {
        public readonly static TimeSpan InfiniteArchiveDataThreshold = TimeSpan.FromTicks(long.MaxValue);

        [TraitAttribute("archive_data_threshold", "base_config.archive_data_threshold")]
        [JsonIgnore]
        public readonly long archiveDataThresholdTicks;

        // TODO: add regex or other check for hangfire compatible cronjob syntax
        [TraitAttribute("clb_runner_interval", "base_config.clb_runner_interval")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        [JsonIgnore]
        public readonly string clbRunnerInterval;

        [TraitAttribute("marked_for_deletion_runner_interval", "base_config.marked_for_deletion_runner_interval")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        [JsonIgnore]
        public readonly string markedForDeletionRunnerInterval;

        [TraitAttribute("external_id_manager_runner_interval", "base_config.external_id_manager_runner_interval")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        [JsonIgnore]
        public readonly string externalIDManagerRunnerInterval;

        [TraitAttribute("archive_old_data_runner_interval", "base_config.archive_old_data_runner_interval")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        [JsonIgnore]
        public readonly string archiveOldDataRunnerInterval;

        [TraitAttribute("name", "__name", optional: true)]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        [JsonIgnore]
        public readonly string Name;

        [JsonProperty(Required = Required.Always)]
        public TimeSpan ArchiveDataThreshold { get; }
        [JsonProperty(Required = Required.Always)]
        public string CLBRunnerInterval => clbRunnerInterval;
        [JsonProperty(Required = Required.Always)]
        public string MarkedForDeletionRunnerInterval => markedForDeletionRunnerInterval;
        [JsonProperty(Required = Required.Always)]
        public string ExternalIDManagerRunnerInterval => externalIDManagerRunnerInterval;
        [JsonProperty(Required = Required.Always)]
        public string ArchiveOldDataRunnerInterval => archiveOldDataRunnerInterval;

        public static MyJSONSerializer<BaseConfigurationV2> Serializer = new MyJSONSerializer<BaseConfigurationV2>(new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.Objects,
            MissingMemberHandling = MissingMemberHandling.Error
        });

        public BaseConfigurationV2()
        {
            this.archiveDataThresholdTicks = 0L;
            this.ArchiveDataThreshold = TimeSpan.FromTicks(archiveDataThresholdTicks);
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
            this.ArchiveDataThreshold = archiveDataThreshold;
            this.clbRunnerInterval = clbRunnerInterval;
            this.markedForDeletionRunnerInterval = markedForDeletionRunnerInterval;
            this.externalIDManagerRunnerInterval = externalIDManagerRunnerInterval;
            this.archiveOldDataRunnerInterval = archiveOldDataRunnerInterval;
            this.Name = "Base-Configuration";
        }
    }
}
