using System.Collections.Generic;
using System.Collections.Immutable;

namespace Omnikeeper.Base.Entity
{
    public class TraitRelation
    {
        public RelationTemplate RelationTemplate { get; set; }
        // TODO: implement anyOf(RelationTemplate[])
        public string Identifier { get; set; }

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
        public CIAttributeTemplate AttributeTemplate { get; set; }
        public string Identifier { get; set; }

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

    public class RecursiveTrait
    {
        public string Name { get; set; }

        public IImmutableList<TraitAttribute> RequiredAttributes { get; set; }
        public IImmutableList<TraitAttribute> OptionalAttributes { get; set; }

        public IImmutableList<string> RequiredTraits { get; set; }

        public ImmutableList<TraitRelation> RequiredRelations { get; set; }
        // TODO: implement optional relations

        public static RecursiveTrait Build(string name,
            IEnumerable<TraitAttribute> requiredAttributes = null,
            IEnumerable<TraitAttribute> optionalAttributes = null,
            IEnumerable<TraitRelation> requiredRelations = null,
            IEnumerable<string> requiredTraits = null)
        {
            return new RecursiveTrait()
            {
                Name = name,
                RequiredAttributes = requiredAttributes?.ToImmutableList() ?? ImmutableList<TraitAttribute>.Empty,
                OptionalAttributes = optionalAttributes?.ToImmutableList() ?? ImmutableList<TraitAttribute>.Empty,
                RequiredRelations = requiredRelations?.ToImmutableList() ?? ImmutableList<TraitRelation>.Empty,
                RequiredTraits = requiredTraits?.ToImmutableList() ?? ImmutableList<string>.Empty
            };
        }
    }

    public class Trait
    {
        public string Name { get; set; }

        public IImmutableList<TraitAttribute> RequiredAttributes { get; set; }
        public IImmutableList<TraitAttribute> OptionalAttributes { get; set; }
        public ImmutableList<TraitRelation> RequiredRelations { get; set; }
        // TODO: implement optional relations

        public static Trait Build(string name,
            IEnumerable<TraitAttribute> requiredAttributes,
            IEnumerable<TraitAttribute> optionalAttributes,
            IEnumerable<TraitRelation> requiredRelations)
        {
            return new Trait()
            {
                Name = name,
                RequiredAttributes = requiredAttributes?.ToImmutableList(),
                OptionalAttributes = optionalAttributes?.ToImmutableList(),
                RequiredRelations = requiredRelations?.ToImmutableList()
            };
        }
    }

    public class RecursiveTraitSet
    {
        public IImmutableDictionary<string, RecursiveTrait> Traits { get; set; }

        public static RecursiveTraitSet Build(IEnumerable<RecursiveTrait> traits)
        {
            return new RecursiveTraitSet()
            {
                Traits = traits.ToImmutableDictionary(t => t.Name)
            };
        }
        public static RecursiveTraitSet Build(params RecursiveTrait[] traits)
        {
            return new RecursiveTraitSet()
            {
                Traits = traits.ToImmutableDictionary(t => t.Name)
            };
        }
    }

    public class TraitSet
    {
        public IImmutableDictionary<string, Trait> Traits { get; set; }

        public static TraitSet Build(IEnumerable<Trait> traits)
        {
            return new TraitSet()
            {
                Traits = traits.ToImmutableDictionary(t => t.Name)
            };
        }
        public static TraitSet Build(params Trait[] traits)
        {
            return new TraitSet()
            {
                Traits = traits.ToImmutableDictionary(t => t.Name)
            };
        }
    }
}
