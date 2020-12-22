﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Omnikeeper.Base.Entity
{
    [Serializable]
    public class EffectiveTrait
    {
        public readonly Trait UnderlyingTrait;
        public readonly IImmutableDictionary<string, MergedCIAttribute> TraitAttributes;
        public readonly IImmutableDictionary<string, IEnumerable<CompactRelatedCI>> TraitRelations;

        public EffectiveTrait(Trait underlyingTrait,
            IDictionary<string, MergedCIAttribute> traitAttributes,
            IDictionary<string, IEnumerable<CompactRelatedCI>> traitRelations)
        {
            UnderlyingTrait = underlyingTrait;
            TraitAttributes = traitAttributes.ToImmutableDictionary();
            TraitRelations = traitRelations.ToImmutableDictionary();
        }
    }
}
