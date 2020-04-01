using Landscape.Base.Model;
using LandscapeRegistry.Entity;
using LandscapeRegistry.Model;
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
    public class CurrentUserService
    {
        public CurrentUserService(IHttpContextAccessor httpContextAccessor, IUserInDatabaseModel userModel, ILayerModel layerModel)
        {
            HttpContextAccessor = httpContextAccessor;
            UserModel = userModel;
            LayerModel = layerModel;
        }

        private IHttpContextAccessor HttpContextAccessor { get; }
        private IUserInDatabaseModel UserModel { get; }
        private ILayerModel LayerModel { get; }

        public async Task<User> GetCurrentUser(NpgsqlTransaction trans)
        {
            return await CreateUserFromClaims(HttpContextAccessor.HttpContext.User.Claims, trans);
        }

        private async Task<User> CreateUserFromClaims(IEnumerable<Claim> claims, NpgsqlTransaction trans)
        {
            var username = claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value;
            
            if (username == null)
            {
                var anonymousGuid = new Guid("2544f9a7-cc17-4cba-8052-e88656cf1ef2"); // TODO: ?
                var userInDatabase = UserInDatabase.Build(-1L, anonymousGuid, "anonymous", UserType.Unknown, DateTimeOffset.Now);
                return User.Build(userInDatabase, new List<Layer>());
            }
            else
            {
                var guidString = claims.FirstOrDefault(c => c.Type == "id")?.Value;
                var groups = claims.Where(c => c.Type == "groups").Select(c => c.Value).ToArray();

                var writableLayers = groups.Where(g => g.StartsWith("/layer_writeaccess_")).Select(async g =>
                {
                    var match = Regex.Match(g, "layer_writeaccess_(.*)");
                    if (!match.Success) throw new Exception("Couldn't parse layer_writeaccess group name");
                    var layerName = match.Groups[1];
                    return await LayerModel.GetLayer(layerName.Value, trans);
                }).Select(t => t.Result).Where(l => l != null).ToList();

                var usertype = UserType.Unknown;
                if (groups.Contains("/humans"))
                    usertype = UserType.Human;
                else if (groups.Contains("/robots"))
                    usertype = UserType.Robot;

                var guid = new Guid(guidString);
                var userInDatabase = await UserModel.CreateOrUpdateFetchUser(username, guid, usertype, null);

                return User.Build(userInDatabase, writableLayers);
            }
        }
    }
}
