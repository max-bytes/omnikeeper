using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Generator;
using System.Text.Json.Serialization;

namespace Omnikeeper.Base.Model.TraitBased
{
    public class RecursiveTraitModel : GenericTraitEntityModel<RecursiveTrait, string>
    {
        public RecursiveTraitModel(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel)
            : base(effectiveTraitModel, ciModel, attributeModel, relationModel)
        {
        }

        public class TraitAttributeSerializer : AttributeJSONSerializer<TraitAttribute>
        {
            public TraitAttributeSerializer() : base(() =>
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

        public class TraitRelationSerializer : AttributeJSONSerializer<TraitRelation>
        {
            public TraitRelationSerializer() : base(() =>
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

    public class ValidatorContextV1Model : GenericTraitEntityModel<ValidatorContextV1, string>
    {
        public ValidatorContextV1Model(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel) : base(effectiveTraitModel, ciModel, attributeModel, relationModel)
        {
        }
    }

    public class AuthRoleModel : GenericTraitEntityModel<AuthRole, string>
    {
        public AuthRoleModel(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel) : base(effectiveTraitModel, ciModel, attributeModel, relationModel)
        {
        }
    }

    public class ChangesetDataModel : GenericTraitEntityModel<ChangesetData, string>
    {
        public ChangesetDataModel(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel) : base(effectiveTraitModel, ciModel, attributeModel, relationModel)
        {
        }
    }
}
