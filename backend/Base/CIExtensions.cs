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
        //public static bool IsOfType(this CI ci, string type)
        //{
        //    return ci.Attributes.FirstOrDefault(a => a.Name == "__type" && a.Value.Value2String() == type) != null;
        //}

        public static IEnumerable<MergedCIAttribute> GetAttributesInGroup(this MergedCI ci, string groupName)
        {
            return ci.MergedAttributes.Where(a => a.Attribute.Name.StartsWith(groupName));
        }
    }
}
