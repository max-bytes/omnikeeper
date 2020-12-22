using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Omnikeeper.Base.Entity.DTO;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils.ModelContext;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class LayerController : ControllerBase
    {
        private readonly ILayerModel layerModel;
        private readonly IModelContextBuilder modelContextBuilder;

        public LayerController(ILayerModel layerModel, IModelContextBuilder modelContextBuilder)
        {
            this.layerModel = layerModel;
            this.modelContextBuilder = modelContextBuilder;
        }

        /// <summary>
        /// list of all layers
        /// </summary>
        /// <returns></returns>
        [HttpGet("getAllLayers")]
        public async Task<ActionResult<IEnumerable<LayerDTO>>> GetAllLayers()
        {
            var trans = modelContextBuilder.BuildImmediate();
            return Ok((await layerModel.GetLayers(trans)).Select(l => LayerDTO.Build(l)));
        }
        /// <summary>
        /// get a layer by name
        /// </summary>
        /// <returns></returns>
        [HttpGet("getLayerByName")]
        public async Task<ActionResult<LayerDTO>> GetLayerByName([FromQuery, Required] string layerName)
        {
            var trans = modelContextBuilder.BuildImmediate();
            var layer = await layerModel.GetLayer(layerName, trans);
            if (layer == null)
                return NotFound($"Could not find layer with name {layerName}");
            return Ok(LayerDTO.Build(layer));
        }

        /// <summary>
        /// get layers by name
        /// </summary>
        /// <returns></returns>
        [HttpGet("getLayersByName")]
        public async Task<ActionResult<IEnumerable<LayerDTO>>> GetLayersByName([FromQuery, Required] string[] layerNames)
        {
            var trans = modelContextBuilder.BuildImmediate();
            var ret = new List<LayerDTO>();
            // TODO: better performance: use GetLayers()
            foreach (var layerName in layerNames)
            {
                var layer = await layerModel.GetLayer(layerName, trans);
                if (layer == null)
                    return NotFound($"Could not find layer with name {layerName}");
                ret.Add(LayerDTO.Build(layer));
            }
            return Ok(ret);
        }
    }
}
