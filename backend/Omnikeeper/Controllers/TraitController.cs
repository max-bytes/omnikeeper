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
    public class TraitController : ControllerBase
    {
        private readonly IEffectiveTraitModel traitModel;
        private readonly ITraitsProvider traitsProvider;
        private readonly ICIBasedAuthorizationService ciBasedAuthorizationService;
        private readonly IModelContextBuilder modelContextBuilder;

        public TraitController(IEffectiveTraitModel traitModel, ITraitsProvider traitsProvider, ICIBasedAuthorizationService ciBasedAuthorizationService,
            IModelContextBuilder modelContextBuilder)
        {
            this.traitModel = traitModel;
            this.traitsProvider = traitsProvider;
            this.ciBasedAuthorizationService = ciBasedAuthorizationService;
            this.modelContextBuilder = modelContextBuilder;
        }

        [HttpGet("getEffectiveTraitsForTraitName")]
        public async Task<ActionResult<IDictionary<Guid, EffectiveTraitDTO>>> GetEffectiveTraitsForTraitName([FromQuery, Required] long[] layerIDs, [FromQuery, Required] string traitName, [FromQuery] DateTimeOffset? atTime = null)
        {
            var timeThreshold = (atTime.HasValue) ? TimeThreshold.BuildAtTime(atTime.Value) : TimeThreshold.BuildLatest();
            var layerset = new LayerSet(layerIDs);
            var trans = modelContextBuilder.BuildImmediate();

            var trait = await traitsProvider.GetActiveTrait(traitName, trans, timeThreshold);
            if (trait == null)
                return BadRequest($"Trait with name \"{traitName}\" not found");
            var traitSets = await traitModel.CalculateEffectiveTraitsForTrait(trait, layerset, new AllCIIDsSelection(), trans, timeThreshold);
            return Ok(traitSets
                .Where(kv => ciBasedAuthorizationService.CanReadCI(kv.Key))
                .ToDictionary(kv => kv.Key, kv => EffectiveTraitDTO.Build(kv.Value.et)));
        }
    }
}
