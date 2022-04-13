using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Utils;
using Omnikeeper.GridView.Entity;
using System;

namespace Omnikeeper.GridView
{
    public class GridViewContextModel : GenericTraitEntityModel<GridViewContext, string>
    {
        public GridViewContextModel(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel) : base(effectiveTraitModel, ciModel, attributeModel, relationModel)
        {
        }

        public class ConfigSerializer : IAttributeJSONSerializer
        {
            private readonly NewtonSoftJSONSerializer<GridViewConfiguration> serializer = new NewtonSoftJSONSerializer<GridViewConfiguration>(() =>
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
}
