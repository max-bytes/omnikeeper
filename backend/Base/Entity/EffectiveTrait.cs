
using Landscape.Base.Entity;
using LandscapeRegistry.Entity;
using LandscapeRegistry.Model;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Landscape.Base.Entity
{
    public class EffectiveTrait
    {
        public Trait UnderlyingTrait { get; private set; }
        public IImmutableDictionary<string, MergedCIAttribute> TraitAttributes { get; private set; }

        public static EffectiveTrait Build(Trait underlyingTrait, IDictionary<string, MergedCIAttribute> traitAttributes)
        {
            return new EffectiveTrait
            {
                UnderlyingTrait = underlyingTrait,
                TraitAttributes = traitAttributes.ToImmutableDictionary()
            };
        }
    }
}
