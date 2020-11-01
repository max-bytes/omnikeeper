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
        private readonly ITraitsProvider traitsProvider;

        public TraitController(IEffectiveTraitModel traitModel, ITraitsProvider traitsProvider)
        {
            this.traitModel = traitModel;
            this.traitsProvider = traitsProvider;
        }

        [HttpGet("getEffectiveTraitsForTraitName")]
        public async Task<ActionResult<IDictionary<Guid, EffectiveTraitDTO>>> GetEffectiveTraitsForTraitName([FromQuery, Required] long[] layerIDs, [FromQuery, Required] string traitName, [FromQuery] DateTimeOffset? atTime = null)
        {
            var timeThreshold = (atTime.HasValue) ? TimeThreshold.BuildAtTime(atTime.Value) : TimeThreshold.BuildLatest();
            var layerset = new LayerSet(layerIDs);
            var trait = await traitsProvider.GetActiveTrait(traitName, null, timeThreshold);
            if (trait == null)
                return BadRequest($"Trait with name \"{traitName}\" not found");
            var traitSets = await traitModel.CalculateEffectiveTraitsForTrait(trait, layerset, null, timeThreshold);
            return Ok(traitSets.ToDictionary(kv => kv.Key, kv => EffectiveTraitDTO.Build(kv.Value.et)));
        }
    }
}
