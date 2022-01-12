using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Omnikeeper.Base.Entity
{
    public class EffectiveTrait
    {
        public readonly Guid CIID;
        public readonly ITrait UnderlyingTrait;
        public readonly IImmutableDictionary<string, MergedCIAttribute> TraitAttributes;
        public readonly IImmutableDictionary<string, IEnumerable<MergedRelation>> OutgoingTraitRelations;
        public readonly IImmutableDictionary<string, IEnumerable<MergedRelation>> IncomingTraitRelations;

        public EffectiveTrait(Guid ciid, ITrait underlyingTrait,
            IDictionary<string, MergedCIAttribute> traitAttributes,
            IDictionary<string, IEnumerable<MergedRelation>> outgoingTraitRelations,
            IDictionary<string, IEnumerable<MergedRelation>> incomingTraitRelations
            )
        {
            CIID = ciid;
            UnderlyingTrait = underlyingTrait;
            TraitAttributes = traitAttributes.ToImmutableDictionary();
            OutgoingTraitRelations = outgoingTraitRelations.ToImmutableDictionary();
            IncomingTraitRelations = incomingTraitRelations.ToImmutableDictionary();
        }
    }

    public static class EffectiveTraitExtensions
    {
        public static IEnumerable<(string attributeName, IAttributeValue attributeValue)> ExtractIDAttributeValueTuples(this EffectiveTrait effectiveTrait)
        {
            var idTraitAttributes = effectiveTrait.UnderlyingTrait.RequiredAttributes.Where(ra => ra.AttributeTemplate.IsID.GetValueOrDefault(false));
            foreach(var idTraitAttribute in idTraitAttributes)
            {
                var ta = effectiveTrait.TraitAttributes[idTraitAttribute.Identifier];

                yield return (ta.Attribute.Name, ta.Attribute.Value);
            }
        }

        public static IAttributeValue? ExtractAttributeValueByTraitAttributeIdentifier(this EffectiveTrait effectiveTrait, string traitAttributeIdentifier)
        {
            return effectiveTrait.TraitAttributes[traitAttributeIdentifier]?.Attribute.Value;
        }
    }
}
