using Hangfire.Common;
using Landscape.Base.Entity;
using Landscape.Base.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LandscapeRegistry.Service
{
    public class CurrentUserService : ICurrentUserService
    {
        public CurrentUserService(IHttpContextAccessor httpContextAccessor, IUserInDatabaseModel userModel, 
            ILayerModel layerModel, IRegistryAuthorizationService authorizationService, IConfiguration configuration)
        {
            HttpContextAccessor = httpContextAccessor;
            UserModel = userModel;
            LayerModel = layerModel;
            AuthorizationService = authorizationService;
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }
        private IRegistryAuthorizationService AuthorizationService { get; }
        private IHttpContextAccessor HttpContextAccessor { get; }
        private IUserInDatabaseModel UserModel { get; }
        private ILayerModel LayerModel { get; }

        public async Task<User> GetCurrentUser(NpgsqlTransaction trans)
        {
            // TODO: caching
            return await CreateUserFromClaims(HttpContextAccessor.HttpContext.User.Claims, trans);
        }

        public string GetUsernameFromClaims(IEnumerable<Claim> claims)
        {
            return claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value;
        }

        private async Task<User> CreateUserFromClaims(IEnumerable<Claim> claims, NpgsqlTransaction trans)
        {
            var username = GetUsernameFromClaims(claims);

            if (username == null)
            {
                var anonymousGuid = new Guid("2544f9a7-cc17-4cba-8052-e88656cf1ef2"); // TODO: ?
                var userInDatabase = UserInDatabase.Build(-1L, anonymousGuid, "anonymous", UserType.Unknown, DateTimeOffset.Now);
                return User.Build(userInDatabase, new List<Layer>());
            }
            else
            {
                var guidString = claims.FirstOrDefault(c => c.Type == "id")?.Value;
                //var groups = claims.Where(c => c.Type == "groups").Select(c => c.Value).ToArray();

                // extract client roles
                var resourceAccessStr = claims.Where(c => c.Type == "resource_access").FirstOrDefault()?.Value;
                var resourceAccess = JObject.Parse(resourceAccessStr);
                var resourceName = Configuration.GetSection("Authentication")["Audience"];
                var clientRoles = resourceAccess?[resourceName]?["roles"]?.Select(tt => tt.Value<string>()).ToArray() ?? new string[] { };

                var writableLayers = new List<Layer>();
                foreach (var role in clientRoles) {
                    var layerName = AuthorizationService.ParseLayerNameFromWriteAccessRoleName(role);
                    if (layerName != null)
                    {
                        var layer = await LayerModel.GetLayer(layerName, trans);
                        if (layer != null)
                            writableLayers.Add(layer);
                    }
                }

                var usertype = UserType.Unknown;
                if (clientRoles.Contains("human"))
                    usertype = UserType.Human;
                else if (clientRoles.Contains("robot"))
                    usertype = UserType.Robot;

                var guid = new Guid(guidString); // TODO: check for null, handle case
                var userInDatabase = await UserModel.UpsertUser(username, guid, usertype, null);

                return User.Build(userInDatabase, writableLayers);
            }
        }
    }
}
