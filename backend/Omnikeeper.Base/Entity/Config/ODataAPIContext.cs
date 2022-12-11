using Omnikeeper.Base.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Omnikeeper.Base.Entity
{
    [TraitEntity("__meta.config.odata_context", TraitOriginType.Core)]
    public class ODataAPIContext : TraitEntity
    {
        [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
        [JsonDerivedType(typeof(ConfigV3), typeDiscriminator: "Omnikeeper.Base.Entity.ConfigV3, Omnikeeper.Base")]
        [JsonDerivedType(typeof(ConfigV4), typeDiscriminator: "ConfigV4")]
        public interface IConfig
        {
        }

        public class ConfigV3 : IConfig
        {
            public ConfigV3(string writeLayerID, string[] readLayerset)
            {
                WriteLayerID = writeLayerID;
                ReadLayerset = readLayerset;
            }

            public string WriteLayerID { get; set; }
            public string[] ReadLayerset { get; set; }
        }

        [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
        [JsonDerivedType(typeof(ContextAuthNone), typeDiscriminator: "ContextAuthNone")]
        [JsonDerivedType(typeof(ContextAuthBasic), typeDiscriminator: "ContextAuthBasic")]
        public interface IContextAuth
        {
        }

        public class ContextAuthNone : IContextAuth
        {
        }

        public class ContextAuthBasic : IContextAuth
        {
            public ContextAuthBasic(string username, string passwordHashed)
            {
                Username = username;
                PasswordHashed = passwordHashed;
            }

            public string Username { get; set; }
            public string PasswordHashed { get; set; }
        }

        public class ConfigV4 : IConfig
        {
            public ConfigV4(string writeLayerID, string[] readLayerset, IContextAuth? contextAuth)
            {
                WriteLayerID = writeLayerID;
                ReadLayerset = readLayerset;
                // workaround to enforce field exists in JSON taken from https://stackoverflow.com/a/66575565
                ContextAuth = contextAuth ?? throw new ArgumentNullException(nameof(contextAuth));
            }

            public string WriteLayerID { get; set; }
            public string[] ReadLayerset { get; set; }
            [NotNull]
            public IContextAuth? ContextAuth { get; set; }
        }

        [TraitAttribute("id", "odata_context.id")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        [TraitEntityID]
        public string ID;

        [TraitAttribute("config", "odata_context.config", jsonSerializer: typeof(ConfigSerializerV4))]
        public IConfig CConfig;

        [TraitAttribute("name", "__name", optional: true)]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        public string Name;

        public static SystemTextJSONSerializer<IConfig> ConfigSerializer = new SystemTextJSONSerializer<IConfig>(new JsonSerializerOptions());

        public ODataAPIContext(string iD, IConfig cConfig)
        {
            ID = iD;
            CConfig = cConfig;
            Name = $"OData-Context - {iD}";
        }
        public ODataAPIContext() { 
            ID = ""; 
            CConfig = new ConfigV3("", System.Array.Empty<string>());
            Name = "";
        }
    }

    public class ConfigSerializerV3 : AttributeJSONSerializer<ODataAPIContext.ConfigV3>
    {
        public ConfigSerializerV3() : base(() =>
        {
            return new JsonSerializerOptions()
            {
                Converters = {
                        new JsonStringEnumConverter()
                    },
                IncludeFields = true
            };
        })
        {
        }
    }

    // supports V3 and V4
    public class ConfigSerializerV4 : AttributeJSONSerializer<ODataAPIContext.ConfigV4>
    {
        private readonly ConfigSerializerV3 V3;
        public ConfigSerializerV4() : base(() =>
        {
            return new JsonSerializerOptions()
            {
                Converters = { new JsonStringEnumConverter() }
            };
        })
        {
            V3 = new ConfigSerializerV3();
        }

        public override ODataAPIContext.ConfigV4 Deserialize(string s)
        {
            // try V4 first
            try
            {
                return base.Deserialize(s);
            } catch (Exception)
            {
                var v3 = V3.Deserialize(s);

                // convert v3 to v4
                return new ODataAPIContext.ConfigV4(v3.WriteLayerID, v3.ReadLayerset, new ODataAPIContext.ContextAuthNone());
            }
        }
    }

}
