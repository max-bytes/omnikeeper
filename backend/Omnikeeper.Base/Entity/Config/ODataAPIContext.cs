
using Newtonsoft.Json;
using Omnikeeper.Base.Utils;

namespace Omnikeeper.Base.Entity
{

    public class ODataAPIContext
    {
        public interface IConfig { }

        public class ConfigV3 : IConfig
        {
            public ConfigV3(long writeLayerID, long[] readLayerset)
            {
                WriteLayerID = writeLayerID;
                ReadLayerset = readLayerset;
            }

            public long WriteLayerID { get; set; }
            public long[] ReadLayerset { get; set; }
        }

        public string ID { get; set; }
        public IConfig CConfig { get; set; }

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
