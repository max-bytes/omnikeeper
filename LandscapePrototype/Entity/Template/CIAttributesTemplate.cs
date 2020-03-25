using LandscapePrototype.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapePrototype.Entity.Template
{
    public class CIAttributeTemplate
    {
        public string Name { get; private set; }
        public AttributeValueType? Type { get; private set; }
        // TODO: status: required(default, other statii: optional, not allowed)
        // TODO: value template, like restricting length, ...

        public static CIAttributeTemplate Build(string name, AttributeValueType? type)
        {
            return new CIAttributeTemplate()
            {
                Name = name,
                Type = type
            };
        }
    }

    public class CIAttributesTemplate
    {
        public CIType CIType { get; private set; }
        public Layer Layer { get; private set; }
        public IImmutableDictionary<string, CIAttributeTemplate> Attributes { get; private set; }

        public static CIAttributesTemplate Build(CIType ciType, Layer layer, IList<CIAttributeTemplate> attributes)
        {
            return new CIAttributesTemplate()
            {
                CIType = ciType,
                Layer = layer,
                Attributes = attributes.ToImmutableDictionary(a => a.Name)
            };
        }
    }
}
