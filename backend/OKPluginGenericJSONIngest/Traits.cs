using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Plugins;
using Omnikeeper.Base.Service;
using Omnikeeper.Entity.AttributeValues;
using System.Collections.Generic;

namespace OKPluginGenericJSONIngest
{
    public static class Traits
    {
        private static readonly TraitOriginV1 traitOrigin = PluginRegistrationBase.GetTraitOrigin(typeof(Traits).Assembly);

        public static readonly RecursiveTrait Context = new RecursiveTrait(null, "__meta.config.gji_context", traitOrigin, 
            new List<TraitAttribute>() {
                new TraitAttribute("id", CIAttributeTemplate.BuildFromParams("gji_context.id", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null), new CIAttributeValueConstraintTextRegex(OKPluginGenericJSONIngest.Context.ContextIDRegex))),
                new TraitAttribute("extract_config", CIAttributeTemplate.BuildFromParams("gji_context.extract_config", AttributeValueType.JSON, false)),
                new TraitAttribute("transform_config", CIAttributeTemplate.BuildFromParams("gji_context.transform_config", AttributeValueType.JSON, false)),
                new TraitAttribute("load_config", CIAttributeTemplate.BuildFromParams("gji_context.load_config", AttributeValueType.JSON, false))
            },
            new List<TraitAttribute>()
            {
                new TraitAttribute("name", CIAttributeTemplate.BuildFromParams(ICIModel.NameAttribute, AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
            });
        public static readonly GenericTrait ContextFlattenedTrait = RecursiveTraitService.FlattenSingleRecursiveTrait(Context);

        public static readonly IEnumerable<RecursiveTrait> RecursiveTraits = new List<RecursiveTrait>() { Context };
    }
}
