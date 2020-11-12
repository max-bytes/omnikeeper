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
        private readonly ICIBasedAuthorizationService ciBasedAuthorizationService;
        private readonly IModelContextBuilder modelContextBuilder;

        public TraitController(IEffectiveTraitModel traitModel, ICIBasedAuthorizationService ciBasedAuthorizationService, 
            IModelContextBuilder modelContextBuilder)
        {
            this.traitModel = traitModel;
            this.ciBasedAuthorizationService = ciBasedAuthorizationService;
            this.modelContextBuilder = modelContextBuilder;
        }

        [HttpGet("getEffectiveTraitSetsForTraitName")]
        public async Task<ActionResult<IDictionary<Guid, EffectiveTraitDTO>>> GetEffectiveTraitsForTraitName([FromQuery, Required] long[] layerIDs, [FromQuery, Required] string traitName, [FromQuery] DateTimeOffset? atTime = null)
        {
            var layerset = new LayerSet(layerIDs);
            var trans = modelContextBuilder.BuildImmediate();
            var traitSets = await traitModel.CalculateEffectiveTraitsForTraitName(traitName, layerset, trans, (atTime.HasValue) ? TimeThreshold.BuildAtTime(atTime.Value) : TimeThreshold.BuildLatest());
            return Ok(traitSets
                .Where(kv => ciBasedAuthorizationService.CanReadCI(kv.Key))
                .ToDictionary(kv => kv.Key, kv => EffectiveTraitDTO.Build(kv.Value)));
        }
    }
}
