using Autofac;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Authz;
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

        public async Task<IAuthenticatedUser> GetCurrentUser(IModelContext trans)
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

        private IAuthenticatedUser? cached = null;
        private readonly SemaphoreLocker cachedLock = new SemaphoreLocker();

        private AuthRoleModel AuthRoleModel { get; }
        private IHttpContextAccessor HttpContextAccessor { get; }
        public ILayerModel LayerModel { get; }
        public IMetaConfigurationModel MetaConfigurationModel { get; }

        public async Task<IAuthenticatedUser> GetCurrentUser(IModelContext trans)
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
            var httpUser = HttpUserUtils.CreateHttpUserFromClaims(HttpContextAccessor.HttpContext!.User.Claims, configuration.GetSection("Authentication")["Audience"], logger);
            return httpUser.Username;
        }

        private async Task<IAuthenticatedUser> _GetCurrentUser(IModelContext trans)
        {
            var httpUser = HttpUserUtils.CreateHttpUserFromClaims(HttpContextAccessor.HttpContext!.User.Claims, configuration.GetSection("Authentication")["Audience"], logger);
            return await HttpUserUtils.CreateAuthenticatedUserFromHTTPUser(httpUser, userModel, LayerModel, MetaConfigurationModel, AuthRoleModel, trans);
        }
    }

    public class CurrentAuthorizedCLBUserService : ICurrentUserService
    {
        public CurrentAuthorizedCLBUserService(string username, IUserInDatabaseModel userModel)
        {
            this.username = username;
            this.userModel = userModel;
        }

        private readonly string username;
        private readonly IUserInDatabaseModel userModel;

        private IAuthenticatedUser? cached = null;
        private readonly SemaphoreLocker cachedLock = new SemaphoreLocker();

        public async Task<IAuthenticatedUser> GetCurrentUser(IModelContext trans)
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

        private async Task<IAuthenticatedUser> _GetCurrentUser(IModelContext trans)
        {
            // upsert user
            var username = GetCurrentUsername();
            var displayName = username;
            // generate a unique but deterministic GUID from the clb Name
            var clbUserGuidNamespace = new Guid("2544f9a7-cc17-4cba-8052-e88656cf1ef1");
            var guid = GuidUtility.Create(clbUserGuidNamespace, username);
            var user = await userModel.UpsertUser(username, displayName, guid, UserType.Robot, trans);

            return new AuthenticatedInternalUser(user);
        }
    }

    public class CurrentAuthorizedMarkedForDeletionUserService : ICurrentUserService
    {
        public CurrentAuthorizedMarkedForDeletionUserService(IUserInDatabaseModel userModel)
        {
            this.userModel = userModel;
        }

        private readonly IUserInDatabaseModel userModel;

        private IAuthenticatedUser? cached = null;
        private readonly SemaphoreLocker cachedLock = new SemaphoreLocker();

        public string GetCurrentUsername()
        {
            return $"__marked_for_deletion";
        }

        public async Task<IAuthenticatedUser> GetCurrentUser(IModelContext trans)
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

        private async Task<IAuthenticatedUser> _GetCurrentUser(IModelContext trans)
        {
            // upsert user
            var username = GetCurrentUsername();
            var displayName = username;
            var userGuid = new Guid("2544f9a7-cc17-4cba-8052-e88656cf1ef2");
            var user = await userModel.UpsertUser(username, displayName, userGuid, UserType.Robot, trans);

            return new AuthenticatedInternalUser(user);
        }
    }

}