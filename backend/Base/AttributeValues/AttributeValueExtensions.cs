using Landscape.Base.Entity;
using LandscapeRegistry.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Landscape.Base.AttributeValues
{
    public static class AttributeValueExtensions
    {
        public static IEnumerable<string> TryReadValueTextArray(this MergedCIAttribute attribute)
        {
            if (attribute?.Attribute.Value is AttributeArrayValueText v)
            {
                return v.Values.Select(vv => vv.Value);
            }

            return default;
        }
    }
}
