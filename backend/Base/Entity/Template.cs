using System.Collections.Generic;
using System.Collections.Immutable;

namespace Landscape.Base.Entity
{
    public class Template
    {
        public CIType CIType { get; private set; }
        public IImmutableDictionary<string, CIAttributeTemplate> AttributeTemplates { get; private set; }
        public IImmutableDictionary<string, RelationTemplate> RelationTemplates { get; private set; }

        public IImmutableDictionary<string, Trait> Traits { get; private set; } // TODO: actually check if the traits are fulfilled

        public static Template Build(CIType ciType, IEnumerable<CIAttributeTemplate> attributes, IEnumerable<RelationTemplate> relations, IEnumerable<Trait> traits)
        {
            return new Template()
            {
                CIType = ciType,
                AttributeTemplates = attributes.ToImmutableDictionary(t => t.Name),
                RelationTemplates = relations.ToImmutableDictionary(t => t.Predicate.ID),
                Traits = traits.ToImmutableDictionary(t => t.Name)
            };
        }
    }
}
