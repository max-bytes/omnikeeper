using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Model
{
    public static class CoreTraits
    {
        private static readonly RecursiveTrait Named = new RecursiveTrait("named", new TraitOriginV1(TraitOriginType.Core), new List<TraitAttribute>() {
            new TraitAttribute("name",
                CIAttributeTemplate.BuildFromParams("__name", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
            )
        });

        public static readonly RecursiveTrait Trait = new RecursiveTrait("__meta.config.trait", new TraitOriginV1(TraitOriginType.Core), 
            new List<TraitAttribute>() {// TODO: use snake_case for names and attributes
                new TraitAttribute("id", CIAttributeTemplate.BuildFromParams("trait.id", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null), new CIAttributeValueConstraintTextRegex(IDValidations.TraitIDRegex))),
                new TraitAttribute("requiredAttributes", CIAttributeTemplate.BuildFromParams("trait.requiredAttributes", AttributeValueType.JSON, true, new CIAttributeValueConstraintArrayLength(1, null)))
            },
            new List<TraitAttribute>()
            {// TODO: use snake_case for names and attributes
                new TraitAttribute("requiredTraits", CIAttributeTemplate.BuildFromParams("trait.requiredTraits", AttributeValueType.Text, true)),
                new TraitAttribute("optionalAttributes", CIAttributeTemplate.BuildFromParams("trait.optionalAttributes", AttributeValueType.JSON, true)),
                new TraitAttribute("requiredRelations", CIAttributeTemplate.BuildFromParams("trait.requiredRelations", AttributeValueType.JSON, true)),
                new TraitAttribute("name", CIAttributeTemplate.BuildFromParams("__name", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
            }
        );
        public static readonly GenericTrait TraitFlattened = RecursiveTraitService.FlattenSingleRecursiveTrait(Trait);

        public static readonly RecursiveTrait Predicate = new RecursiveTrait("__meta.config.predicate", new TraitOriginV1(TraitOriginType.Core),
            new List<TraitAttribute>() { // TODO: use snake_case for names and attributes
                new TraitAttribute("id", CIAttributeTemplate.BuildFromParams("predicate.id", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null), new CIAttributeValueConstraintTextRegex(IDValidations.PredicateIDRegex))),
                new TraitAttribute("wordingFrom", CIAttributeTemplate.BuildFromParams("predicate.wordingFrom", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
                new TraitAttribute("wordingTo", CIAttributeTemplate.BuildFromParams("predicate.wordingTo", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
            },
            new List<TraitAttribute>()
            {
                new TraitAttribute("name", CIAttributeTemplate.BuildFromParams("__name", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
                // TODO: constraints
            }
        );
        public static readonly GenericTrait PredicateFlattened = RecursiveTraitService.FlattenSingleRecursiveTrait(Predicate);

        public static readonly RecursiveTrait AuthRole = new RecursiveTrait("__meta.config.auth_role", new TraitOriginV1(TraitOriginType.Core),
            new List<TraitAttribute>() {// TODO: use snake_case for names and attributes
                new TraitAttribute("id", CIAttributeTemplate.BuildFromParams("authRole.id", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
            },
            new List<TraitAttribute>()
            {
                new TraitAttribute("name", CIAttributeTemplate.BuildFromParams("__name", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
                new TraitAttribute("permissions", CIAttributeTemplate.BuildFromParams("authRole.permissions", AttributeValueType.Text, true)),
            }
        );
        public static readonly GenericTrait AuthRoleFlattened = RecursiveTraitService.FlattenSingleRecursiveTrait(AuthRole);

        // TODO: move to plugin, once that exists
        public static readonly RecursiveTrait GridviewContext = new RecursiveTrait("__meta.config.gridview_context", new TraitOriginV1(TraitOriginType.Core),
            new List<TraitAttribute>() {
                new TraitAttribute("id", CIAttributeTemplate.BuildFromParams("gridview_context.id", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
                new TraitAttribute("config", CIAttributeTemplate.BuildFromParams("gridview_context.config", AttributeValueType.JSON, false)),
            },
            new List<TraitAttribute>()
            {
                new TraitAttribute("name", CIAttributeTemplate.BuildFromParams("__name", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
                new TraitAttribute("speaking_name", CIAttributeTemplate.BuildFromParams("gridview_context.speaking_name", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
                new TraitAttribute("description", CIAttributeTemplate.BuildFromParams("gridview_context.description", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
            }
        );
        public static readonly GenericTrait GridviewContextFlattened = RecursiveTraitService.FlattenSingleRecursiveTrait(GridviewContext);

        public static readonly IEnumerable<RecursiveTrait> RecursiveTraits = new List<RecursiveTrait>() { Named, Trait, Predicate, AuthRole, GridviewContext };
    }


    public class TraitEmpty : ITrait
    {
        public string ID => "empty";

        public IImmutableSet<string> AncestorTraits => ImmutableHashSet<string>.Empty;

        public TraitOriginV1 Origin => new TraitOriginV1(TraitOriginType.Core);
    }
}
