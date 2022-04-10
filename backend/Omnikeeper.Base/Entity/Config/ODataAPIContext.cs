﻿
using Newtonsoft.Json;
using Omnikeeper.Base.Utils;

namespace Omnikeeper.Base.Entity
{
    //[ProtoContract(SkipConstructor = true)]
    public class ODataAPIContext
    {
        //[ProtoContract]
        //[ProtoInclude(1, typeof(ConfigV3))]
        public interface IConfig { }

        //[ProtoContract(SkipConstructor = true)]
        public class ConfigV3 : IConfig
        {
            public ConfigV3(string writeLayerID, string[] readLayerset)
            {
                WriteLayerID = writeLayerID;
                ReadLayerset = readLayerset;
            }

            //[ProtoMember(1)] 
            public string WriteLayerID { get; set; }
            //[ProtoMember(2)] 
            public string[] ReadLayerset { get; set; }
        }

        //[ProtoMember(1)] 
        public string ID { get; set; }
        //[ProtoMember(2)] 
        public IConfig CConfig { get; set; }

        public static NewtonSoftJSONSerializer<IConfig> ConfigSerializer = new NewtonSoftJSONSerializer<IConfig>(new JsonSerializerSettings()
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
