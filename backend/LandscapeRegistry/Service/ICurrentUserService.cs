using Landscape.Base.Entity;
using Landscape.Base.Model;
using Microsoft.AspNetCore.Http;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
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
