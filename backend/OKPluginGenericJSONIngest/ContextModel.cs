using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using OKPluginGenericJSONIngest.Extract;
using OKPluginGenericJSONIngest.Load;
using OKPluginGenericJSONIngest.Transform;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Utils;
using System;

namespace OKPluginGenericJSONIngest
{
    public class ContextModel : GenericTraitEntityModel<Context, string>
    {
        public ContextModel(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel) : base(effectiveTraitModel, ciModel, attributeModel, relationModel)
        {
        }
        public class ExtractConfigSerializer : IAttributeJSONSerializer
        {
            private readonly NewtonSoftJSONSerializer<IExtractConfig> serializer = new NewtonSoftJSONSerializer<IExtractConfig>(() =>
            {
                var s = new JsonSerializerSettings()
                {
                    TypeNameHandling = TypeNameHandling.Objects
                };
                s.Converters.Add(new StringEnumConverter());
                return s;
            });

            public object Deserialize(JToken jo, Type type)
            {
                return serializer.Deserialize(jo, type);
            }

            public JObject SerializeToJObject(object o)
            {
                return serializer.SerializeToJObject(o);
            }
        }

        public class TransformConfigSerializer : IAttributeJSONSerializer
        {
            private readonly NewtonSoftJSONSerializer<ITransformConfig> serializer = new NewtonSoftJSONSerializer<ITransformConfig>(() =>
            {
                var s = new JsonSerializerSettings()
                {
                    TypeNameHandling = TypeNameHandling.Objects
                };
                s.Converters.Add(new StringEnumConverter());
                return s;
            });

            public object Deserialize(JToken jo, Type type)
            {
                return serializer.Deserialize(jo, type);
            }

            public JObject SerializeToJObject(object o)
            {
                return serializer.SerializeToJObject(o);
            }
        }

        public class LoadConfigSerializer : IAttributeJSONSerializer
        {
            private readonly NewtonSoftJSONSerializer<ILoadConfig> serializer = new NewtonSoftJSONSerializer<ILoadConfig>(() =>
            {
                var s = new JsonSerializerSettings()
                {
                    TypeNameHandling = TypeNameHandling.Objects
                };
                s.Converters.Add(new StringEnumConverter());
                return s;
            });

            public object Deserialize(JToken jo, Type type)
            {
                return serializer.Deserialize(jo, type);
            }

            public JObject SerializeToJObject(object o)
            {
                return serializer.SerializeToJObject(o);
            }
        }
    }
}
