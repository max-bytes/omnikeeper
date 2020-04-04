using Landscape.Base.Entity;
using LandscapeRegistry;
using LandscapeRegistry.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Landscape.Base
{
    public static class CIExtensions
    {
        public static IEnumerable<MergedCIAttribute> GetAttributesInGroup(this MergedCI ci, string groupName)
        {
            return ci.MergedAttributes.Where(a => a.Attribute.Name.StartsWith(groupName));
        }
    }
}
