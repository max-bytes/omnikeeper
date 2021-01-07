using Newtonsoft.Json;
using Omnikeeper.Base.Utils;
using ProtoBuf;
using System;

namespace Omnikeeper.Base.Entity.Config
{
    [ProtoContract(SkipConstructor = true)]
    public class BaseConfigurationV1
    {
        public readonly static TimeSpan InfiniteArchiveChangesetThreshold = TimeSpan.FromTicks(long.MaxValue);

        [ProtoMember(1)] private readonly TimeSpan archiveChangesetThreshold;
        [ProtoMember(2)] private readonly string clbRunnerInterval;
        [ProtoMember(3)] private readonly string markedForDeletionRunnerInterval;
        [ProtoMember(4)] private readonly string externalIDManagerRunnerInterval;
        [ProtoMember(5)] private readonly string archiveOldDataRunnerInterval;

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
