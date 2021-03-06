using Autofac;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Omnikeeper.Base.CLB;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace Omnikeeper.Service
{
    public class CurrentUserAccessor : ICurrentUserAccessor
    {
        private readonly ScopedLifetimeAccessor scopedLifetimeAccessor;

        public CurrentUserAccessor(ScopedLifetimeAccessor scopedLifetimeAccessor)
        {
            this.scopedLifetimeAccessor = scopedLifetimeAccessor;
        }

        public async Task<AuthenticatedUser> GetCurrentUser(IModelContext trans)
        {
            var scope = scopedLifetimeAccessor.GetLifetimeScope();
            if (scope == null)
                throw new Exception("Cannot get current user: not in proper scope");
            return await scope.Resolve<ICurrentUserService>().GetCurrentUser(trans);
        }

        public string GetCurrentUsername()
        {
            var scope = scopedLifetimeAccessor.GetLifetimeScope();
            if (scope == null)
                throw new Exception("Cannot get current username: not in proper scope");
            return scope.Resolve<ICurrentUserService>().GetCurrentUsername();
        }
    }

    public class CurrentAuthorizedHttpUserService : ICurrentUserService
    {
        public CurrentAuthorizedHttpUserService(IHttpContextAccessor httpContextAccessor,
            ILayerModel layerModel, IMetaConfigurationModel metaConfigurationModel,
            IUserInDatabaseModel userModel, IConfiguration configuration, ILogger<CurrentAuthorizedHttpUserService> logger,
            AuthRoleModel authRoleModel)
        {
            HttpContextAccessor = httpContextAccessor;
            LayerModel = layerModel;
            MetaConfigurationModel = metaConfigurationModel;
            this.userModel = userModel;
            this.configuration = configuration;
            this.logger = logger;
            AuthRoleModel = authRoleModel;
        }

        private readonly IUserInDatabaseModel userModel;
        private readonly IConfiguration configuration;
        private readonly ILogger<CurrentAuthorizedHttpUserService> logger;

        private AuthenticatedUser? cached = null;
        private readonly SemaphoreLocker cachedLock = new SemaphoreLocker();

        private AuthRoleModel AuthRoleModel { get; }
        private IHttpContextAccessor HttpContextAccessor { get; }
        public ILayerModel LayerModel { get; }
        public IMetaConfigurationModel MetaConfigurationModel { get; }

        public async Task<AuthenticatedUser> GetCurrentUser(IModelContext trans)
        {
            return await cachedLock.LockAsync(async () =>
            {
                if (cached == null)
                {
                    cached = await _GetCurrentUser(trans);
                }
                return cached;
            });
        }

        public string GetCurrentUsername()
        {
            var httpUser = HttpUserUtils.CreateUserFromClaims(HttpContextAccessor.HttpContext!.User.Claims, configuration.GetSection("Authentication")["Audience"], logger);
            return httpUser.Username;
        }

        private async Task<AuthenticatedUser> _GetCurrentUser(IModelContext trans)
        {
            var httpUser = HttpUserUtils.CreateUserFromClaims(HttpContextAccessor.HttpContext!.User.Claims, configuration.GetSection("Authentication")["Audience"], logger);
            return await HttpUserUtils.CreateAuthenticationUserFromHTTPUser(httpUser, userModel, LayerModel, MetaConfigurationModel, AuthRoleModel, trans);
        }
    }

    public class CurrentAuthorizedCLBUserService : ICurrentUserService
    {
        public CurrentAuthorizedCLBUserService(string username, IUserInDatabaseModel userModel, ILayerModel layerModel)
        {
            this.username = username;
            this.userModel = userModel;
            this.layerModel = layerModel;
        }

        private readonly ILayerModel layerModel;
        private readonly string username;
        private readonly IUserInDatabaseModel userModel;

        private AuthenticatedUser? cached = null;
        private readonly SemaphoreLocker cachedLock = new SemaphoreLocker();

        public async Task<AuthenticatedUser> GetCurrentUser(IModelContext trans)
        {
            return await cachedLock.LockAsync(async () =>
            {
                if (cached == null)
                {
                    cached = await _GetCurrentUser(trans);
                }
                return cached;
            });
        }

        public string GetCurrentUsername()
        {
            return username;
        }

        private async Task<AuthenticatedUser> _GetCurrentUser(IModelContext trans)
        {
            // CLBs implicitly have all permissions
            // TODO: become more restricive, just allow writing to target layer and reading from all
            var suar = await PermissionUtils.GetSuperUserAuthRole(layerModel, trans);

            // upsert user
            var username = GetCurrentUsername();
            var displayName = username;
            // generate a unique but deterministic GUID from the clb Name
            var clbUserGuidNamespace = new Guid("2544f9a7-cc17-4cba-8052-e88656cf1ef1");
            var guid = GuidUtility.Create(clbUserGuidNamespace, username);
            var user = await userModel.UpsertUser(username, displayName, guid, UserType.Robot, trans);

            return new AuthenticatedUser(user, new AuthRole[] { suar });
        }
    }

    public class CurrentAuthorizedMarkedForDeletionUserService : ICurrentUserService
    {
        public CurrentAuthorizedMarkedForDeletionUserService(IUserInDatabaseModel userModel, ILayerModel layerModel)
        {
            this.userModel = userModel;
            this.layerModel = layerModel;
        }

        private readonly ILayerModel layerModel;
        private readonly IUserInDatabaseModel userModel;

        private AuthenticatedUser? cached = null;
        private readonly SemaphoreLocker cachedLock = new SemaphoreLocker();

        public string GetCurrentUsername()
        {
            return $"__marked_for_deletion";
        }

        public async Task<AuthenticatedUser> GetCurrentUser(IModelContext trans)
        {
            return await cachedLock.LockAsync(async () =>
            {
                if (cached == null)
                {
                    cached = await _GetCurrentUser(trans);
                }
                return cached;
            });
        }

        private async Task<AuthenticatedUser> _GetCurrentUser(IModelContext trans)
        {
            // user implicitly has all permissions
            var suar = await PermissionUtils.GetSuperUserAuthRole(layerModel, trans);

            // upsert user
            var username = GetCurrentUsername();
            var displayName = username;
            var userGuid = new Guid("2544f9a7-cc17-4cba-8052-e88656cf1ef2");
            var user = await userModel.UpsertUser(username, displayName, userGuid, UserType.Robot, trans);

            return new AuthenticatedUser(user, new AuthRole[] { suar });
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

        public static bool HasSuperUserClientRole(HttpUser httpUser)
        {
            return httpUser.ClientRoles.Contains("__ok_superuser");
        }

        public static async Task<AuthenticatedUser> CreateAuthenticationUserFromHTTPUser(HttpUser httpUser, IUserInDatabaseModel userModel, ILayerModel LayerModel,
            IMetaConfigurationModel MetaConfigurationModel, AuthRoleModel AuthRoleModel, IModelContext trans)
        {
            var userInDatabase = await userModel.UpsertUser(httpUser.Username, httpUser.DisplayName, httpUser.UserID, httpUser.UserType, trans);

            if (HasSuperUserClientRole(httpUser))
            {
                var suar = await PermissionUtils.GetSuperUserAuthRole(LayerModel, trans);
                return new AuthenticatedUser(userInDatabase, new AuthRole[] { suar });
            }
            else
            {
                var metaConfiguration = await MetaConfigurationModel.GetConfigOrDefault(trans);

                var allAuthRoles = await AuthRoleModel.GetByDataID(AllCIIDsSelection.Instance, metaConfiguration.ConfigLayerset, trans, TimeThreshold.BuildLatest());

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

        public static HttpUser CreateUserFromClaims(IEnumerable<Claim> claims, string audience, ILogger logger)
        {
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
                using var resourceAccess = JsonDocument.Parse(resourceAccessStr);
                if (resourceAccess == null)
                {
                    throw new Exception("Cannot parse roles in user token: Cannot parse resource_access JSON value");
                }
                var resourceName = audience;
                var clientRoles = new HashSet<string>();
                try
                {
                    var claimRoles = resourceAccess.RootElement.GetProperty(resourceName).GetProperty("roles").EnumerateArray();
                    clientRoles = claimRoles.Select(tt => tt.GetString()!).ToHashSet();
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, $"Cannot parse roles in user token for user {username}: key-path \"resource_access\"->\"{resourceName}\"->\"roles\" not found; either no roles assigned or token structure invalid");
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