using GraphQL.Types;
using Landscape.Base.Entity;

namespace LandscapeRegistry.Entity.GraphQL
{
    public class EffectiveTraitSetType : ObjectGraphType<EffectiveTraitSet>
    {
        public EffectiveTraitSetType()
        {
            Field(x => x.EffectiveTraits, type: typeof(ListGraphType<EffectiveTraitType>));
        }
    }
    public class EffectiveTraitType : ObjectGraphType<EffectiveTrait>
    {
        public EffectiveTraitType()
        {
            Field(x => x.UnderlyingTrait, type: typeof(TraitType));
            Field(x => x.TraitAttributes.Values, type: typeof(ListGraphType<MergedCIAttributeType>));
        }
    }
    public class TraitType : ObjectGraphType<Trait>
    {
        public TraitType()
        {
            Field(x => x.Name);
        }
    }
}
