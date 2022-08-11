using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Model.Config;
using Omnikeeper.Service;
using System;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace Omnikeeper.Controllers.OData
{
    public class ODataAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly IModelContextBuilder modelContextBuilder;
        private readonly ODataAPIContextModel oDataAPIContextModel;
        private readonly IMetaConfigurationModel metaConfigurationModel;

        public ODataAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock, IModelContextBuilder modelContextBuilder, ODataAPIContextModel oDataAPIContextModel, IMetaConfigurationModel metaConfigurationModel
            ) : base(options, logger, encoder, clock)
        {
            this.modelContextBuilder = modelContextBuilder;
            this.oDataAPIContextModel = oDataAPIContextModel;
            this.metaConfigurationModel = metaConfigurationModel;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var tmp = Request.RouteValues;
            if (tmp.TryGetValue("context", out var odataContextObject) && odataContextObject is string odataContext)
            { 
                using var trans = modelContextBuilder.BuildImmediate();
                var timeThreshold = TimeThreshold.BuildLatest();

                var authConfig = await ODataAPIContextService.GetAuthConfigFromContext(oDataAPIContextModel, metaConfigurationModel, odataContext, trans, timeThreshold);

                switch (authConfig)
                {
                    case ODataAPIContext.ContextAuthNone _:
                        {
                            var identity = new ClaimsIdentity(Array.Empty<Claim>(), "None");
                            var claimsPrincipal = new ClaimsPrincipal(identity);
                            return AuthenticateResult.Success(new AuthenticationTicket(claimsPrincipal, Scheme.Name));
                        }
                    case ODataAPIContext.ContextAuthBasic b:
                        var authHeader = Request.Headers["Authorization"].ToString();
                        if (authHeader == null || !authHeader.StartsWith("basic", StringComparison.OrdinalIgnoreCase))
                        {
                            Response.StatusCode = 401;
                            Response.Headers.Add("WWW-Authenticate", $"Basic realm=\"omnikeeper odata with context {odataContext}\"");
                            return AuthenticateResult.Fail("Invalid Authorization Header");
                        }
                        var token = authHeader.Substring("Basic ".Length).Trim();
                        var credentialstring = Encoding.UTF8.GetString(Convert.FromBase64String(token));
                        var credentials = credentialstring.Split(':');

                        if (credentials.Length != 2)
                        {
                            Response.StatusCode = 401;
                            Response.Headers.Add("WWW-Authenticate", $"Basic realm=\"omnikeeper odata with context {odataContext}\"");
                            return AuthenticateResult.Fail("Invalid Authorization Header");
                        }

                        var username = credentials[0];
                        var password = credentials[1];
                        if (username == b.Username)
                        {
                            var passwordHasher = new PasswordHasher<string>();
                            var r = passwordHasher.VerifyHashedPassword(username, b.PasswordHashed, password);
                            if (r == PasswordVerificationResult.Success)
                            {
                                // TODO: if we want to do any kind of user stats tracking, we need to set this properly
                                var claims = new[] { new Claim("name", credentials[0]) };
                                var identity = new ClaimsIdentity(claims, "Basic");
                                var claimsPrincipal = new ClaimsPrincipal(identity);
                                return AuthenticateResult.Success(new AuthenticationTicket(claimsPrincipal, Scheme.Name));
                            }
                        }

                        Response.StatusCode = 401;
                        Response.Headers.Add("WWW-Authenticate", $"Basic realm=\"omnikeeper odata with context {odataContext}\"");
                        return AuthenticateResult.Fail("Invalid credentials");

                    default:
                        throw new Exception("Invalid ContextAuth encountered");
                }
            }
            else
            {
                throw new Exception("Improper odata URL structure detected");
            }
        }
    }
}
