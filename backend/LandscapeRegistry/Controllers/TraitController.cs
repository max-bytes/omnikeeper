using Landscape.Base.Entity;
using Landscape.Base.Entity.DTO;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using LandscapeRegistry.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapeRegistry.Controllers
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
        public async Task<ActionResult<IEnumerable<EffectiveTraitSetDTO>>> GetEffectiveTraitSetsForTraitName([FromQuery, Required]long[] layerIDs, [FromQuery, Required]string traitName, [FromQuery]DateTimeOffset? atTime = null)
        {
            var layerset = new LayerSet(layerIDs);
            var traitSets = await traitModel.CalculateEffectiveTraitSetsForTraitName(traitName, layerset, null, (atTime.HasValue) ? TimeThreshold.BuildAtTime(atTime.Value) : TimeThreshold.BuildLatest());
            return Ok(traitSets.Select(traitSet => EffectiveTraitSetDTO.Build(traitSet)));
        }
    }
}
