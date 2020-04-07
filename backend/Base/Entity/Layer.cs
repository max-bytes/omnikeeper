using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Landscape.Base.Entity
{
    public class Layer
    {
        public string Name { get; private set; }
        public long ID { get; private set; }

        public override int GetHashCode() => HashCode.Combine(Name, ID);
        public override bool Equals(object obj)
        {
            if (obj is Layer other)
            {
                return Name.Equals(other.Name) && ID.Equals(other.ID);
            }
            else return false;
        }

        public static Layer Build(string name, long id)
        {
            var r = new Layer
            {
                Name = name,
                ID = id
            };
            return r;
        }
    }
}
