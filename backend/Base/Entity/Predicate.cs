
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Landscape.Base.Entity
{
    public class Predicate
    {
        public string ID { get; private set; }
        public string WordingFrom { get; private set; }
        public string WordingTo { get; private set; }

        public static Predicate Build(string id, string wordingFrom, string wordingTo)
        {
            return new Predicate
            {
                ID = id,
                WordingFrom = wordingFrom,
                WordingTo = wordingTo
            };
        }
    }
}
