﻿using Omnikeeper.Base.Entity;
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
            new List<TraitAttribute>() {
                new TraitAttribute("id", CIAttributeTemplate.BuildFromParams("trait.id", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
                new TraitAttribute("requiredAttributes", CIAttributeTemplate.BuildFromParams("trait.requiredAttributes", AttributeValueType.JSON, true, new CIAttributeValueConstraintArrayLength(1, null)))
            },
            new List<TraitAttribute>()
            {
                new TraitAttribute("requiredTraits", CIAttributeTemplate.BuildFromParams("trait.requiredTraits", AttributeValueType.Text, true)),
                new TraitAttribute("optionalAttributes", CIAttributeTemplate.BuildFromParams("trait.optionalAttributes", AttributeValueType.JSON, true)),
                new TraitAttribute("requiredRelations", CIAttributeTemplate.BuildFromParams("trait.requiredRelations", AttributeValueType.JSON, true)),
                new TraitAttribute("name", CIAttributeTemplate.BuildFromParams("__name", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
            }
        );

        public static readonly RecursiveTrait Predicate = new RecursiveTrait("__meta.config.predicate", new TraitOriginV1(TraitOriginType.Core),
            new List<TraitAttribute>() {
                new TraitAttribute("id", CIAttributeTemplate.BuildFromParams("predicate.id", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
                new TraitAttribute("wordingFrom", CIAttributeTemplate.BuildFromParams("predicate.wordingFrom", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
                new TraitAttribute("wordingTo", CIAttributeTemplate.BuildFromParams("predicate.wordingTo", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
            },
            new List<TraitAttribute>()
            {
                new TraitAttribute("name", CIAttributeTemplate.BuildFromParams("__name", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
                // TODO: constraints
            }
        );

        public static readonly IEnumerable<RecursiveTrait> Traits = new List<RecursiveTrait>() { Named, Trait, Predicate };
    }


    public class TraitEmpty : ITrait
    {
        public string Name => "empty";

        public IImmutableSet<string> AncestorTraits => ImmutableHashSet<string>.Empty;

        public TraitOriginV1 Origin => new TraitOriginV1(TraitOriginType.Core);
    }
}
