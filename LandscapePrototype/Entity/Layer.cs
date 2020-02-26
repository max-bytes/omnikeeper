using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapePrototype.Entity
{
    public class Layer
    {
        public string Name { get; private set; }
        public long ID { get; private set; }

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
