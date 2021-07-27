using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Omnikeeper.Service
{
    public class CurrentUserService : ICurrentUserService
    {
        public CurrentUserService(IHttpContextAccessor httpContextAccessor, IUserInDatabaseModel userModel,
            ILayerModel layerModel, ILayerBasedAuthorizationService authorizationService, IConfiguration configuration)
        {
            HttpContextAccessor = httpContextAccessor;
            UserModel = userModel;
            LayerModel = layerModel;
            AuthorizationService = authorizationService;
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }
        private ILayerBasedAuthorizationService AuthorizationService { get; }
        private IHttpContextAccessor HttpContextAccessor { get; }
        private IUserInDatabaseModel UserModel { get; }
        private ILayerModel LayerModel { get; }

        public async Task<AuthenticatedUser> GetCurrentUser(IModelContext trans)
        {
            // TODO: caching
            return await CreateUserFromClaims(HttpContextAccessor.HttpContext.User.Claims, trans);
        }

        public IEnumerable<(string type, string value)> DebugGetAllClaims()
        {
            return HttpContextAccessor.HttpContext.User.Claims.Select(c => (c.Type, c.Value));
        }

        public string? GetUsernameFromClaims(IEnumerable<Claim> claims)
        {
            return claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value;
        }

        private async Task<AuthenticatedUser> CreateUserFromClaims(IEnumerable<Claim> claims, IModelContext trans)
        {
            var username = GetUsernameFromClaims(claims);

            if (username == null)
            {
                var anonymousGuid = new Guid("2544f9a7-cc17-4cba-8052-e88656cf1ef2"); // TODO: ?
                var userInDatabase = await UserModel.UpsertUser("anonymous", "anonymous", anonymousGuid, UserType.Unknown, trans);
                return new AuthenticatedUser(userInDatabase, new List<Layer>());
            }
            else
            {
                var guidString = claims.FirstOrDefault(c => c.Type == "id")?.Value;
                if (guidString == null)
                {
                    throw new Exception("Cannot parse user id inside user token: key \"id\" not present");
                }
                var guid = new Guid(guidString);

                //var groups = claims.Where(c => c.Type == "groups").Select(c => c.Value).ToArray();

                // cached list of writable layers
                var writableLayers = await AuthorizationService.GetWritableLayersForUser(claims, LayerModel, trans);

                // extract client roles
                var resourceAccessStr = claims.Where(c => c.Type == "resource_access").FirstOrDefault()?.Value;
                if (resourceAccessStr == null)
                {
                    throw new Exception("Cannot parse roles in user token: key \"resource_access\" not found");
                }
                var resourceAccess = JObject.Parse(resourceAccessStr);
                if (resourceAccess == null)
                {
                    throw new Exception("Cannot parse roles in user token: Cannot parse resource_access JSON value");
                }
                var resourceName = Configuration.GetSection("Authentication")["Audience"];
                var claimRoles = resourceAccess[resourceName]?["roles"];
                if (claimRoles == null)
                {
                    throw new Exception($"Cannot parse roles in user token: key-path \"{resourceName}\"->\"roles\" not found");
                }
                var clientRoles = claimRoles.Select(tt => tt.Value<string>()).ToArray() ?? new string[] { };

                var usertype = UserType.Unknown;
                if (clientRoles.Contains("human"))
                    usertype = UserType.Human;
                else if (clientRoles.Contains("robot"))
                    usertype = UserType.Robot;

                var displayName = usertype switch
                {
                    UserType.Human => claims.FirstOrDefault(c => c.Type == "name")?.Value ?? username,
                    UserType.Robot => username,
                    UserType.Unknown => username,
                    _ => throw new Exception("Unknown UserType encountered")
                };

                var userInDatabase = await UserModel.UpsertUser(username, displayName, guid, usertype, trans);

                return new AuthenticatedUser(userInDatabase, writableLayers);
            }
        }
    }
}
