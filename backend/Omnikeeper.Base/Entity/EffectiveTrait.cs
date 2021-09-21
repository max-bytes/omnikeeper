using System.Collections.Generic;
using System.Collections.Immutable;

namespace Omnikeeper.Base.Entity
{
    public class EffectiveTrait
    {
        public readonly ITrait UnderlyingTrait;
        public readonly IImmutableDictionary<string, MergedCIAttribute> TraitAttributes;
        public readonly IImmutableDictionary<string, IEnumerable<MergedRelation>> OutgoingTraitRelations;
        public readonly IImmutableDictionary<string, IEnumerable<MergedRelation>> IncomingTraitRelations;

        public EffectiveTrait(ITrait underlyingTrait,
            IDictionary<string, MergedCIAttribute> traitAttributes,
            IDictionary<string, IEnumerable<MergedRelation>> outgoingTraitRelations,
            IDictionary<string, IEnumerable<MergedRelation>> incomingTraitRelations
            )
        {
            UnderlyingTrait = underlyingTrait;
            TraitAttributes = traitAttributes.ToImmutableDictionary();
            OutgoingTraitRelations = outgoingTraitRelations.ToImmutableDictionary();
            IncomingTraitRelations = incomingTraitRelations.ToImmutableDictionary();
        }
    }
}
