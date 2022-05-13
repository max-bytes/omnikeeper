using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Omnikeeper.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class UsageStatsController : Controller
    {
        private readonly IUsageStatsModel usageStatsModel;
        private readonly IModelContextBuilder modelContextBuilder;
        private readonly ICurrentUserAccessor currentUserAccessor;
        private readonly IManagementAuthorizationService managementAuthorizationService;

        public UsageStatsController(IUsageStatsModel usageStatsModel, IModelContextBuilder modelContextBuilder, ICurrentUserAccessor currentUserAccessor, IManagementAuthorizationService managementAuthorizationService)
        {
            this.usageStatsModel = usageStatsModel;
            this.modelContextBuilder = modelContextBuilder;
            this.currentUserAccessor = currentUserAccessor;
            this.managementAuthorizationService = managementAuthorizationService;
        }

        [HttpGet("fetch")]
        public async Task<IActionResult> Fetch([FromQuery, Required]DateTimeOffset from, [FromQuery, Required] DateTimeOffset to)
        {
            using var trans = modelContextBuilder.BuildImmediate();

            var user = await currentUserAccessor.GetCurrentUser(trans);
            if (!managementAuthorizationService.HasManagementPermission(user))
            {
                return Forbid();
            }

            var elements = await usageStatsModel.GetElements(from, to, trans);

            var options = new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter() },
                IncludeFields = true,
            };

            return Json(elements, options);
        }
    }
}
