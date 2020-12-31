using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Omnikeeper.Base.Utils;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Omnikeeper.Base.Entity
{
    [Serializable]
    public class TraitRelation
    {
        public readonly RelationTemplate RelationTemplate;
        // TODO: implement anyOf(RelationTemplate[])
        public readonly string Identifier;

        public TraitRelation(string identifier, RelationTemplate relationTemplate)
        {
            Identifier = identifier;
            RelationTemplate = relationTemplate;
        }
    }
    [Serializable]
    public class TraitAttribute
    {
        public readonly CIAttributeTemplate AttributeTemplate;
        public readonly string Identifier;

        // TODO: implement anyOf(CIAttributeTemplate[])

        public TraitAttribute(string identifier, CIAttributeTemplate attributeTemplate)
        {
            Identifier = identifier;
            AttributeTemplate = attributeTemplate;
        }
    }

    [Serializable]
    public class RecursiveTrait
    {
        public readonly string Name;
        public readonly TraitAttribute[] RequiredAttributes;
        public readonly TraitAttribute[] OptionalAttributes;
        public readonly string[] RequiredTraits;
        public readonly TraitRelation[] RequiredRelations;
        // TODO: implement optional relations

        public RecursiveTrait(string name,
            IEnumerable<TraitAttribute>? requiredAttributes = null,
            IEnumerable<TraitAttribute>? optionalAttributes = null,
            IEnumerable<TraitRelation>? requiredRelations = null,
            IEnumerable<string>? requiredTraits = null)
        {
            Name = name;
            RequiredAttributes = requiredAttributes?.ToArray() ?? new TraitAttribute[0];
            OptionalAttributes = optionalAttributes?.ToArray() ?? new TraitAttribute[0];
            RequiredRelations = requiredRelations?.ToArray() ?? new TraitRelation[0];
            RequiredTraits = requiredTraits?.ToArray() ?? new string[0];
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

    [Serializable]
    public class RecursiveTraitSet
    {
        [JsonConstructor]
        private RecursiveTraitSet(IDictionary<string, RecursiveTrait> traits)
        {
            this.traits = traits;
        }

        private readonly IDictionary<string, RecursiveTrait> traits;
        public IDictionary<string, RecursiveTrait> Traits => traits;

        public static RecursiveTraitSet Build(IEnumerable<RecursiveTrait> traits)
        {
            return new RecursiveTraitSet(traits.ToDictionary(t => t.Name));
        }
        public static RecursiveTraitSet Build(params RecursiveTrait[] traits)
        {
            return new RecursiveTraitSet(traits.ToDictionary(t => t.Name));
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
