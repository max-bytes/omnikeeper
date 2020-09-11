using Landscape.Base.Entity;
using Npgsql;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace LandscapeRegistry.Service
{
    public interface ICurrentUserService
    {
        Task<AuthenticatedUser> GetCurrentUser(NpgsqlTransaction trans);
        string GetUsernameFromClaims(IEnumerable<Claim> claims);

        IEnumerable<(string type, string value)> DebugGetAllClaims();
    }
}
