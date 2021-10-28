using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Omnikeeper.Base.CLB;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
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
        public CurrentUserService(IHttpContextAccessor httpContextAccessor, CLBContextAccessor clbContextAccessor,
            CurrentHTTPUserService httpService, CurrentCLBUserService clbService, ILayerModel layerModel, IMetaConfigurationModel metaConfigurationModel, 
            GenericTraitEntityModel<AuthRole, string> authRoleModel)
        {
            HttpContextAccessor = httpContextAccessor;
            this.clbContextAccessor = clbContextAccessor;
            this.httpService = httpService;
            this.clbService = clbService;
            LayerModel = layerModel;
            MetaConfigurationModel = metaConfigurationModel;
            AuthRoleModel = authRoleModel;
        }

        private readonly CLBContextAccessor clbContextAccessor;
        private readonly CurrentHTTPUserService httpService;
        private readonly CurrentCLBUserService clbService;

        private GenericTraitEntityModel<AuthRole, string> AuthRoleModel { get; }
        private IHttpContextAccessor HttpContextAccessor { get; }
        public ILayerModel LayerModel { get; }
        public IMetaConfigurationModel MetaConfigurationModel { get; }

        public async Task<AuthenticatedUser> GetCurrentUser(IModelContext trans)
        {
            if (clbContextAccessor.CLBContext != null)
            {
                // CLBs implicitly have all permissions
                var suar = await PermissionUtils.GetSuperUserAuthRole(LayerModel, trans);
                var user = await clbService.CreateAndGetCurrentUser(clbContextAccessor.CLBContext, trans);
                return new AuthenticatedUser(user, new AuthRole[] { suar });
            }
            else if (HttpContextAccessor.HttpContext != null)
            {
                // TODO: caching
                var (userInDatabase, httpUser) = await httpService.CreateAndGetCurrentUser(HttpContextAccessor.HttpContext, trans);

                if (httpUser.ClientRoles.Contains("__ok_superuser"))
                {
                    var suar = await PermissionUtils.GetSuperUserAuthRole(LayerModel, trans);
                    return new AuthenticatedUser(userInDatabase, new AuthRole[] { suar });
                }
                else
                {
                    var metaConfiguration = await MetaConfigurationModel.GetConfigOrDefault(trans);

                    var allAuthRoles = await AuthRoleModel.GetAllByDataID(metaConfiguration.ConfigLayerset, trans, TimeThreshold.BuildLatest());

                    var activeAuthRoles = new List<AuthRole>();
                    foreach (var role in httpUser.ClientRoles)
                    {
                        if (allAuthRoles.TryGetValue(role, out var authRole))
                        {
                            activeAuthRoles.Add(authRole);
                        }
                    }

                    // order auth roles of user by ID, so they are consistent
                    activeAuthRoles.Sort((a, b) => a.ID.CompareTo(b.ID));

                    return new AuthenticatedUser(userInDatabase, activeAuthRoles.ToArray());
                }

            }
            else
            {
                throw new Exception("Could not get current user: executing in unknown context");
            }
        }
    }

    public class CurrentUserInDatabaseService : ICurrentUserInDatabaseService
    {
        public CurrentUserInDatabaseService(IHttpContextAccessor httpContextAccessor, CLBContextAccessor clbContextAccessor, CurrentHTTPUserService httpService, CurrentCLBUserService clbService)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.clbContextAccessor = clbContextAccessor;
            this.httpService = httpService;
            this.clbService = clbService;
        }

        private readonly CLBContextAccessor clbContextAccessor;
        private readonly CurrentHTTPUserService httpService;
        private readonly CurrentCLBUserService clbService;

        private readonly IHttpContextAccessor httpContextAccessor;

        public async Task<UserInDatabase> CreateAndGetCurrentUser(IModelContext trans)
        {
            if (clbContextAccessor.CLBContext != null)
            {
                return await clbService.CreateAndGetCurrentUser(clbContextAccessor.CLBContext, trans);
            }
            else if (httpContextAccessor.HttpContext != null)
            {
                var (userInDatabase, _) = await httpService.CreateAndGetCurrentUser(httpContextAccessor.HttpContext, trans);

                return userInDatabase;
            }
            else
            {
                throw new Exception("Could not get current user: executing in unknown context");
            }
        }
    }

    public class CurrentCLBUserService
    {
        private readonly IUserInDatabaseModel userModel;

        public CurrentCLBUserService(IUserInDatabaseModel userModel)
        {
            this.userModel = userModel;
        }

        public async Task<UserInDatabase> CreateAndGetCurrentUser(CLBContext clbContext, IModelContext trans)
        {
            // upsert user
            var username = $"__cl.{clbContext.Brain.Name}"; // make username the same as CLB name
            var displayName = username;
            // generate a unique but deterministic GUID from the clb Name
            var clbUserGuidNamespace = new Guid("2544f9a7-cc17-4cba-8052-e88656cf1ef1");
            var guid = GuidUtility.Create(clbUserGuidNamespace, clbContext.Brain.Name);
            var user = await userModel.UpsertUser(username, displayName, guid, UserType.Robot, trans);
            return user;
        }
    }

    public class CurrentHTTPUserService
    {
        public CurrentHTTPUserService(IUserInDatabaseModel userModel, IConfiguration configuration, ILogger<CurrentHTTPUserService> logger)
        {
            UserModel = userModel;
            Configuration = configuration;
            Logger = logger;
        }

        private IConfiguration Configuration { get; }
        public ILogger<CurrentHTTPUserService> Logger { get; }
        private IUserInDatabaseModel UserModel { get; }

        public async Task<(UserInDatabase userInDatabase, HttpUser httpUser)> CreateAndGetCurrentUser(HttpContext httpContext, IModelContext trans)
        {
            // TODO: caching?
            var httpUser = HttpUserUtils.CreateUserFromHttpContext(httpContext, Configuration, Logger);
            var userInDatabase = await UserModel.UpsertUser(httpUser.Username, httpUser.DisplayName, httpUser.UserID, httpUser.UserType, trans);

            return (userInDatabase, httpUser);
        }
    }

    public class HttpUser
    {
        public readonly Guid UserID;
        public readonly string Username;
        public readonly string DisplayName;
        public readonly ISet<string> ClientRoles;
        public readonly UserType UserType;

        public HttpUser(string username, string displayName, Guid userID, UserType userType, ISet<string> clientRoles)
        {
            UserID = userID;
            Username = username;
            DisplayName = displayName;
            ClientRoles = clientRoles;
            UserType = userType;
        }
    }

    public static class HttpUserUtils
    {
        public static string? GetUsernameFromClaims(IEnumerable<Claim> claims)
        {
            return claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value;
        }

        public static HttpUser CreateUserFromHttpContext(HttpContext httpContext, IConfiguration configuration, ILogger logger)
        {
            var claims = httpContext.User.Claims;
            var username = GetUsernameFromClaims(claims);

            if (username == null)
            {
                var anonymousGuid = new Guid("2544f9a7-cc17-4cba-8052-e88656cf1ef2"); // TODO: ?
                return new HttpUser("anonymous", "anonymous", anonymousGuid, UserType.Unknown, new HashSet<string>());
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
                var resourceName = configuration.GetSection("Authentication")["Audience"];
                var claimRoles = resourceAccess[resourceName]?["roles"];
                var clientRoles = new HashSet<string>();
                if (claimRoles == null)
                {
                    logger.LogWarning($"Cannot parse roles in user token for user {username}: key-path \"resource_access\"->\"{resourceName}\"->\"roles\" not found; either no roles assigned or token structure invalid");
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

                return new HttpUser(username, displayName, guid, usertype, clientRoles);
            }
        }
    }
}