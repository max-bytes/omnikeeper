using LandscapePrototype.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapePrototype.Entity.GraphQL
{
    public class LandscapeUserContext : Dictionary<string, object>
    {
        public LandscapeUserContext()
        {
        }

        public DateTimeOffset TimeThreshold {
            get
            {
                return (DateTimeOffset)this["TimeThreshold"];
            }
            set
            {
                Add("TimeThreshold", value);
            }
        }

        public LayerSet LayerSet
        {
            get
            {
                return (LayerSet)this["LayerSet"];
            }
            set
            {
                Add("LayerSet", value);
            }
        }
    }
}
