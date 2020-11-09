using Npgsql;
using Omnikeeper.Base.Entity;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Service
{
    public interface ICurrentUserService
    {
        Task<AuthenticatedUser> GetCurrentUser(NpgsqlTransaction trans);
        string GetUsernameFromClaims(IEnumerable<Claim> claims);

        IEnumerable<(string type, string value)> DebugGetAllClaims();
    }
}
