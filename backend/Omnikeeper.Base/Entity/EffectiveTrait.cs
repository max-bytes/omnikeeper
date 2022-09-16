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
        public static IAttributeValue? ExtractAttributeValueByTraitAttributeIdentifier(this EffectiveTrait effectiveTrait, string traitAttributeIdentifier)
        {
            return effectiveTrait.TraitAttributes[traitAttributeIdentifier]?.Attribute.Value;
        }

        public static ISet<string> ExtractAffectingLayerIDs(this EffectiveTrait effectiveTrait)
        {
            var ret = new HashSet<string>();
            foreach (var ta in effectiveTrait.TraitAttributes)
                ret.Add(ta.Value.LayerStackIDs.First());
            foreach (var tr in effectiveTrait.IncomingTraitRelations)
                foreach (var r in tr.Value)
                    ret.Add(r.LayerStackIDs.First());
            foreach (var tr in effectiveTrait.OutgoingTraitRelations)
                foreach (var r in tr.Value)
                    ret.Add(r.LayerStackIDs.First());
            return ret;
        }
    }
}
