using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.Config;
using Omnikeeper.Base.Generator;
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
        public static readonly RecursiveTrait Named = new RecursiveTrait("named", new TraitOriginV1(TraitOriginType.Core), new List<TraitAttribute>() {
            new TraitAttribute("name",
                CIAttributeTemplate.BuildFromParams(ICIModel.NameAttribute, AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
            )
        });

        public static readonly IEnumerable<RecursiveTrait> RecursiveTraits = new List<RecursiveTrait>() { 
            Named,
            TraitEntityHelper.Class2RecursiveTrait<BaseConfigurationV2>(),
            TraitEntityHelper.Class2RecursiveTrait<RecursiveTrait>(),
            TraitEntityHelper.Class2RecursiveTrait<Predicate>(),
            TraitEntityHelper.Class2RecursiveTrait<AuthRole>(),
            TraitEntityHelper.Class2RecursiveTrait<GeneratorV1>(),
            //TraitBuilderFromClass.Class2RecursiveTrait<GridViewContext>(), // TODO: add?
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
