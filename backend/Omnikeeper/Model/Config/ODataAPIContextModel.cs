using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.TraitBased;

namespace Omnikeeper.Model.Config
{
    public class ODataAPIContextModel : GenericTraitEntityModel<ODataAPIContext, string>
    {
        public ODataAPIContextModel(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel)
            : base(effectiveTraitModel, ciModel, attributeModel, relationModel)
        {
        }
    }
}
