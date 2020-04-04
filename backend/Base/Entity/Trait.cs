using LandscapeRegistry.Entity;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Landscape.Base.Entity
{
    public class TraitAttribute
    {
        public CIAttributeTemplate AttributeTemplate { get;private set; }
        public string AlternativeName => AttributeTemplate.Name; // TODO

        // TODO: implement anyOf(CIAttributeTemplate[])
        // TODO: add optional name/label (to refer to it)

        public static TraitAttribute Build(CIAttributeTemplate attributeTemplate)
        {
            return new TraitAttribute()
            {
                AttributeTemplate = attributeTemplate
            };
        }
    }
    public class Trait
    {
        public string Name { get; private set; }

        public IImmutableList<TraitAttribute> Attributes { get; private set; }

        public static Trait Build(string name, IList<TraitAttribute> attributes)
        {
            return new Trait()
            {
                Name = name,
                Attributes = attributes.ToImmutableList()
            };
        }
    }
}
