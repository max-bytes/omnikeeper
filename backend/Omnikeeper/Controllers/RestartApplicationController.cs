using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils.ModelContext;
using System.Threading.Tasks;

namespace Omnikeeper.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class RestartApplicationController : Controller
    {
        private readonly IHostApplicationLifetime appLifetime;
        private readonly IModelContextBuilder modelContextBuilder;
        private readonly ICurrentUserService currentUserService;
        private readonly IManagementAuthorizationService managementAuthorizationService;

        public RestartApplicationController(IHostApplicationLifetime appLifetime, IModelContextBuilder modelContextBuilder, ICurrentUserService currentUserService, IManagementAuthorizationService managementAuthorizationService)
        {
            this.appLifetime = appLifetime;
            this.modelContextBuilder = modelContextBuilder;
            this.currentUserService = currentUserService;
            this.managementAuthorizationService = managementAuthorizationService;
        }

        [HttpGet("restart")]
        public async Task<IActionResult> Restart()
        {
            var trans = modelContextBuilder.BuildImmediate();
            var user = await currentUserService.GetCurrentUser(trans);
            if (!managementAuthorizationService.HasManagementPermission(user))
                return Forbid($"User \"{user.Username}\" does not have permission to restart");

            // NOTE: while this code only stops the application (and doesn't restart it), omnikeeper is normally run in IIS inside a docker container, which DOES automatically restart after being stopped
            appLifetime.StopApplication();
            return new EmptyResult();
        }
    }
}
