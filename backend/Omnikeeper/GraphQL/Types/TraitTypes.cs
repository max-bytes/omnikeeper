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

            // HACK, TODO: objects are complex, simply returning them as JSON strings for now
            Field("requiredAttributes", x => x.RequiredAttributes.Select(r => TraitAttribute.Serializer.SerializeToString(r)), type: typeof(ListGraphType<StringGraphType>));
            Field("optionalAttributes", x => x.OptionalAttributes.Select(r => TraitAttribute.Serializer.SerializeToString(r)), type: typeof(ListGraphType<StringGraphType>));
            Field("requiredRelations", x => x.RequiredRelations.Select(r => TraitRelation.Serializer.SerializeToString(r)), type: typeof(ListGraphType<StringGraphType>));
            Field("optionalRelations", x => x.OptionalRelations.Select(r => TraitRelation.Serializer.SerializeToString(r)), type: typeof(ListGraphType<StringGraphType>));
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
            Field("requiredAttributes", x => x.RequiredAttributes.Select(a => TraitAttribute.Serializer.SerializeToString(a)), type: typeof(ListGraphType<StringGraphType>));
            Field("optionalAttributes", x => x.OptionalAttributes.Select(a => TraitAttribute.Serializer.SerializeToString(a)), type: typeof(ListGraphType<StringGraphType>));
            Field("requiredRelations", x => x.RequiredRelations.Select(a => TraitRelation.Serializer.SerializeToString(a)), type: typeof(ListGraphType<StringGraphType>));
            Field("optionalRelations", x => x.OptionalRelations.Select(a => TraitRelation.Serializer.SerializeToString(a)), type: typeof(ListGraphType<StringGraphType>));
            Field("requiredTraits", x => x.RequiredTraits, type: typeof(ListGraphType<StringGraphType>));
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
