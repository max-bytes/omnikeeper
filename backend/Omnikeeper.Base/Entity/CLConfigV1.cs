using Newtonsoft.Json.Linq;
using ProtoBuf;
using System;

namespace Omnikeeper.Base.Entity
{
    [ProtoContract(SkipConstructor = true)]
    public class CLConfigV1 : IEquatable<CLConfigV1>
    {
        public CLConfigV1(string id, string clBrainReference, JObject clBrainConfig)
        {
            ID = id;
            CLBrainReference = clBrainReference;
            CLBrainConfig = clBrainConfig;
        }

        [ProtoMember(1)] public readonly string ID;
        [ProtoMember(2)] public readonly string CLBrainReference;
        [ProtoMember(3)] public readonly JObject CLBrainConfig;

        public override bool Equals(object? obj) => Equals(obj as CLConfigV1);
        public bool Equals(CLConfigV1? other)
        {
            return other != null && ID == other.ID &&
                   CLBrainReference == other.CLBrainReference &&
                   CLBrainConfig.Equals(other.CLBrainConfig);
        }
        public override int GetHashCode() => HashCode.Combine(ID, CLBrainReference, CLBrainConfig);
    }
}
