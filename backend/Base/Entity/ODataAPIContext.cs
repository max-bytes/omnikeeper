
using Landscape.Base.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using YamlDotNet.Serialization;

namespace Landscape.Base.Entity
{

    public class ODataAPIContext
    {
        public interface IConfig { }

        public class ConfigV3 : IConfig
        {
            public long WriteLayerID { get; set; }
            public long[] ReadLayerset { get; set; }
        }

        public string ID { get; set; }
        public IConfig CConfig { get; set; }

        public static MyJSONSerializer<IConfig> ConfigSerializer = new MyJSONSerializer<IConfig>(new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.Objects
        });
    }

}
