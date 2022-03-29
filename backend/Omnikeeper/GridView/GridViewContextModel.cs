using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Utils;
using Omnikeeper.GridView.Entity;

namespace Omnikeeper.GridView
{
    public class GridViewContextModel : GenericTraitEntityModel<GridViewContext, string>
    {
        public GridViewContextModel(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel) : base(effectiveTraitModel, ciModel, attributeModel, relationModel, serializer)
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
}
