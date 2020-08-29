using Landscape.Base.Entity;
using Landscape.Base.Entity.DTO;
using Landscape.Base.Model;
using Landscape.Base.Utils;
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
    public class CIController : ControllerBase
    {
        private readonly ICIModel ciModel;

        public CIController(ICIModel ciModel)
        {
            this.ciModel = ciModel;
        }

        /// <summary>
        /// list of all CI-IDs
        /// </summary>
        /// <returns></returns>
        [HttpGet("getAllCIIDs")]
        public async Task<ActionResult<IEnumerable<string>>> GetAllCIIDs()
        {
            return Ok(await ciModel.GetCIIDs(null));
        }

        /// <summary>
        /// single CI by CI-ID
        /// </summary>
        /// <param name="layerIDs">Specifies which layers contribute to the result, and in which order</param>
        /// <param name="CIID"></param>
        /// <param name="atTime">Specify datetime, for which point in time to get the data; leave empty to use current time (https://www.newtonsoft.com/json/help/html/DatesInJSON.htm)</param>
        /// <returns></returns>
        [HttpGet("getCIByID")]
        public async Task<ActionResult<CIDTO>> GetCIByID([FromQuery, Required]long[] layerIDs, [FromQuery, Required]Guid CIID, [FromQuery]DateTimeOffset? atTime = null)
        {
            var layerset = new LayerSet(layerIDs);
            var ci = await ciModel.GetMergedCI(CIID, layerset, null, (atTime.HasValue) ? TimeThreshold.BuildAtTime(atTime.Value) : TimeThreshold.BuildLatest());
            if (ci == null) return NotFound();
            return Ok(CIDTO.Build(ci));
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
        public async Task<ActionResult<IEnumerable<CIDTO>>> GetCIsByID([FromQuery, Required]long[] layerIDs, [FromQuery, Required]Guid[] CIIDs, [FromQuery]DateTimeOffset? atTime = null)
        {
            if (CIIDs.IsEmpty())
                return BadRequest("Empty CIID list");

            var layerset = new LayerSet(layerIDs);
            var cis = await ciModel.GetMergedCIs(SpecificCIIDsSelection.Build(CIIDs), layerset, true, null, (atTime.HasValue) ? TimeThreshold.BuildAtTime(atTime.Value) : TimeThreshold.BuildLatest());
            return Ok(cis.Select(ci => CIDTO.Build(ci)));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="layerIDs"></param>
        /// <param name="atTime"></param>
        /// <returns></returns>
        [HttpGet("getCIIDsOfNonEmptyCIs")]
        public async Task<ActionResult<IEnumerable<Guid>>> GetCIIDsOfNonEmptyCIs([FromQuery, Required]long[] layerIDs, [FromQuery]DateTimeOffset? atTime = null)
        {
            var layerset = new LayerSet(layerIDs);
            var ciids = await ciModel.GetCIIDsOfNonEmptyCIs(layerset, null, (atTime.HasValue) ? TimeThreshold.BuildAtTime(atTime.Value) : TimeThreshold.BuildLatest());
            return Ok(ciids);
        }

        
    }
}
