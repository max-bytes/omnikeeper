using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.TraitBased;

namespace OKPluginValidation
{
    public class ValidationIssueModel : GenericTraitEntityModel<ValidationIssue, string>
    {
        public ValidationIssueModel(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel, IChangesetModel changesetModel) : 
            base(effectiveTraitModel, ciModel, attributeModel, relationModel, changesetModel)
        {
        }
    }
}
