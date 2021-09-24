using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DTO;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class CISearchController : ControllerBase
    {
        private readonly ICISearchModel ciSearchModel;
        private readonly IModelContextBuilder modelContextBuilder;
        private readonly ILayerBasedAuthorizationService layerBasedAuthorizationService;
        private readonly ICurrentUserService currentUserService;

        public CISearchController(ICISearchModel ciSearchModel,
            IModelContextBuilder modelContextBuilder, ILayerBasedAuthorizationService layerBasedAuthorizationService, ICurrentUserService currentUserService)
        {
            this.ciSearchModel = ciSearchModel;
            this.modelContextBuilder = modelContextBuilder;
            this.layerBasedAuthorizationService = layerBasedAuthorizationService;
            this.currentUserService = currentUserService;
        }

        [HttpGet("searchCIsByTraits")]
        public async Task<ActionResult<IEnumerable<CIDTO>>> SearchCIsByTraits([FromQuery, Required] string[] layerIDs, [FromQuery, Required] string[] withTraits, [FromQuery, Required] string[] withoutTraits, [FromQuery] DateTimeOffset? atTime = null)
        {
            var timeThreshold = (atTime.HasValue) ? TimeThreshold.BuildAtTime(atTime.Value) : TimeThreshold.BuildLatest();
            var layerset = new LayerSet(layerIDs);
            var trans = modelContextBuilder.BuildImmediate();
            var user = await currentUserService.GetCurrentUser(trans);

            if (!layerBasedAuthorizationService.CanUserReadFromAllLayers(user, layerIDs))
                return Forbid($"User \"{user.Username}\" does not have permission to read from at least one of the following layerIDs: {string.Join(',', layerIDs)}");
            //TODO: ci-based authz

            var cis = await ciSearchModel.SearchForMergedCIsByTraits(new AllCIIDsSelection(), AllAttributeSelection.Instance, withTraits, withoutTraits, layerset, trans, timeThreshold);

            return Ok(cis.Select(ci => CIDTO.BuildFromMergedCI(ci)));
        }
    }
}
