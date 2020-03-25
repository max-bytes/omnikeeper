
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

        public override int GetHashCode() => HashCode.Combine(ID);
        public override bool Equals(object obj)
        {
            if (obj is CIType other)
            {
                return ID.Equals(other.ID);
            }
            else return false;
        }
    }
}
