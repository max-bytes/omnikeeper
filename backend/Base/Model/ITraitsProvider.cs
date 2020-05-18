using Landscape.Base.Entity;
using Landscape.Base.Utils;
using Npgsql;
using System;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Landscape.Base.Model
{
    public interface ITraitsProvider
    {
        void Register(string source, Trait[] t);

        IImmutableDictionary<string, Trait> GetTraits();
    }
}
