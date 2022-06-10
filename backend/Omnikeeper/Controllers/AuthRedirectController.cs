using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Omnikeeper.Controllers
{
    [ApiController]
    [ApiVersionNeutral]
    [Route(".well-known/openid-configuration")]
    public class AuthRedirectController : ControllerBase
    {
        private readonly IConfiguration configuration;

        public AuthRedirectController(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var defaultBaseURL = configuration.GetSection("Authentication")["Authority"];
            var baseURL = configuration.GetSection("Authentication").GetValue<string>("AuthorityForFrontend", defaultBaseURL);
            return Redirect($"{baseURL}/.well-known/openid-configuration");
        }
    }
}
