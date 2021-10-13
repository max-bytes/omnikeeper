﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Omnikeeper.Base.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Omnikeeper.Base.Entity
{
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

    public class TraitOriginV1
    {
        public TraitOriginV1(TraitOriginType type, string? info = null)
        {
            Type = type;
            Info = info;
        }

        public readonly TraitOriginType Type;
        public readonly string? Info;
    } // TODO: equality/hash/...?

    [TraitEntity("__meta.config.trait", TraitOriginType.Core)]
    public class RecursiveTrait : TraitEntity
    {
        [TraitAttribute("id", "trait.id")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        [TraitAttributeValueConstraintTextRegex(IDValidations.TraitIDRegexString, IDValidations.TraitIDRegexOptions)]
        [TraitEntityID]
        public readonly string ID;

        public TraitOriginV1 Origin { get; }

        [TraitAttribute("name", "__name", optional: true)]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        public readonly string Name;

        [TraitAttribute("required_attributes", "trait.required_attributes", isJSONSerialized: true)]
        [TraitAttributeValueConstraintArrayLength(1, -1)]
        public readonly TraitAttribute[] RequiredAttributes = Array.Empty<TraitAttribute>();

        [TraitAttribute("optional_attributes", "trait.optional_attributes", isJSONSerialized: true, optional: true)]
        public readonly TraitAttribute[] OptionalAttributes = Array.Empty<TraitAttribute>();

        [TraitAttribute("required_relations", "trait.required_relations", isJSONSerialized: true, optional: true)]
        public readonly TraitRelation[] RequiredRelations = Array.Empty<TraitRelation>();

        [TraitAttribute("optional_relations", "trait.optional_relations", isJSONSerialized: true, optional: true)]
        public readonly TraitRelation[] OptionalRelations = Array.Empty<TraitRelation>();

        [TraitAttribute("required_traits", "trait.required_traits", optional: true)]
        public readonly string[] RequiredTraits = Array.Empty<string>();

        public RecursiveTrait() {
            ID = "";
            Name = "";
            Origin = new TraitOriginV1(TraitOriginType.Data);
        }

        [JsonConstructor]
        public RecursiveTrait(string id, TraitOriginV1 origin,
            IEnumerable<TraitAttribute>? requiredAttributes = null,
            IEnumerable<TraitAttribute>? optionalAttributes = null,
            IEnumerable<TraitRelation>? requiredRelations = null,
            IEnumerable<TraitRelation>? optionalRelations = null,
            IEnumerable<string>? requiredTraits = null)
        {
            ID = id;
            Name = $"Trait - {ID}";
            Origin = origin ?? new TraitOriginV1(TraitOriginType.Data);
            RequiredAttributes = requiredAttributes?.ToArray() ?? new TraitAttribute[0];
            OptionalAttributes = optionalAttributes?.ToArray() ?? new TraitAttribute[0];
            RequiredRelations = requiredRelations?.ToArray() ?? new TraitRelation[0];
            OptionalRelations = optionalRelations?.ToArray() ?? new TraitRelation[0];
            RequiredTraits = requiredTraits?.ToArray() ?? new string[0];
        }

        // TODO: still needed?
        public static readonly MyJSONSerializer<RecursiveTrait> Serializer = new MyJSONSerializer<RecursiveTrait>(() =>
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
