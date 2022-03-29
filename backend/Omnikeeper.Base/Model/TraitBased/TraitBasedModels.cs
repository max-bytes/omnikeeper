using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Generator;
using Omnikeeper.Base.Utils;

namespace Omnikeeper.Base.Model.TraitBased
{
    public class RecursiveTraitModel : GenericTraitEntityModel<RecursiveTrait, string>
    {
        public RecursiveTraitModel(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel) 
            : base(effectiveTraitModel, ciModel, attributeModel, relationModel, serializer)
        {
        }

        private static readonly NewtonSoftJSONSerializer<object> serializer = new NewtonSoftJSONSerializer<object>(() =>
        {
            var s = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Objects
            };
            s.Converters.Add(new StringEnumConverter());
            return s;
        });
    }

    public class InnerLayerDataModel : GenericTraitEntityModel<LayerData, string>
    {
        public InnerLayerDataModel(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel) : base(effectiveTraitModel, ciModel, attributeModel, relationModel, null)
        {
        }
    }

    public class PredicateModel : GenericTraitEntityModel<Predicate, string>
    {
        public PredicateModel(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel) : base(effectiveTraitModel, ciModel, attributeModel, relationModel, null)
        {
        }
    }

    public class GeneratorV1Model : GenericTraitEntityModel<GeneratorV1, string>
    {
        public GeneratorV1Model(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel) : base(effectiveTraitModel, ciModel, attributeModel, relationModel, null)
        {
        }
    }

    public class CLConfigV1Model : GenericTraitEntityModel<CLConfigV1, string>
    {
        public CLConfigV1Model(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel) : base(effectiveTraitModel, ciModel, attributeModel, relationModel, null)
        {
        }
    }

    public class AuthRoleModel : GenericTraitEntityModel<AuthRole, string>
    {
        public AuthRoleModel(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel) : base(effectiveTraitModel, ciModel, attributeModel, relationModel, null)
        {
        }
    }
}
