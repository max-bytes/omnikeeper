using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Omnikeeper.Base.Authz;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.GraphQL;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OKPluginVisualization
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class CytoscapeController : ControllerBase
    {
        private readonly IModelContextBuilder modelContextBuilder;
        private readonly ITraitsHolder traitsHolder;
        private readonly ICurrentUserAccessor currentUserAccessor;
        private readonly IAuthzFilterManager authzFilterManager;
        private readonly TraitCentricDataGenerator traitCentricDataGenerator;

        public CytoscapeController(IModelContextBuilder modelContextBuilder, ITraitsHolder traitsHolder, ICurrentUserAccessor currentUserAccessor,
            IAuthzFilterManager authzFilterManager, TraitCentricDataGenerator traitCentricDataGenerator)
        {
            this.modelContextBuilder = modelContextBuilder;
            this.traitsHolder = traitsHolder;
            this.currentUserAccessor = currentUserAccessor;
            this.authzFilterManager = authzFilterManager;
            this.traitCentricDataGenerator = traitCentricDataGenerator;
        }

        [HttpGet("traitCentric")]
        public async Task<IActionResult> TraitCentric([FromQuery, Required] string[] layerIDs, [FromQuery] string[]? traitIDs, [FromQuery] string? traitIDsRegex)
        {
            if (layerIDs.IsEmpty())
                return BadRequest("No layer IDs specified");

            using var trans = modelContextBuilder.BuildImmediate();
            var timeThreshold = TimeThreshold.BuildLatest();

            var user = await currentUserAccessor.GetCurrentUser(trans);

            var layerSet = new LayerSet(layerIDs);

            if (await authzFilterManager.ApplyFilterForQuery(new QueryOperationContext(), user, layerSet, trans, timeThreshold) is AuthzFilterResultDeny d)
                return Forbid(d.Reason);

            IEnumerable<ITrait> traits;
            if (traitIDsRegex != null && !traitIDsRegex.IsEmpty())
                traits = traitsHolder.GetTraits().Values.Where(t => Regex.Match(t.ID, traitIDsRegex).Success);
            else if (traitIDs != null && !traitIDs.IsEmpty())
                traits = traitsHolder.GetTraits(traitIDs).Values;
            else
                return BadRequest("No trait IDs specified");

            var ret = await traitCentricDataGenerator.GenerateCytoscape(layerSet, traits, trans, timeThreshold);

            return Ok(ret.RootElement);
        }
    }
}
