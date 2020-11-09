using Newtonsoft.Json;
using Omnikeeper.Base.Utils;
using System;

namespace Omnikeeper.Base.Entity.Config
{
    public class BaseConfigurationV1
    {
        public readonly static TimeSpan InfiniteArchiveChangesetThreshold = TimeSpan.FromTicks(long.MaxValue);

        [JsonProperty(Required = Required.Always)]
        public TimeSpan ArchiveChangesetThreshold { get; set; }
        [JsonProperty(Required = Required.Always)]
        public string CLBRunnerInterval { get; set; }
        [JsonProperty(Required = Required.Always)]
        public string MarkedForDeletionRunnerInterval { get; set; }
        [JsonProperty(Required = Required.Always)]
        public string ExternalIDManagerRunnerInterval { get; set; }
        [JsonProperty(Required = Required.Always)]
        public string ArchiveOldDataRunnerInterval { get; set; }

        public static MyJSONSerializer<BaseConfigurationV1> Serializer = new MyJSONSerializer<BaseConfigurationV1>(new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.Objects,
            MissingMemberHandling = MissingMemberHandling.Error
        });
    }
}
