using Landscape.Base.Entity;
using Npgsql;
using System;
using System.Threading.Tasks;

namespace Landscape.Base.Model
{
    public interface IUserInDatabaseModel
    {
        Task<UserInDatabase> UpsertUser(string username, Guid uuid, UserType type, NpgsqlTransaction trans);
    }
}
