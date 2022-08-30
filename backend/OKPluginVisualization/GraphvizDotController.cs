using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Omnikeeper.Base.Authz;

namespace OKPluginVisualization
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class GraphvizDotController : ControllerBase
    {
        private readonly IModelContextBuilder modelContextBuilder;
        private readonly ITraitsProvider traitsProvider;
        private readonly ICurrentUserAccessor currentUserAccessor;
        private readonly IAuthzFilterManager authzFilterManager;
        private readonly TraitCentricDataGenerator traitCentricDataGenerator;
        private readonly LayerCentricUsageGenerator layerCentricUsageGenerator;

        public GraphvizDotController(IModelContextBuilder modelContextBuilder, ITraitsProvider traitsProvider, ICurrentUserAccessor currentUserAccessor,
            IAuthzFilterManager authzFilterManager, TraitCentricDataGenerator traitCentricDataGenerator, LayerCentricUsageGenerator layerCentricUsageGenerator)
        {
            this.modelContextBuilder = modelContextBuilder;
            this.traitsProvider = traitsProvider;
            this.currentUserAccessor = currentUserAccessor;
            this.authzFilterManager = authzFilterManager;
            this.traitCentricDataGenerator = traitCentricDataGenerator;
            this.layerCentricUsageGenerator = layerCentricUsageGenerator;
        }

        [HttpGet("traitCentric")]
        public async Task<IActionResult> TraitCentric([FromQuery, Required] string[] layerIDs, [FromQuery] string[]? traitIDs, [FromQuery] string? traitIDsRegex)
        {
            if (layerIDs.IsEmpty())
                return BadRequest("No layer IDs specified");

            using var trans = modelContextBuilder.BuildImmediate();
            var timeThreshold = TimeThreshold.BuildLatest();

            var user = await currentUserAccessor.GetCurrentUser(trans);

            if (await authzFilterManager.ApplyPreFilterForQuery(QueryOperation.Query, user, layerIDs, trans) is AuthzFilterResultDeny d)
                return Forbid(d.Reason);

            var layerSet = new LayerSet(layerIDs);

            IEnumerable<ITrait> traits;
            if (traitIDsRegex != null && !traitIDsRegex.IsEmpty())
                traits = (await traitsProvider.GetActiveTraits(trans, timeThreshold)).Values.Where(t => Regex.Match(t.ID, traitIDsRegex).Success);
            else if (traitIDs != null && !traitIDs.IsEmpty())
                traits = (await traitsProvider.GetActiveTraitsByIDs(traitIDs, trans, timeThreshold)).Values;
            else
                return BadRequest("No trait IDs specified");

            var ret = await traitCentricDataGenerator.GenerateDot(layerSet, traits, trans, timeThreshold);

            return Content(ret);
        }


        [HttpGet("layerCentric")]
        public async Task<IActionResult> LayerCentric([FromQuery, Required] string[] layerIDs, [FromQuery, Required] DateTimeOffset from, [FromQuery, Required] DateTimeOffset to)
        {
            if (layerIDs.IsEmpty())
                return BadRequest("No layer IDs specified");

            using var trans = modelContextBuilder.BuildImmediate();

            var user = await currentUserAccessor.GetCurrentUser(trans);

            if (await authzFilterManager.ApplyPreFilterForQuery(QueryOperation.Query, user, layerIDs, trans) is AuthzFilterResultDeny d)
                return Forbid(d.Reason);

            var layerSet = new LayerSet(layerIDs);
            var timeThreshold = TimeThreshold.BuildLatest();

            var ret = await layerCentricUsageGenerator.Generate(layerSet, from, to, trans, timeThreshold);

            return Content(ret);
        }
    }
}
