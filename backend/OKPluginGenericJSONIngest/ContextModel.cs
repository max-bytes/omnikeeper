using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.TraitBased;

namespace OKPluginGenericJSONIngest
{
    public class ContextModel : GenericTraitEntityModel<Context, string>
    {
        public ContextModel(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel) : base(effectiveTraitModel, ciModel, attributeModel, relationModel, null)
        {
        }
    }
}
