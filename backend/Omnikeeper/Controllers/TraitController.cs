﻿using Microsoft.AspNetCore.Authorization;
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
    [Obsolete]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class TraitController : ControllerBase
    {
        private readonly IEffectiveTraitModel traitModel;
        private readonly ITraitsProvider traitsProvider;
        private readonly ICurrentUserAccessor currentUserService;
        private readonly ICIBasedAuthorizationService ciBasedAuthorizationService;
        private readonly ICIModel ciModel;
        private readonly IModelContextBuilder modelContextBuilder;
        private readonly ILayerBasedAuthorizationService layerBasedAuthorizationService;

        public TraitController(IEffectiveTraitModel traitModel, ITraitsProvider traitsProvider, ICIBasedAuthorizationService ciBasedAuthorizationService, ICIModel ciModel,
            IModelContextBuilder modelContextBuilder, ILayerBasedAuthorizationService layerBasedAuthorizationService, ICurrentUserAccessor currentUserService)
        {
            this.traitModel = traitModel;
            this.traitsProvider = traitsProvider;
            this.ciBasedAuthorizationService = ciBasedAuthorizationService;
            this.ciModel = ciModel;
            this.modelContextBuilder = modelContextBuilder;
            this.layerBasedAuthorizationService = layerBasedAuthorizationService;
            this.currentUserService = currentUserService;
        }

        [HttpGet("getEffectiveTraitsForTraitName")]
        public async Task<ActionResult<IDictionary<Guid, EffectiveTraitDTO>>> GetEffectiveTraitsForTraitName([FromQuery, Required] string[] layerIDs, [FromQuery, Required] string traitName, [FromQuery] DateTimeOffset? atTime = null)
        {
            var timeThreshold = (atTime.HasValue) ? TimeThreshold.BuildAtTime(atTime.Value) : TimeThreshold.BuildLatest();
            var layerset = new LayerSet(layerIDs);
            var trans = modelContextBuilder.BuildImmediate();
            var user = await currentUserService.GetCurrentUser(trans);

            if (!layerBasedAuthorizationService.CanUserReadFromAllLayers(user, layerIDs))
                return Forbid($"User \"{user.Username}\" does not have permission to read from at least one of the following layerIDs: {string.Join(',', layerIDs)}");

            var trait = await traitsProvider.GetActiveTrait(traitName, trans, timeThreshold);
            if (trait == null)
                return BadRequest($"Trait with name \"{traitName}\" not found");
            // TODO: reduce attribute selection
            var cis = await ciModel.GetMergedCIs(new AllCIIDsSelection(), layerset, includeEmptyCIs: trait is TraitEmpty, AllAttributeSelection.Instance, trans, timeThreshold);
            var traitSets = await traitModel.GetEffectiveTraitsForTrait(trait, cis, layerset, trans, timeThreshold);
            return Ok(traitSets
                .Where(kv => ciBasedAuthorizationService.CanReadCI(kv.Key)) // TODO: refactor to use a method that queries all ciids at once, returning those that are readable
                .ToDictionary(kv => kv.Key, kv => EffectiveTraitDTO.Build(kv.Value)));
        }
    }
}
