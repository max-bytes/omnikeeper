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
        public async Task<ActionResult<Layer>> GetLayerByName([FromQuery,Required]string layerName)
        {
            return Ok(await layerModel.GetLayer(layerName, null));
        }
    }
}
