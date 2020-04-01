using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using LandscapeRegistry.Entity;
using LandscapeRegistry.Entity.AttributeValues;
using LandscapeRegistry.Model;
using LandscapeRegistry.Model.Cached;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace LandscapeRegistry.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class CIController : ControllerBase
    {
        private readonly CIModel ciModel;
        private readonly CachedLayerModel layerModel;

        public CIController(CIModel ciModel, CachedLayerModel layerModel)
        {
            this.ciModel = ciModel;
            this.layerModel = layerModel;
        }

        /// <summary>
        /// list of all CI-types
        /// </summary>
        /// <returns></returns>
        [HttpGet("getAllCITypes")]
        public async Task<ActionResult<IEnumerable<CIType>>> GetAllCITypes()
        {
            return Ok(await ciModel.GetCITypes(null));
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
        /// list of merged CIs with a specific CI-type
        /// </summary>
        /// <param name="layerIDs">Specifies which layers contribute to the result, and in which order</param>
        /// <param name="CITypeIDs"></param>
        /// <param name="atTime">Specify datetime, for which point in time to get the data; leave empty to use current time (https://www.newtonsoft.com/json/help/html/DatesInJSON.htm)</param>
        /// <returns></returns>
        [HttpGet("getMergedCIsByType")]
        public async Task<ActionResult<IEnumerable<MergedCI>>> GetMergedCIsByType([FromQuery,Required]long[] layerIDs, [FromQuery,Required]string[] CITypeIDs, [FromQuery]DateTimeOffset? atTime = null)
        {
            var layerset = new LayerSet(layerIDs);
            return Ok(await ciModel.GetMergedCIsByType(layerset, null, atTime ?? DateTimeOffset.Now, CITypeIDs));
        }

        /// <summary>
        /// list of merged CIs with speficied CI-types, in a simplified form
        /// </summary>
        /// <param name="layerIDs">Specifies which layers contribute to the result, and in which order</param>
        /// <param name="CITypeIDs"></param>
        /// <param name="atTime">Specify datetime, for which point in time to get the data; leave empty to use current time (https://www.newtonsoft.com/json/help/html/DatesInJSON.htm)</param>
        /// <returns></returns>
        [HttpGet("getSimplifiedCIsByType")]
        public async Task<ActionResult<IEnumerable<SimplifiedCI>>> GetSimplifiedCIsByType([FromQuery, Required]long[] layerIDs, [FromQuery, Required]string[] CITypeIDs, [FromQuery]DateTimeOffset? atTime = null)
        {
            var layerset = new LayerSet(layerIDs);
            var cis = await ciModel.GetMergedCIsByType(layerset, null, atTime ?? DateTimeOffset.Now, CITypeIDs);
            return Ok(cis.Select(ci => SimplifiedCI.Build(ci)));
        }

        /// <summary>
        /// single CI by CI-ID, in a simplified form
        /// </summary>
        /// <param name="layerIDs">Specifies which layers contribute to the result, and in which order</param>
        /// <param name="CIID"></param>
        /// <param name="atTime">Specify datetime, for which point in time to get the data; leave empty to use current time (https://www.newtonsoft.com/json/help/html/DatesInJSON.htm)</param>
        /// <returns></returns>
        [HttpGet("getSimplifiedCIByID")]
        public async Task<ActionResult<SimplifiedCI>> GetSimplifiedCIByID([FromQuery, Required]long[] layerIDs, [FromQuery, Required]string CIID, [FromQuery]DateTimeOffset? atTime = null)
        {
            var layerset = new LayerSet(layerIDs);
            var ci = await ciModel.GetMergedCI(CIID, layerset, null, atTime ?? DateTimeOffset.Now);
            if (ci == null) return NotFound();
            return Ok(SimplifiedCI.Build(ci));
        }
    }
}
