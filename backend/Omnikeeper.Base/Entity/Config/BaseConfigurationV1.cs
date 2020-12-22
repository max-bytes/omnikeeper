using Newtonsoft.Json;
using Omnikeeper.Base.Utils;
using System;

namespace Omnikeeper.Base.Entity.Config
{
    [Serializable]
    public class BaseConfigurationV1
    {
        public readonly static TimeSpan InfiniteArchiveChangesetThreshold = TimeSpan.FromTicks(long.MaxValue);

        private readonly TimeSpan archiveChangesetThreshold;
        private readonly string clbRunnerInterval;
        private readonly string markedForDeletionRunnerInterval;
        private readonly string externalIDManagerRunnerInterval;
        private readonly string archiveOldDataRunnerInterval;

        [JsonProperty(Required = Required.Always)]
        public TimeSpan ArchiveChangesetThreshold => archiveChangesetThreshold;
        [JsonProperty(Required = Required.Always)]
        public string CLBRunnerInterval => clbRunnerInterval;
        [JsonProperty(Required = Required.Always)]
        public string MarkedForDeletionRunnerInterval => markedForDeletionRunnerInterval;
        [JsonProperty(Required = Required.Always)]
        public string ExternalIDManagerRunnerInterval => externalIDManagerRunnerInterval;
        [JsonProperty(Required = Required.Always)]
        public string ArchiveOldDataRunnerInterval => archiveOldDataRunnerInterval;

        public static MyJSONSerializer<BaseConfigurationV1> Serializer = new MyJSONSerializer<BaseConfigurationV1>(new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.Objects,
            MissingMemberHandling = MissingMemberHandling.Error
        });

        public BaseConfigurationV1(TimeSpan archiveChangesetThreshold, string clbRunnerInterval, string markedForDeletionRunnerInterval, string externalIDManagerRunnerInterval, string archiveOldDataRunnerInterval)
        {
            this.archiveChangesetThreshold = archiveChangesetThreshold;
            this.clbRunnerInterval = clbRunnerInterval;
            this.markedForDeletionRunnerInterval = markedForDeletionRunnerInterval;
            this.externalIDManagerRunnerInterval = externalIDManagerRunnerInterval;
            this.archiveOldDataRunnerInterval = archiveOldDataRunnerInterval;
        }
    }
}
