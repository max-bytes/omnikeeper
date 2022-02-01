using Autofac;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Service;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Startup
{
    /// <summary>
    /// NOTE: this is a really nasty implementation, but hangfire does not provide any other way to do authorization for its dashboard
    /// </summary>
    public class HangFireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        private static readonly string HangFireCookieName = "HangFireCookie";
        private static readonly int CookieExpirationMinutes = 20;
        private readonly TokenValidationParameters tokenValidationParameters;
        private readonly string authority;
        private readonly string audience;
        private ILogger<HangFireAuthorizationFilter> logger;

        public HangFireAuthorizationFilter(TokenValidationParameters tokenValidationParameters, string authority, string audience, ILogger<HangFireAuthorizationFilter> logger)
        {
            this.tokenValidationParameters = tokenValidationParameters;
            this.authority = authority;
            this.audience = audience;
            this.logger = logger;
        }
        public bool Authorize(DashboardContext context)
        {
            return AuthorizeAsync(context).GetAwaiter().GetResult();
        }

        private async Task<bool> AuthorizeAsync(DashboardContext context)
        {
            string? access_token;
            var setCookie = false;

            // try to get token from query string
            // TODO: not ideal to pass the access_token via a query string, but it's the only feasible way for now..., improve this
            var httpContext = context.GetHttpContext();
            if (httpContext.Request.Query.ContainsKey("access_token"))
            {
                access_token = httpContext.Request.Query["access_token"].FirstOrDefault();
                setCookie = true;
            }
            else
            {
                access_token = httpContext.Request.Cookies[HangFireCookieName];
            }

            if (string.IsNullOrEmpty(access_token))
            {
                return false;
            }

            if (setCookie)
            {
                httpContext.Response.Cookies.Append(HangFireCookieName,
                access_token,
                new CookieOptions()
                {
                    Expires = DateTime.Now.AddMinutes(CookieExpirationMinutes)
                });
            }

            try
            {
                var dr = new HttpDocumentRetriever() { RequireHttps = false };
                var openidConfigManaged = new ConfigurationManager<OpenIdConnectConfiguration>(
                    $"{authority}/.well-known/openid-configuration",
                    new OpenIdConnectConfigurationRetriever(),
                    dr);
                var config = await openidConfigManaged.GetConfigurationAsync();

                JwtSecurityTokenHandler hand = new JwtSecurityTokenHandler();

                // NOTE: for some reason, when doing a manual token validation, we need to set the issuer signing keys and the issuer
                // the middleware that's used for regular JWT authentication somehow does this under-the-hood and does not require setting these...
                // but here we DO need them
                // see, for example: https://stackoverflow.com/questions/58758198/does-addjwtbearer-do-what-i-think-it-does
                tokenValidationParameters.IssuerSigningKeys = config.SigningKeys;
                tokenValidationParameters.ValidIssuer = authority;
                var claims = hand.ValidateToken(access_token, tokenValidationParameters, out var validatedToken);

                // check management permissions
                var httpUser = HttpUserUtils.CreateUserFromClaims(claims.Claims, audience, logger);
                return HttpUserUtils.HasSuperUserClientRole(httpUser);

                // NOTE: the following does not work because of DI troubles and async disposal problems
                //using var trans = modelContextBuilder.BuildImmediate();
                //var authenticatedUser = await HttpUserUtils.CreateAuthenticationUserFromHTTPUser(httpUser, userModel, layerModel, metaConfigurationModel, authRoleModel, trans);
                //return managementAuthorizationService.HasManagementPermission(authenticatedUser);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error during dashboard hangfire jwt validation process");
                throw;
            }
        }
    }
}
