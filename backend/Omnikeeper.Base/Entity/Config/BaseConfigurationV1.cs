using Newtonsoft.Json;
using Omnikeeper.Base.Utils;
using ProtoBuf;
using System;
using System.Collections.Generic;

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
        [ProtoMember(6)] private readonly string[] configLayerset;
        [ProtoMember(7)] private readonly string configWriteLayer;

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
        [JsonProperty(Required = Required.Always)]
        public string[] ConfigLayerset => configLayerset;
        [JsonProperty(Required = Required.Always)]
        public string ConfigWriteLayer => configWriteLayer;

        public static MyJSONSerializer<BaseConfigurationV1> Serializer = new MyJSONSerializer<BaseConfigurationV1>(new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.Objects,
            MissingMemberHandling = MissingMemberHandling.Error
        });

        public BaseConfigurationV1(TimeSpan archiveChangesetThreshold, string clbRunnerInterval, string markedForDeletionRunnerInterval, string externalIDManagerRunnerInterval, string archiveOldDataRunnerInterval, string[] configLayerset, string configWriteLayer)
        {
            this.archiveChangesetThreshold = archiveChangesetThreshold;
            this.clbRunnerInterval = clbRunnerInterval;
            this.markedForDeletionRunnerInterval = markedForDeletionRunnerInterval;
            this.externalIDManagerRunnerInterval = externalIDManagerRunnerInterval;
            this.archiveOldDataRunnerInterval = archiveOldDataRunnerInterval;
            this.configLayerset = configLayerset;
            this.configWriteLayer = configWriteLayer;
        }
    }
}
