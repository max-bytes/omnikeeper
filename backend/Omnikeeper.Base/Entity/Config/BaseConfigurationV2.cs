using Newtonsoft.Json;
using Omnikeeper.Base.Utils;
using ProtoBuf;
using System;

namespace Omnikeeper.Base.Entity.Config
{
    [ProtoContract(SkipConstructor = true)]
    public class BaseConfigurationV2
    {
        public readonly static TimeSpan InfiniteArchiveDataThreshold = TimeSpan.FromTicks(long.MaxValue);

        [ProtoMember(1)] private readonly TimeSpan archiveDataThreshold;
        [ProtoMember(2)] private readonly string clbRunnerInterval;
        [ProtoMember(3)] private readonly string markedForDeletionRunnerInterval;
        [ProtoMember(4)] private readonly string externalIDManagerRunnerInterval;
        [ProtoMember(5)] private readonly string archiveOldDataRunnerInterval;

        [JsonProperty(Required = Required.Always)]
        public TimeSpan ArchiveDataThreshold => archiveDataThreshold;
        [JsonProperty(Required = Required.Always)]
        public string CLBRunnerInterval => clbRunnerInterval;
        [JsonProperty(Required = Required.Always)]
        public string MarkedForDeletionRunnerInterval => markedForDeletionRunnerInterval;
        [JsonProperty(Required = Required.Always)]
        public string ExternalIDManagerRunnerInterval => externalIDManagerRunnerInterval;
        [JsonProperty(Required = Required.Always)]
        public string ArchiveOldDataRunnerInterval => archiveOldDataRunnerInterval;

        // TODO: remove?
        public static MyJSONSerializer<BaseConfigurationV2> Serializer = new MyJSONSerializer<BaseConfigurationV2>(new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.Objects,
            MissingMemberHandling = MissingMemberHandling.Error
        });

        public BaseConfigurationV2(TimeSpan archiveDataThreshold, string clbRunnerInterval, string markedForDeletionRunnerInterval, string externalIDManagerRunnerInterval, string archiveOldDataRunnerInterval)
        {
            this.archiveDataThreshold = archiveDataThreshold;
            this.clbRunnerInterval = clbRunnerInterval;
            this.markedForDeletionRunnerInterval = markedForDeletionRunnerInterval;
            this.externalIDManagerRunnerInterval = externalIDManagerRunnerInterval;
            this.archiveOldDataRunnerInterval = archiveOldDataRunnerInterval;
        }
    }
}
