using GraphQL.Types;
using Omnikeeper.Base.Entity;
using System;

namespace Omnikeeper.GraphQL
{
    public class EffectiveTraitType : ObjectGraphType<EffectiveTrait>
    {
        public EffectiveTraitType()
        {
            Field(x => x.UnderlyingTrait, type: typeof(TraitType));
            Field("attributes", x => x.TraitAttributes.Values, type: typeof(ListGraphType<MergedCIAttributeType>)); // TODO: don't ignore/drop traitattribute identifier (=key of dict)
        }
    }
    public class TraitType : ObjectGraphType<ITrait>
    {
        public TraitType()
        {
            Field(x => x.Name);
            Field(x => x.Origin, type: typeof(TraitOriginV1Type));
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

    public class EffectiveTraitListItemType : ObjectGraphType<ValueTuple<string, int>>
    {
        public EffectiveTraitListItemType()
        {
            Field("name", x => x.Item1);
            Field("count", x => x.Item2);
        }
    }
}
