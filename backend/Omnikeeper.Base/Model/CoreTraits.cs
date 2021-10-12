using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Omnikeeper.Base.Model
{
    public static class CoreTraits
    {
        public static readonly RecursiveTrait Named = new RecursiveTrait(null, "named", new TraitOriginV1(TraitOriginType.Core), new List<TraitAttribute>() {
            new TraitAttribute("name",
                CIAttributeTemplate.BuildFromParams(ICIModel.NameAttribute, AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
            )
        });

        // TODO: move to plugin, once that exists
        public static readonly RecursiveTrait GridviewContext = new RecursiveTrait(null, "__meta.config.gridview_context", new TraitOriginV1(TraitOriginType.Core),
            new List<TraitAttribute>() {
                new TraitAttribute("id", CIAttributeTemplate.BuildFromParams("gridview_context.id", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null), new CIAttributeValueConstraintTextRegex(IDValidations.GridViewContextIDRegex))),
                new TraitAttribute("config", CIAttributeTemplate.BuildFromParams("gridview_context.config", AttributeValueType.JSON, false)),
            },
            new List<TraitAttribute>()
            {
                new TraitAttribute("speaking_name", CIAttributeTemplate.BuildFromParams("gridview_context.speaking_name", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
                new TraitAttribute("description", CIAttributeTemplate.BuildFromParams("gridview_context.description", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
                new TraitAttribute("name", CIAttributeTemplate.BuildFromParams(ICIModel.NameAttribute, AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
            }
        );
        public static readonly GenericTrait GridviewContextFlattened = RecursiveTraitService.FlattenSingleRecursiveTrait(GridviewContext);

        public static readonly IEnumerable<RecursiveTrait> RecursiveTraits = new List<RecursiveTrait>() { 
            Named,
            BaseConfigurationModel.Trait,
            TraitBuilderFromClass.Class2RecursiveTrait<RecursiveTrait>(),
            TraitBuilderFromClass.Class2RecursiveTrait<Predicate>(), 
            TraitBuilderFromClass.Class2RecursiveTrait<AuthRole>(), GeneratorModel.Generator,
            GridviewContext,
        };
    }


    public class TraitEmpty : ITrait
    {
        public static string StaticID => "empty";

        public string ID => StaticID;

        public IImmutableSet<string> AncestorTraits => ImmutableHashSet<string>.Empty;

        public TraitOriginV1 Origin => new TraitOriginV1(TraitOriginType.Core);

        public IImmutableList<TraitAttribute> RequiredAttributes { get => ImmutableList<TraitAttribute>.Empty;  }
        public IImmutableList<TraitAttribute> OptionalAttributes { get => ImmutableList<TraitAttribute>.Empty; }
        public IImmutableList<TraitRelation> RequiredRelations { get => ImmutableList<TraitRelation>.Empty; }
        public IImmutableList<TraitRelation> OptionalRelations { get => ImmutableList<TraitRelation>.Empty; }
    }
}
