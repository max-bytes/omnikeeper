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
    [Obsolete]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class CIController : ControllerBase
    {
        private readonly ICIModel ciModel;
        private readonly ICurrentUserAccessor currentUserService;
        private readonly ICIBasedAuthorizationService ciBasedAuthorizationService;
        private readonly IModelContextBuilder modelContextBuilder;
        private readonly ILayerBasedAuthorizationService layerBasedAuthorizationService;

        public CIController(ICIModel ciModel, ICIBasedAuthorizationService ciBasedAuthorizationService, ICurrentUserAccessor currentUserService, IModelContextBuilder modelContextBuilder, ILayerBasedAuthorizationService layerBasedAuthorizationService)
        {
            this.ciModel = ciModel;
            this.ciBasedAuthorizationService = ciBasedAuthorizationService;
            this.currentUserService = currentUserService;
            this.modelContextBuilder = modelContextBuilder;
            this.layerBasedAuthorizationService = layerBasedAuthorizationService;
        }

        /// <summary>
        /// list of all CI-IDs
        /// </summary>
        /// <returns></returns>
        [HttpGet("getAllCIIDs")]
        public async Task<ActionResult<IEnumerable<Guid>>> GetAllCIIDs()
        {
            var trans = modelContextBuilder.BuildImmediate();
            var ciids = await ciModel.GetCIIDs(trans);
            ciids = ciids.Where(ciid => ciBasedAuthorizationService.CanReadCI(ciid)); // TODO: refactor to use a method that queries all ciids at once, returning those that are readable
            return Ok(ciids);
        }

        /// <summary>
        /// single CI by CI-ID
        /// </summary>
        /// <param name="layerIDs">Specifies which layers contribute to the result, and in which order</param>
        /// <param name="CIID"></param>
        /// <param name="atTime">Specify datetime, for which point in time to get the data; leave empty to use current time (https://www.newtonsoft.com/json/help/html/DatesInJSON.htm)</param>
        /// <returns></returns>
        [HttpGet("getCIByID")]
        public async Task<ActionResult<CIDTO>> GetCIByID([FromQuery, Required] string[] layerIDs, [FromQuery, Required] Guid CIID, [FromQuery] DateTimeOffset? atTime = null)
        {
            var trans = modelContextBuilder.BuildImmediate();
            var user = await currentUserService.GetCurrentUser(trans);
            if (!layerBasedAuthorizationService.CanUserReadFromAllLayers(user, layerIDs))
                return Forbid($"User \"{user.Username}\" does not have permission to read from at least one of the following layerIDs: {string.Join(',', layerIDs)}");
            if (!ciBasedAuthorizationService.CanReadCI(CIID))
            {
                return Forbid($"User \"{user.Username}\" does not have permission to write to CI {CIID}");
            }

            var layerset = new LayerSet(layerIDs);
            var ci = await ciModel.GetMergedCI(CIID, layerset, AllAttributeSelection.Instance, trans, (atTime.HasValue) ? TimeThreshold.BuildAtTime(atTime.Value) : TimeThreshold.BuildLatest());
            if (ci == null) return NotFound();
            return Ok(CIDTO.BuildFromMergedCI(ci));
        }

        /// <summary>
        /// multiple CIs by CI-ID
        /// !Watch out for the query URL getting too long because of a lot of CIIDs!
        /// TODO: consider using POST
        /// </summary>
        /// <param name="layerIDs">Specifies which layers contribute to the result, and in which order</param>
        /// <param name="CIIDs"></param>
        /// <param name="atTime">Specify datetime, for which point in time to get the data; leave empty to use current time (https://www.newtonsoft.com/json/help/html/DatesInJSON.htm)</param>
        /// <returns></returns>
        [HttpGet("getCIsByID")]
        public async Task<ActionResult<IEnumerable<CIDTO>>> GetCIsByID([FromQuery, Required] string[] layerIDs, [FromQuery, Required] Guid[] CIIDs, [FromQuery] DateTimeOffset? atTime = null)
        {
            if (CIIDs.IsEmpty())
                return BadRequest("Empty CIID list");
            var trans = modelContextBuilder.BuildImmediate();
            var user = await currentUserService.GetCurrentUser(trans);
            if (!layerBasedAuthorizationService.CanUserReadFromAllLayers(user, layerIDs))
                return Forbid($"User \"{user.Username}\" does not have permission to read from at least one of the following layerIDs: {string.Join(',', layerIDs)}");
            if (!ciBasedAuthorizationService.CanReadAllCIs(CIIDs, out var notAllowedCI))
            {
                return Forbid($"User \"{user.Username}\" does not have permission to read from CI {notAllowedCI}");
            }

            var layerset = new LayerSet(layerIDs);
            var cis = await ciModel.GetMergedCIs(SpecificCIIDsSelection.Build(CIIDs), layerset, true, AllAttributeSelection.Instance, trans, (atTime.HasValue) ? TimeThreshold.BuildAtTime(atTime.Value) : TimeThreshold.BuildLatest());
            return Ok(cis.Select(ci => CIDTO.BuildFromMergedCI(ci)));
        }
    }
}
