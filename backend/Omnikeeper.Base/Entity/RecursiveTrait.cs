using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Omnikeeper.Base.Entity
{
    public class TraitRelation
    {
        public readonly RelationTemplate RelationTemplate;
        public readonly string Identifier;

        public TraitRelation(string identifier, RelationTemplate relationTemplate)
        {
            Identifier = identifier;
            RelationTemplate = relationTemplate;
        }
    }

    public class TraitAttribute
    {
        public readonly CIAttributeTemplate AttributeTemplate;
        public readonly string Identifier;

        public TraitAttribute(string identifier, CIAttributeTemplate attributeTemplate)
        {
            Identifier = identifier;
            AttributeTemplate = attributeTemplate;
        }
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
        public string ID;

        public TraitOriginV1 Origin { get; }

        [TraitAttribute("name", "__name", optional: true)]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        public string Name;

        [TraitAttribute("required_attributes", "trait.required_attributes", jsonSerializer: typeof(RecursiveTraitModel.TraitAttributeSerializer))]
        [TraitAttributeValueConstraintArrayLength(1, -1)]
        public TraitAttribute[] RequiredAttributes = Array.Empty<TraitAttribute>();

        [TraitAttribute("optional_attributes", "trait.optional_attributes", jsonSerializer: typeof(RecursiveTraitModel.TraitAttributeSerializer), optional: true, initToDefaultWhenMissing: false)]
        public TraitAttribute[] OptionalAttributes = Array.Empty<TraitAttribute>();

        [TraitAttribute("optional_relations", "trait.optional_relations", jsonSerializer: typeof(RecursiveTraitModel.TraitRelationSerializer), optional: true, initToDefaultWhenMissing: false)]
        public TraitRelation[] OptionalRelations = Array.Empty<TraitRelation>();

        [TraitAttribute("required_traits", "trait.required_traits", optional: true, initToDefaultWhenMissing: false)]
        public string[] RequiredTraits = Array.Empty<string>();

        public RecursiveTrait()
        {
            ID = "";
            Name = "";
            Origin = new TraitOriginV1(TraitOriginType.Data);
        }

        [JsonConstructor]
        public RecursiveTrait(string id, TraitOriginV1 origin,
            IEnumerable<TraitAttribute>? requiredAttributes = null,
            IEnumerable<TraitAttribute>? optionalAttributes = null,
            IEnumerable<TraitRelation>? optionalRelations = null,
            IEnumerable<string>? requiredTraits = null)
        {
            ID = id;
            Name = $"Trait - {ID}";
            Origin = origin ?? new TraitOriginV1(TraitOriginType.Data);
            RequiredAttributes = requiredAttributes?.ToArray() ?? new TraitAttribute[0];
            OptionalAttributes = optionalAttributes?.ToArray() ?? new TraitAttribute[0];
            OptionalRelations = optionalRelations?.ToArray() ?? new TraitRelation[0];
            RequiredTraits = requiredTraits?.ToArray() ?? new string[0];
        }
    }
}
