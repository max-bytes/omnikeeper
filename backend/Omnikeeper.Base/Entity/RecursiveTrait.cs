using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Omnikeeper.Base.Utils;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Omnikeeper.Base.Entity
{
    public class TraitRelation
    {
        public RelationTemplate RelationTemplate { get; set; }
        // TODO: implement anyOf(RelationTemplate[])
        public string Identifier { get; set; }

        public TraitRelation(string identifier, RelationTemplate relationTemplate)
        {
            Identifier = identifier;
            RelationTemplate = relationTemplate;
        }
    }
    public class TraitAttribute
    {
        public CIAttributeTemplate AttributeTemplate { get; set; }
        public string Identifier { get; set; }

        // TODO: implement anyOf(CIAttributeTemplate[])

        public TraitAttribute(string identifier, CIAttributeTemplate attributeTemplate)
        {
            Identifier = identifier;
            AttributeTemplate = attributeTemplate;
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

        public RecursiveTrait(string name,
            IEnumerable<TraitAttribute>? requiredAttributes = null,
            IEnumerable<TraitAttribute>? optionalAttributes = null,
            IEnumerable<TraitRelation>? requiredRelations = null,
            IEnumerable<string>? requiredTraits = null)
        {
            Name = name;
            RequiredAttributes = requiredAttributes?.ToImmutableList() ?? ImmutableList<TraitAttribute>.Empty;
            OptionalAttributes = optionalAttributes?.ToImmutableList() ?? ImmutableList<TraitAttribute>.Empty;
            RequiredRelations = requiredRelations?.ToImmutableList() ?? ImmutableList<TraitRelation>.Empty;
            RequiredTraits = requiredTraits?.ToImmutableList() ?? ImmutableList<string>.Empty;
        }

    }

    public class Trait
    {
        private Trait(string name, IImmutableList<TraitAttribute> requiredAttributes, IImmutableList<TraitAttribute> optionalAttributes, ImmutableList<TraitRelation> requiredRelations)
        {
            Name = name;
            RequiredAttributes = requiredAttributes;
            OptionalAttributes = optionalAttributes;
            RequiredRelations = requiredRelations;
        }

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
            return new Trait(name, 
                requiredAttributes.ToImmutableList(), optionalAttributes.ToImmutableList(), requiredRelations.ToImmutableList()
            );
        }
    }

    public class RecursiveTraitSet
    {
        [JsonConstructor]
        private RecursiveTraitSet(IImmutableDictionary<string, RecursiveTrait> traits)
        {
            Traits = traits;
        }

        public IImmutableDictionary<string, RecursiveTrait> Traits { get; set; }

        public static RecursiveTraitSet Build(IEnumerable<RecursiveTrait> traits)
        {
            return new RecursiveTraitSet(traits.ToImmutableDictionary(t => t.Name));
        }
        public static RecursiveTraitSet Build(params RecursiveTrait[] traits)
        {
            return new RecursiveTraitSet(traits.ToImmutableDictionary(t => t.Name));
        }

        public static MyJSONSerializer<RecursiveTraitSet> Serializer = new MyJSONSerializer<RecursiveTraitSet>(() =>
        {
            var s = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Objects
            };
            s.Converters.Add(new StringEnumConverter());
            return s;
        });
    }

    public class TraitSet
    {
        private TraitSet(IImmutableDictionary<string, Trait> traits)
        {
            Traits = traits;
        }

        public IImmutableDictionary<string, Trait> Traits { get; set; }

        public static TraitSet Build(IEnumerable<Trait> traits)
        {
            return new TraitSet(traits.ToImmutableDictionary(t => t.Name));
        }
        public static TraitSet Build(params Trait[] traits)
        {
            return new TraitSet(traits.ToImmutableDictionary(t => t.Name));
        }
    }
}
