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
        public Task<IImmutableDictionary<string, Trait>> GetTraits(NpgsqlTransaction trans, TimeThreshold atTime);
    }
}
