using Landscape.Base.Entity;
using Landscape.Base.Entity.DTO;
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
    public class CIController : ControllerBase
    {
        private readonly CIModel ciModel;

        public CIController(CIModel ciModel)
        {
            this.ciModel = ciModel;
        }

        /// <summary>
        /// list of all CI-types
        /// </summary>
        /// <returns></returns>
        [HttpGet("getAllCITypes")]
        public async Task<ActionResult<IEnumerable<CITypeDTO>>> GetAllCITypes()
        {
            return Ok((await ciModel.GetCITypes(null, null)).Select(t => CITypeDTO.Build(t)));
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

        ///// <summary>
        ///// list of merged CIs with a specific CI-type
        ///// </summary>
        ///// <param name="layerIDs">Specifies which layers contribute to the result, and in which order</param>
        ///// <param name="CITypeIDs"></param>
        ///// <param name="atTime">Specify datetime, for which point in time to get the data; leave empty to use current time (https://www.newtonsoft.com/json/help/html/DatesInJSON.htm)</param>
        ///// <returns></returns>
        //[HttpGet("getMergedCIsByType")]
        //public async Task<ActionResult<IEnumerable<MergedCI>>> GetMergedCIsByType([FromQuery,Required]long[] layerIDs, [FromQuery,Required]string[] CITypeIDs, [FromQuery]DateTimeOffset? atTime = null)
        //{
        //    var layerset = new LayerSet(layerIDs);
        //    return Ok(await ciModel.GetMergedCIsByType(layerset, null, atTime ?? DateTimeOffset.Now, CITypeIDs));
        //}

        /// <summary>
        /// list of merged CIs with speficied CI-types
        /// </summary>
        /// <param name="layerIDs">Specifies which layers contribute to the result, and in which order</param>
        /// <param name="CITypeIDs"></param>
        /// <param name="atTime">Specify datetime, for which point in time to get the data; leave empty to use current time (https://www.newtonsoft.com/json/help/html/DatesInJSON.htm)</param>
        /// <returns></returns>
        [HttpGet("getCIsByType")]
        public async Task<ActionResult<IEnumerable<CIDTO>>> GetCIsByType([FromQuery, Required]long[] layerIDs, [FromQuery, Required]string[] CITypeIDs, [FromQuery]DateTimeOffset? atTime = null)
        {
            var layerset = new LayerSet(layerIDs);
            var cis = await ciModel.GetMergedCIsByType(layerset, null, atTime ?? DateTimeOffset.Now, CITypeIDs);
            return Ok(cis.Select(ci => CIDTO.Build(ci)));
        }

        /// <summary>
        /// single CI by CI-ID
        /// </summary>
        /// <param name="layerIDs">Specifies which layers contribute to the result, and in which order</param>
        /// <param name="CIID"></param>
        /// <param name="atTime">Specify datetime, for which point in time to get the data; leave empty to use current time (https://www.newtonsoft.com/json/help/html/DatesInJSON.htm)</param>
        /// <returns></returns>
        [HttpGet("getCIByID")]
        public async Task<ActionResult<CIDTO>> GetCIByID([FromQuery, Required]long[] layerIDs, [FromQuery, Required]string CIID, [FromQuery]DateTimeOffset? atTime = null)
        {
            var layerset = new LayerSet(layerIDs);
            var ci = await ciModel.GetMergedCI(CIID, layerset, null, atTime ?? DateTimeOffset.Now);
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
        public async Task<ActionResult<IEnumerable<CIDTO>>> GetCIsByID([FromQuery, Required]long[] layerIDs, [FromQuery, Required]string[] CIIDs, [FromQuery]DateTimeOffset? atTime = null)
        {
            var layerset = new LayerSet(layerIDs);
            var cis = await ciModel.GetMergedCIs(layerset, true, null, atTime ?? DateTimeOffset.Now, CIIDs);
            return Ok(cis.Select(ci => CIDTO.Build(ci)));
        }
    }
}
