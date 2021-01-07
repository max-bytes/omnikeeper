
using Newtonsoft.Json;
using Omnikeeper.Base.Utils;
using ProtoBuf;
using System;

namespace Omnikeeper.Base.Entity
{
    [ProtoContract(SkipConstructor = true)]
    public class ODataAPIContext
    {
        [ProtoContract]
        [ProtoInclude(1, typeof(ConfigV3))]
        public interface IConfig { }

        [ProtoContract(SkipConstructor = true)]
        public class ConfigV3 : IConfig
        {
            public ConfigV3(long writeLayerID, long[] readLayerset)
            {
                WriteLayerID = writeLayerID;
                ReadLayerset = readLayerset;
            }

            [ProtoMember(1)] public long WriteLayerID { get; set; }
            [ProtoMember(2)] public long[] ReadLayerset { get; set; }
        }

        [ProtoMember(1)] public string ID { get; set; }
        [ProtoMember(2)] public IConfig CConfig { get; set; }

        public static MyJSONSerializer<IConfig> ConfigSerializer = new MyJSONSerializer<IConfig>(new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.Objects
        });

        public ODataAPIContext(string iD, IConfig cConfig)
        {
            ID = iD;
            CConfig = cConfig;
        }
    }

}
