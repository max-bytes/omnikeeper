﻿using System.Collections.Generic;
using System.Collections.Immutable;

namespace Landscape.Base.Entity
{
    public class TraitRelation
    {
        public RelationTemplate RelationTemplate { get; private set; }
        // TODO: implement anyOf(RelationTemplate[])
        public string Identifier { get; private set; }

        public static TraitRelation Build(string identifier, RelationTemplate relationTemplate)
        {
            return new TraitRelation()
            {
                Identifier = identifier,
                RelationTemplate = relationTemplate
            };
        }
    }
    public class TraitAttribute
    {
        public CIAttributeTemplate AttributeTemplate { get; private set; }
        public string Identifier { get; private set; }

        // TODO: implement anyOf(CIAttributeTemplate[])

        public static TraitAttribute Build(string identifier, CIAttributeTemplate attributeTemplate)
        {
            return new TraitAttribute()
            {
                Identifier = identifier,
                AttributeTemplate = attributeTemplate
            };
        }
    }

    public class Trait
    {
        public string Name { get; private set; }

        public IImmutableList<TraitAttribute> RequiredAttributes { get; private set; }
        public IImmutableList<TraitAttribute> OptionalAttributes { get; private set; }

        public IImmutableList<string> RequiredTraits { get; private set; }

        public ImmutableList<TraitRelation> RequiredRelations { get; private set; }
        // TODO: implement optional relations

        public static Trait Build(string name, 
            IEnumerable<TraitAttribute> requiredAttributes = null, 
            IEnumerable<TraitAttribute> optionalAttributes = null,
            IEnumerable<TraitRelation> requiredRelations = null,
            IEnumerable<string> requiredTraits = null)
        {
            return new Trait()
            {
                Name = name,
                RequiredAttributes = requiredAttributes?.ToImmutableList() ?? ImmutableList<TraitAttribute>.Empty,
                OptionalAttributes = optionalAttributes?.ToImmutableList() ?? ImmutableList<TraitAttribute>.Empty,
                RequiredRelations = requiredRelations?.ToImmutableList() ?? ImmutableList<TraitRelation>.Empty,
                RequiredTraits = requiredTraits?.ToImmutableList() ?? ImmutableList<string>.Empty
            };
        }
    }
}
