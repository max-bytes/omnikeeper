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
    public class TraitType : ObjectGraphType<Trait>
    {
        public TraitType()
        {
            Field(x => x.Name);
        }
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
