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

        public IActionResult Index()
        {
            var baseURL = configuration.GetSection("Authentication")["Authority"];
            return Redirect($"{baseURL}/.well-known/openid-configuration");
        }
    }
}
