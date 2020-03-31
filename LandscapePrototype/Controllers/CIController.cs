using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using LandscapePrototype.Entity;
using LandscapePrototype.Entity.AttributeValues;
using LandscapePrototype.Model;
using LandscapePrototype.Model.Cached;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace LandscapePrototype.Controllers
{
    [ApiController]
    [Route("[controller]")]
    //[Authorize]
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
        /// <param name="CITypeID"></param>
        /// <param name="atTime">Specify datetime, for which point in time to get the data; leave empty to use current time (https://www.newtonsoft.com/json/help/html/DatesInJSON.htm)</param>
        /// <returns></returns>
        [HttpGet("getMergedCIsWithType")]
        public async Task<ActionResult<IEnumerable<MergedCI>>> GetMergedCIsWithType([FromQuery,Required]long[] layerIDs, [FromQuery,Required]string CITypeID, [FromQuery]DateTimeOffset? atTime = null)
        {
            var layerset = new LayerSet(layerIDs);
            return Ok(await ciModel.GetMergedCIsByType(layerset, null, atTime ?? DateTimeOffset.Now, CITypeID));
        }

        /// <summary>
        /// list of merged CIs with a specific CI-type, in a simplified form
        /// </summary>
        /// <param name="layerIDs">Specifies which layers contribute to the result, and in which order</param>
        /// <param name="CITypeID"></param>
        /// <param name="atTime">Specify datetime, for which point in time to get the data; leave empty to use current time (https://www.newtonsoft.com/json/help/html/DatesInJSON.htm)</param>
        /// <returns></returns>
        [HttpGet("getSimplifiedCIsByType")]
        public async Task<ActionResult<IEnumerable<SimplifiedCI>>> GetSimplifiedCIsByType([FromQuery, Required]long[] layerIDs, [FromQuery, Required]string CITypeID, [FromQuery]DateTimeOffset? atTime = null)
        {
            var layerset = new LayerSet(layerIDs);
            var cis = await ciModel.GetMergedCIsByType(layerset, null, atTime ?? DateTimeOffset.Now, CITypeID);
            return Ok(cis.Select(ci => SimplifiedCI.Build(ci)));
        }
    }
}
