using Landscape.Base.Model;
using LandscapeRegistry.Entity;
using LandscapeRegistry.Entity.AttributeValues;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Landscape.Base.Entity
{
    public class Traits
    {
        public IImmutableDictionary<string, Trait> traits { get; private set; }

        public async static Task<Traits> Build(IEnumerable<Trait> traits, NpgsqlTransaction trans)
        {
            return new Traits()
            {
                traits = traits.ToImmutableDictionary(t => t.Name)
            };
        }
    }
}
