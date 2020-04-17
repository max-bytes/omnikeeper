using Landscape.Base.Entity.DTO;
using LandscapeRegistry.Model.Cached;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        public async Task<ActionResult<IEnumerable<LayerDTO>>> GetAllLayers()
        {
            return Ok((await layerModel.GetLayers(null)).Select(l => LayerDTO.Build(l)));
        }
        /// <summary>
        /// get a layer by name
        /// </summary>
        /// <returns></returns>
        [HttpGet("getLayerByName")]
        public async Task<ActionResult<LayerDTO>> GetLayerByName([FromQuery, Required]string layerName)
        {
            return Ok(LayerDTO.Build(await layerModel.GetLayer(layerName, null)));
        }

        /// <summary>
        /// get layers by name
        /// </summary>
        /// <returns></returns>
        [HttpGet("getLayersByName")]
        public async Task<ActionResult<IEnumerable<LayerDTO>>> GetLayersByName([FromQuery, Required]string[] layerNames)
        {
            var ret = new List<LayerDTO>();
            // TODO: better performance: use GetLayers()
            foreach (var layerName in layerNames)
                ret.Add(LayerDTO.Build(await layerModel.GetLayer(layerName, null)));
            return Ok(ret);
        }
    }
}
