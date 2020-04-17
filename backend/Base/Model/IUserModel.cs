using Landscape.Base.Entity;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Landscape.Base.Model
{
    public interface IUserModel
    {
        Task<User> CreateUserFromClaims(IEnumerable<Claim> claims);
    }
}
