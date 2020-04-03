using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapeRegistry.Entity.Template
{
    public class Trait
    {
        public string Name { get; private set; }

        public IImmutableDictionary<string, CIAttributeTemplate> Attributes { get; private set; }

        public static Trait Build(string name, IDictionary<string, CIAttributeTemplate> attributes)
        {
            return new Trait()
            {
                Name = name,
                Attributes = attributes.ToImmutableDictionary()
            };
        }
    }
}
