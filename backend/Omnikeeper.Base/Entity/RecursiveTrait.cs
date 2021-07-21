using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Omnikeeper.Base.Utils;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Omnikeeper.Base.Entity
{
    [ProtoContract(SkipConstructor = true)]
    public class TraitRelation
    {
        [ProtoMember(1)] public readonly RelationTemplate RelationTemplate;
        // TODO: implement anyOf(RelationTemplate[])
        [ProtoMember(2)] public readonly string Identifier;

        public TraitRelation(string identifier, RelationTemplate relationTemplate)
        {
            Identifier = identifier;
            RelationTemplate = relationTemplate;
        }

        public static readonly MyJSONSerializer<TraitRelation> Serializer = new MyJSONSerializer<TraitRelation>(() =>
        {
            var s = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Objects
            };
            s.Converters.Add(new StringEnumConverter());
            return s;
        });
    }
    [ProtoContract(SkipConstructor = true)]
    public class TraitAttribute
    {
        [ProtoMember(1)] public readonly CIAttributeTemplate AttributeTemplate;
        [ProtoMember(2)] public readonly string Identifier;

        // TODO: implement anyOf(CIAttributeTemplate[])

        public TraitAttribute(string identifier, CIAttributeTemplate attributeTemplate)
        {
            Identifier = identifier;
            AttributeTemplate = attributeTemplate;
        }

        public static readonly MyJSONSerializer<TraitAttribute> Serializer = new MyJSONSerializer<TraitAttribute>(() =>
        {
            var s = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Objects
            };
            s.Converters.Add(new StringEnumConverter());
            return s;
        });
    }

    public enum TraitOriginType
    {
        Plugin,
        Core,
        Data
    }

    [ProtoContract(SkipConstructor = true)]
    public class TraitOriginV1
    {
        public TraitOriginV1(TraitOriginType type, string? info = null)
        {
            Type = type;
            Info = info;
        }

        [ProtoMember(1)]
        public readonly TraitOriginType Type;
        [ProtoMember(2)]
        public readonly string? Info;
    } // TODO: equality/hash/...?

    [ProtoContract] // NOTE: cannot skip constructor, because then initializations are not done either, leaving arrays at null
    public class RecursiveTrait
    {
        [ProtoMember(1)] public readonly string Name; // TODO: rename to "id" because name is misleading
        [ProtoMember(2)] public readonly TraitOriginV1 Origin;
        [ProtoMember(3)] public readonly TraitAttribute[] RequiredAttributes = Array.Empty<TraitAttribute>();
        [ProtoMember(4)] public readonly TraitAttribute[] OptionalAttributes = Array.Empty<TraitAttribute>();
        [ProtoMember(5)] public readonly string[] RequiredTraits = Array.Empty<string>();
        [ProtoMember(6)] public readonly TraitRelation[] RequiredRelations = Array.Empty<TraitRelation>();
        // TODO: implement optional relations

#pragma warning disable CS8618
        private RecursiveTrait() { }
#pragma warning restore CS8618

        public RecursiveTrait(string name, TraitOriginV1 origin,
            IEnumerable<TraitAttribute>? requiredAttributes = null,
            IEnumerable<TraitAttribute>? optionalAttributes = null,
            IEnumerable<TraitRelation>? requiredRelations = null,
            IEnumerable<string>? requiredTraits = null)
        {
            Name = name;
            Origin = origin ?? new TraitOriginV1(TraitOriginType.Data);
            RequiredAttributes = requiredAttributes?.ToArray() ?? new TraitAttribute[0];
            OptionalAttributes = optionalAttributes?.ToArray() ?? new TraitAttribute[0];
            RequiredRelations = requiredRelations?.ToArray() ?? new TraitRelation[0];
            RequiredTraits = requiredTraits?.ToArray() ?? new string[0];
        }
    }

    // TODO: needed? Its just a wrapper over a dictionary of RecursiveTraits after all...
    [ProtoContract] // NOTE: cannot skip constructor, because then initializations are not done either, leaving arrays at null
    public class RecursiveTraitSet
    {
        [JsonConstructor]
        private RecursiveTraitSet(IDictionary<string, RecursiveTrait> traits)
        {
            this.traits = traits;
        }

#pragma warning disable CS8618
        private RecursiveTraitSet() { }
#pragma warning restore CS8618

        [ProtoMember(1)]
        private readonly IDictionary<string, RecursiveTrait> traits = new Dictionary<string,RecursiveTrait>();
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
}
