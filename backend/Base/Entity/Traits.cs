using Npgsql;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Landscape.Base.Entity
{
    public class Traits
    {
        public IImmutableDictionary<string, Trait> traits { get; private set; }

        public async static Task<Traits> Build(IEnumerable<Trait> traits)
        {
            return new Traits()
            {
                traits = traits.ToImmutableDictionary(t => t.Name)
            };
        }
    }
}
