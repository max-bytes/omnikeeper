﻿using System.Collections.Generic;
using System.Collections.Immutable;

namespace Landscape.Base.Entity
{
    public class Template
    {
        public string CITypeID { get; private set; }
        public IImmutableDictionary<string, CIAttributeTemplate> AttributeTemplates { get; private set; }
        public IImmutableDictionary<string, RelationTemplate> RelationTemplates { get; private set; }

        public IImmutableDictionary<string, RecursiveTrait> Traits { get; private set; } // TODO: actually check if the traits are fulfilled

        public static Template Build(string ciTypeID, IEnumerable<CIAttributeTemplate> attributes, IEnumerable<RelationTemplate> relations, IEnumerable<RecursiveTrait> traits)
        {
            return new Template()
            {
                CITypeID = ciTypeID,
                AttributeTemplates = attributes.ToImmutableDictionary(t => t.Name),
                RelationTemplates = relations.ToImmutableDictionary(t => t.PredicateID),
                Traits = traits.ToImmutableDictionary(t => t.Name)
            };
        }
    }
}
