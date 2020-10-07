using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DTO;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public TraitController(IEffectiveTraitModel traitModel)
        {
            this.traitModel = traitModel;
        }

        [HttpGet("getEffectiveTraitSetsForTraitName")]
        public async Task<ActionResult<IDictionary<Guid, EffectiveTraitDTO>>> GetEffectiveTraitsForTraitName([FromQuery, Required] long[] layerIDs, [FromQuery, Required] string traitName, [FromQuery] DateTimeOffset? atTime = null)
        {
            var layerset = new LayerSet(layerIDs);
            var traitSets = await traitModel.CalculateEffectiveTraitsForTraitName(traitName, layerset, null, (atTime.HasValue) ? TimeThreshold.BuildAtTime(atTime.Value) : TimeThreshold.BuildLatest());
            return Ok(traitSets.ToDictionary(kv => kv.Key, kv => EffectiveTraitDTO.Build(kv.Value)));
        }
    }
}
