using GraphQL.Language.AST;
using GraphQL.Types;
using Omnikeeper.Base.Entity;
using System.Collections.Generic;
using System.Linq;

namespace Omnikeeper.GraphQL.Types
{
    public class EffectiveTraitType : ObjectGraphType<EffectiveTrait>
    {
        public EffectiveTraitType()
        {
            Field("ciid", x => x.CIID);
            Field(x => x.UnderlyingTrait, type: typeof(TraitType));
            Field("traitAttributes", x => x.TraitAttributes.Select(t => new EffectiveTraitAttribute(t.Key, t.Value)), type: typeof(ListGraphType<EffectiveTraitAttributeType>));
            Field("outgoingTraitRelations", x => x.OutgoingTraitRelations.Select(t => new EffectiveTraitRelation(t.Key, t.Value)), type: typeof(ListGraphType<EffectiveTraitRelationType>));
            Field("incomingTraitRelations", x => x.IncomingTraitRelations.Select(t => new EffectiveTraitRelation(t.Key, t.Value)), type: typeof(ListGraphType<EffectiveTraitRelationType>));
        }
    }
    public class TraitType : ObjectGraphType<ITrait>
    {
        public TraitType()
        {
            Field("id", x => x.ID);
            Field(x => x.Origin, type: typeof(TraitOriginV1Type));
            Field("ancestorTraits", x => x.AncestorTraits);

            Field("requiredAttributes", x => x.RequiredAttributes, type: typeof(ListGraphType<TraitAttributeType>));
            Field("optionalAttributes", x => x.OptionalAttributes, type: typeof(ListGraphType<TraitAttributeType>));
            Field("optionalRelations", x => x.OptionalRelations, type: typeof(ListGraphType<TraitRelationType>));
        }
    }
    public class TraitOriginV1Type : ObjectGraphType<TraitOriginV1>
    {
        public TraitOriginV1Type()
        {
            Field(x => x.Type, type: typeof(TraitOriginTypeType));
            Field(x => x.Info, nullable: true);
        }
    }
    public class TraitOriginTypeType : EnumerationGraphType<TraitOriginType>
    {
    }

    public class RecursiveTraitType : ObjectGraphType<RecursiveTrait>
    {
        public RecursiveTraitType()
        {
            Field("id", x => x.ID);
            Field("requiredAttributes", x => x.RequiredAttributes, type: typeof(ListGraphType<TraitAttributeType>));
            Field("optionalAttributes", x => x.OptionalAttributes, type: typeof(ListGraphType<TraitAttributeType>));
            Field("optionalRelations", x => x.OptionalRelations, type: typeof(ListGraphType<TraitRelationType>));
            Field("requiredTraits", x => x.RequiredTraits, type: typeof(ListGraphType<StringGraphType>));
        }
    }

    public class TraitAttributeType : ObjectGraphType<TraitAttribute>
    {
        public TraitAttributeType()
        {
            Field("identifier", x => x.Identifier);
            Field("template", x => x.AttributeTemplate, type: typeof(CIAttributeTemplateType));
        }
    }

    public class CIAttributeTemplateType : ObjectGraphType<CIAttributeTemplate>
    {
        public CIAttributeTemplateType()
        {
            Field("name", x => x.Name);
            Field("type", x => x.Type, type: typeof(AttributeValueTypeType));
            Field("isArray", x => x.IsArray, type: typeof(BooleanGraphType));
            Field("isID", x => x.IsID, type: typeof(BooleanGraphType));
            Field("valueConstraints", x => x.ValueConstraints, type: typeof(ListGraphType<AttributeValueConstraintType>));
        }
    }

    public class AttributeValueConstraintType : ScalarGraphType
    {
        public AttributeValueConstraintType()
        {
            Name = "AttributeValueConstraint";
        }

        public override object? ParseLiteral(IValue value)
        {
            if (value is NullValue)
                return null;

            if (value is StringValue stringValue)
                return ParseValue(stringValue.Value);

            return ThrowLiteralConversionError(value);
        }

        public override object? ParseValue(object? value)
        {
            if (value == null)
                return null;

            if (value is string valueStr)
                return ICIAttributeValueConstraint.Serializer.Deserialize(valueStr);
            return ThrowValueConversionError(value);
        }

        public override object? Serialize(object? value)
        {
            if (value == null)
                return null;

            if (value is ICIAttributeValueConstraint vc)
                return ICIAttributeValueConstraint.Serializer.SerializeToString(vc);
            return ThrowSerializationError(value);
        }
    }

    public class TraitRelationType : ObjectGraphType<TraitRelation>
    {
        public TraitRelationType()
        {
            Field("identifier", x => x.Identifier);
            Field("template", x => x.RelationTemplate, type: typeof(RelationTemplateType));
        }
    }

    public class RelationTemplateType : ObjectGraphType<RelationTemplate>
    {
        public RelationTemplateType() {
            Field("predicateID", x => x.PredicateID);
            Field("directionForward", x => x.DirectionForward);
            Field("traitHints", x => x.TraitHints);
        }
    }

    public class EffectiveTraitAttribute
    {
        public readonly string Identifier;
        public readonly MergedCIAttribute Attribute;

        public EffectiveTraitAttribute(string identifier, MergedCIAttribute attribute)
        {
            Identifier = identifier;
            Attribute = attribute;
        }
    }

    public class EffectiveTraitAttributeType : ObjectGraphType<EffectiveTraitAttribute>
    {
        public EffectiveTraitAttributeType()
        {
            Field("identifier", x => x.Identifier);
            Field("mergedAttribute", x => x.Attribute, type: typeof(MergedCIAttributeType));
        }
    }

    public class EffectiveTraitRelation
    {
        public readonly string Identifier;
        public readonly IEnumerable<MergedRelation> Relations;

        public EffectiveTraitRelation(string identifier, IEnumerable<MergedRelation> relations)
        {
            Identifier = identifier;
            Relations = relations;
        }
    }

    public class EffectiveTraitRelationType : ObjectGraphType<EffectiveTraitRelation>
    {
        public EffectiveTraitRelationType()
        {
            Field("identifier", x => x.Identifier);
            Field("relations", x => x.Relations, type: typeof(ListGraphType<MergedRelationType>));
        }
    }
}
