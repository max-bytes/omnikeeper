using Landscape.Base.Entity;
using LandscapeRegistry.Entity;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Landscape.Base.Model
{
    public interface IUserInDatabaseModel
    {
        Task<UserInDatabase> UpsertUser(string username, Guid uuid, UserType type, NpgsqlTransaction trans);
    }
}
