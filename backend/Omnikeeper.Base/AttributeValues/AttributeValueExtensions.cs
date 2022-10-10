using Omnikeeper.Base.Entity;
using Omnikeeper.Entity.AttributeValues;
using System.Collections.Generic;

namespace Omnikeeper.Base.AttributeValues
{
    public static class AttributeValueExtensions
    {
        public static IEnumerable<string>? TryReadValueTextArray(this MergedCIAttribute attribute)
        {
            if (attribute?.Attribute.Value is AttributeArrayValueText v)
            {
                return v.Values;
            }

            return null;
        }
    }
}
