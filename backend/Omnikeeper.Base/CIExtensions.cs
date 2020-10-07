using Omnikeeper.Base.Entity;
using System.Collections.Generic;
using System.Linq;

namespace Omnikeeper.Base
{
    public static class CIExtensions
    {
        public static IEnumerable<MergedCIAttribute> GetAttributesInGroup(this MergedCI ci, string groupName)
        {
            return ci.MergedAttributes.Where(kv => kv.Key.StartsWith(groupName)).Select(kv => kv.Value);
        }
    }
}
