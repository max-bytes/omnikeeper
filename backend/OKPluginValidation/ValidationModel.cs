using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.TraitBased;

namespace OKPluginValidation
{
    public class ValidationModel : GenericTraitEntityModel<Validation, string>
    {
        public ValidationModel(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel) : base(effectiveTraitModel, ciModel, attributeModel, relationModel)
        {
        }
    }
}
