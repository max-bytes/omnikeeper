using LandscapePrototype;
using LandscapePrototype.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Landscape.Base
{
    public static class CIExtensions
    {
        public static bool IsOfType(this CI ci, string type)
        {
            return ci.Attributes.FirstOrDefault(a => a.Name == "__type" && a.Value.Value2String() == type) != null;
        }

        public static IEnumerable<CIAttribute> GetAttributesInGroup(this CI ci, string groupName)
        {
            return ci.Attributes.Where(a => a.Name.StartsWith(groupName));
        }
    }
}
