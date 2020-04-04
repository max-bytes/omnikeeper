using Landscape.Base.Entity;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Landscape.Base.Model
{
    public interface ITraitsProvider
    {
        public Task<Traits> GetTraits(NpgsqlTransaction trans);
    }
}
