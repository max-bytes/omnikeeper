using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DTO;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
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
    public class TraitController : ControllerBase
    {
        private readonly IEffectiveTraitModel traitModel;
        private readonly ICIBasedAuthorizationService ciBasedAuthorizationService;

        public TraitController(IEffectiveTraitModel traitModel, ICIBasedAuthorizationService ciBasedAuthorizationService)
        {
            this.traitModel = traitModel;
            this.ciBasedAuthorizationService = ciBasedAuthorizationService;
        }

        [HttpGet("getEffectiveTraitsForTraitName")]
        public async Task<ActionResult<IDictionary<Guid, EffectiveTraitDTO>>> GetEffectiveTraitsForTraitName([FromQuery, Required] long[] layerIDs, [FromQuery, Required] string traitName, [FromQuery] DateTimeOffset? atTime = null)
        {
            var timeThreshold = (atTime.HasValue) ? TimeThreshold.BuildAtTime(atTime.Value) : TimeThreshold.BuildLatest();
            var layerset = new LayerSet(layerIDs);
            var traitSets = await traitModel.CalculateEffectiveTraitsForTraitName(traitName, layerset, null, (atTime.HasValue) ? TimeThreshold.BuildAtTime(atTime.Value) : TimeThreshold.BuildLatest());
            return Ok(traitSets
                .Where(kv => ciBasedAuthorizationService.CanReadCI(kv.Key))
                .ToDictionary(kv => kv.Key, kv => EffectiveTraitDTO.Build(kv.Value.et)));
        }
    }
}
