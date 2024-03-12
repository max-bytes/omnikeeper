using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Authz
{
    public class HttpUser
    {
        public readonly Guid UserID;
        public readonly string Username;
        public readonly string DisplayName;
        public readonly ISet<string> ClientRoles;
        public readonly IEnumerable<Claim> Claims;
        public readonly UserType UserType;

        public HttpUser(string username, string displayName, Guid userID, UserType userType, ISet<string> clientRoles, IEnumerable<Claim> claims)
        {
            UserID = userID;
            Username = username;
            DisplayName = displayName;
            ClientRoles = clientRoles;
            Claims = claims;
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

        public static async Task<IAuthenticatedUser> CreateAuthenticatedUserFromHTTPUser(HttpUser httpUser, IUserInDatabaseModel userModel, ILayerModel LayerModel,
            IMetaConfigurationModel MetaConfigurationModel, AuthRoleModel AuthRoleModel, IModelContext trans)
        {
            var userInDatabase = await userModel.UpsertUser(httpUser.Username, httpUser.DisplayName, httpUser.UserID, httpUser.UserType, trans);

            if (HasSuperUserClientRole(httpUser))
            {
                var suar = await PermissionUtils.GetSuperUserAuthRole(LayerModel, trans);
                return new AuthenticatedHttpUser(userInDatabase, new AuthRole[] { suar }, httpUser);
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

                return new AuthenticatedHttpUser(userInDatabase, activeAuthRoles.ToArray(), httpUser);
            }
        }

        public static HttpUser CreateAnonymousHttpUser(IEnumerable<Claim> claims)
        {
            var anonymousGuid = new Guid("2544f9a7-cc17-4cba-8052-e88656cf1ef2"); // TODO: ?
            return new HttpUser("anonymous", "anonymous", anonymousGuid, UserType.Unknown, new HashSet<string>(), claims);
        }

        public static HttpUser CreateHttpUserFromClaimsPrincipal(ClaimsPrincipal claimsPrincipal, string audience, ILogger logger)
        {
            var claims = claimsPrincipal.Claims;
            var username = GetUsernameFromClaims(claims);

            if (username == null)
            {
                return CreateAnonymousHttpUser(claims);
            }
            else
            {
                var guidString = claimsPrincipal.FindFirstValue("id") ?? claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
                if (guidString == null)
                {
                    throw new Exception($"Cannot parse user id inside user token: neither key \"id\" nor key {ClaimTypes.NameIdentifier} present");
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

                return new HttpUser(username, displayName, guid, usertype, clientRoles, claims);
            }
        }
    }
}
