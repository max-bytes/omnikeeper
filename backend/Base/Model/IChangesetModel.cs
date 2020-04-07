using Landscape.Base.Entity;
using LandscapeRegistry.Entity;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Landscape.Base.Model
{
    public interface IChangesetModel
    {
        Task<Changeset> CreateChangeset(long userID, NpgsqlTransaction trans);
    }
}
