using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Generator;
using Omnikeeper.Base.Utils;
using System;

namespace Omnikeeper.Base.Model.TraitBased
{
    public class RecursiveTraitModel : GenericTraitEntityModel<RecursiveTrait, string>
    {
        public RecursiveTraitModel(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel) 
            : base(effectiveTraitModel, ciModel, attributeModel, relationModel)
        {
        }

        public class TraitAttributeSerializer : IAttributeJSONSerializer
        {
            private readonly NewtonSoftJSONSerializer<TraitAttribute> serializer = new NewtonSoftJSONSerializer<TraitAttribute>(() =>
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

        public class TraitRelationSerializer : IAttributeJSONSerializer
        {
            private readonly NewtonSoftJSONSerializer<TraitRelation> serializer = new NewtonSoftJSONSerializer<TraitRelation>(() =>
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

    public class InnerLayerDataModel : GenericTraitEntityModel<LayerData, string>
    {
        public InnerLayerDataModel(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel) : base(effectiveTraitModel, ciModel, attributeModel, relationModel)
        {
        }
    }

    public class PredicateModel : GenericTraitEntityModel<Predicate, string>
    {
        public PredicateModel(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel) : base(effectiveTraitModel, ciModel, attributeModel, relationModel)
        {
        }
    }

    public class GeneratorV1Model : GenericTraitEntityModel<GeneratorV1, string>
    {
        public GeneratorV1Model(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel) : base(effectiveTraitModel, ciModel, attributeModel, relationModel)
        {
        }
    }

    public class CLConfigV1Model : GenericTraitEntityModel<CLConfigV1, string>
    {
        public CLConfigV1Model(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel) : base(effectiveTraitModel, ciModel, attributeModel, relationModel)
        {
        }
    }

    public class AuthRoleModel : GenericTraitEntityModel<AuthRole, string>
    {
        public AuthRoleModel(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel) : base(effectiveTraitModel, ciModel, attributeModel, relationModel)
        {
        }
    }
}
