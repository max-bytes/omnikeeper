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
    public interface IUserModel
    {
        Task<User> CreateUserFromClaims(IEnumerable<Claim> claims);
    }
}
