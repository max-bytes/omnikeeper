using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
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
    public class CytoscapeController : ControllerBase
    {
        private readonly IModelContextBuilder modelContextBuilder;
        private readonly ITraitsProvider traitsProvider;
        private readonly ICurrentUserAccessor currentUserAccessor;
        private readonly IAuthzFilterManager authzFilterManager;
        private readonly TraitCentricDataGenerator traitCentricDataGenerator;

        public CytoscapeController(IModelContextBuilder modelContextBuilder, ITraitsProvider traitsProvider, ICurrentUserAccessor currentUserAccessor,
            IAuthzFilterManager authzFilterManager, TraitCentricDataGenerator traitCentricDataGenerator)
        {
            this.modelContextBuilder = modelContextBuilder;
            this.traitsProvider = traitsProvider;
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

            var ret = await traitCentricDataGenerator.GenerateCytoscape(layerSet, traits, trans, timeThreshold);

            return Ok(ret.RootElement);
        }
    }
}
