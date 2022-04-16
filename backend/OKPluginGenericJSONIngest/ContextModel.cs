using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using OKPluginGenericJSONIngest.Extract;
using OKPluginGenericJSONIngest.Load;
using OKPluginGenericJSONIngest.Transform;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.TraitBased;
using System.Text.Json.Serialization;

namespace OKPluginGenericJSONIngest
{
    public class ContextModel : GenericTraitEntityModel<Context, string>
    {
        public ContextModel(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel) : base(effectiveTraitModel, ciModel, attributeModel, relationModel)
        {
        }
        public class ExtractConfigSerializer : AttributeJSONSerializer<IExtractConfig>
        {
            public ExtractConfigSerializer() : base(() =>
            {
                return new System.Text.Json.JsonSerializerOptions()
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

        public class TransformConfigSerializer : AttributeJSONSerializer<ITransformConfig>
        {
            public TransformConfigSerializer() : base(() =>
            {
                return new System.Text.Json.JsonSerializerOptions()
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

        public class LoadConfigSerializer : AttributeJSONSerializer<ILoadConfig>
        {
            public LoadConfigSerializer() : base(() =>
            {
                return new System.Text.Json.JsonSerializerOptions()
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
    }
}
