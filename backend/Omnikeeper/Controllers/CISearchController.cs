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
        private readonly ITraitsProvider traitsProvider;
        private readonly ICIBasedAuthorizationService ciBasedAuthorizationService;
        private readonly IModelContextBuilder modelContextBuilder;

        public CISearchController(ICISearchModel ciSearchModel, ITraitsProvider traitsProvider, ICIBasedAuthorizationService ciBasedAuthorizationService,
            IModelContextBuilder modelContextBuilder)
        {
            this.ciSearchModel = ciSearchModel;
            this.traitsProvider = traitsProvider;
            this.ciBasedAuthorizationService = ciBasedAuthorizationService;
            this.modelContextBuilder = modelContextBuilder;
        }

        [HttpGet("searchCIsByTraits")]
        public async Task<ActionResult<IEnumerable<CIDTO>>> SearchCIsByTraits([FromQuery, Required] long[] layerIDs, [FromQuery, Required] string[] withTraits, [FromQuery, Required] string[] withoutTraits, [FromQuery] DateTimeOffset? atTime = null)
        {
            var timeThreshold = (atTime.HasValue) ? TimeThreshold.BuildAtTime(atTime.Value) : TimeThreshold.BuildLatest();
            var layerset = new LayerSet(layerIDs);
            var trans = modelContextBuilder.BuildImmediate();

            var cis = await ciSearchModel.SearchForMergedCIsByTraits(new AllCIIDsSelection(), withTraits, withoutTraits, layerset, trans, timeThreshold);

            return Ok(cis.Select(ci => CIDTO.BuildFromMergedCI(ci)));
        }
    }
}
