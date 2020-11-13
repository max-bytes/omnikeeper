using Omnikeeper.Base.Entity;
using Omnikeeper.Entity.AttributeValues;
using System.Collections.Generic;
using System.Linq;

namespace Omnikeeper.Base.AttributeValues
{
    public static class AttributeValueExtensions
    {
        public static IEnumerable<string>? TryReadValueTextArray(this MergedCIAttribute attribute)
        {
            if (attribute?.Attribute.Value is AttributeArrayValueText v)
            {
                return v.Values.Select(vv => vv.Value);
            }

            return null;
        }
    }
}
