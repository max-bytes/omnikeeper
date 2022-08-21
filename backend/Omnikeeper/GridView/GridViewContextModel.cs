using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.GridView.Entity;
using System.Text.Json.Serialization;

namespace Omnikeeper.GridView
{
    public class GridViewContextModel : GenericTraitEntityModel<GridViewContext, string>
    {
        public GridViewContextModel(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel, IChangesetModel changesetModel) 
            : base(effectiveTraitModel, ciModel, attributeModel, relationModel, changesetModel)
        {
        }

        public class ConfigSerializer : AttributeJSONSerializer<GridViewConfiguration>
        {
            public ConfigSerializer() : base(() =>
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
