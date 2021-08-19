using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
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

        public CurrentUserService(IHttpContextAccessor httpContextAccessor, IUserInDatabaseModel userModel, ILayerModel layerModel,
            IAuthRoleModel authRoleModel, IConfiguration configuration, ILogger<CurrentUserService> logger)
        {
            HttpContextAccessor = httpContextAccessor;
            UserModel = userModel;
            LayerModel = layerModel;
            AuthRoleModel = authRoleModel;
            Configuration = configuration;
            Logger = logger;
        }

        private IAuthRoleModel AuthRoleModel { get; }
        private IConfiguration Configuration { get; }
        public ILogger<CurrentUserService> Logger { get; }
        private IHttpContextAccessor HttpContextAccessor { get; }
        private IUserInDatabaseModel UserModel { get; }
        public ILayerModel LayerModel { get; }

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
                return new AuthenticatedUser(userInDatabase, new HashSet<string>() { });
            }
            else
            {
                var guidString = claims.FirstOrDefault(c => c.Type == "id")?.Value;
                if (guidString == null)
                {
                    throw new Exception("Cannot parse user id inside user token: key \"id\" not present");
                }
                var guid = new Guid(guidString);

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
                var clientRoles = new HashSet<string>();
                if (claimRoles == null)
                {
                    Logger.LogWarning($"Cannot parse roles in user token for user {username}: key-path \"resource_access\"->\"{resourceName}\"->\"roles\" not found; either no roles assigned or token structure invalid");

                }
                else
                {
                    clientRoles = claimRoles.Select(tt => tt.Value<string>()).ToHashSet();
                }

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

                var finalPermissions = new HashSet<string>();
                if (clientRoles.Contains("__ok_superuser"))
                {
                    var allPermissions = await PermissionUtils.GetAllAvailablePermissions(LayerModel, trans);
                    finalPermissions.UnionWith(allPermissions);
                }
                else
                {
                    var authRoles = await AuthRoleModel.GetAuthRoles(trans, TimeThreshold.BuildLatest());
                    foreach (var role in clientRoles)
                    {
                        if (authRoles.TryGetValue(role, out var authRole))
                        {
                            finalPermissions.UnionWith(authRole.Permissions);
                        }
                    }
                }



                return new AuthenticatedUser(userInDatabase, finalPermissions);
            }
        }
    }
}
