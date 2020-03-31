using LandscapePrototype.Entity;
using LandscapePrototype.Model;
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
        //Task<UserInDatabase> CreateUserFromClaims(IEnumerable<Claim> claims);
        Task<UserInDatabase> CreateOrUpdateFetchUser(string username, Guid uuid, UserType type, NpgsqlTransaction trans);
    }
}
