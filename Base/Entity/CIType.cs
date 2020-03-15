
using LandscapePrototype.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapePrototype.Entity
{
    public class CIType
    {
        public string ID { get; private set; }
        public static CIType Build(string id)
        {
            return new CIType
            {
                ID = id
            };
        }
    }
}
