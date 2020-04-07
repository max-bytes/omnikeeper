using Landscape.Base.Entity;
using LandscapeRegistry.Model.Cached;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace LandscapeRegistry.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class LayerController : ControllerBase
    {
        private readonly CachedLayerModel layerModel;

        public LayerController(CachedLayerModel layerModel)
        {
            this.layerModel = layerModel;
        }

        /// <summary>
        /// list of all layers
        /// </summary>
        /// <returns></returns>
        [HttpGet("getAllLayers")]
        public async Task<ActionResult<IEnumerable<Layer>>> GetAllLayers()
        {
            return Ok(await layerModel.GetLayers(null));
        }
        /// <summary>
        /// get a layer by name
        /// </summary>
        /// <returns></returns>
        [HttpGet("getLayerByName")]
        public async Task<ActionResult<Layer>> GetLayerByName([FromQuery, Required]string layerName)
        {
            return Ok(await layerModel.GetLayer(layerName, null));
        }

        /// <summary>
        /// get layers by name
        /// </summary>
        /// <returns></returns>
        [HttpGet("getLayersByName")]
        public async Task<ActionResult<IEnumerable<Layer>>> GetLayersByName([FromQuery, Required]string[] layerNames)
        {
            var ret = new List<Layer>();
            // TODO: better performance: use GetLayers()
            foreach (var layerName in layerNames)
                ret.Add(await layerModel.GetLayer(layerName, null));
            return Ok(ret);
        }
    }
}
