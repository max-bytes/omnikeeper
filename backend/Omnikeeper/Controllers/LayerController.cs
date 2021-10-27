using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Omnikeeper.Base.Entity.DTO;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
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
    public class LayerController : ControllerBase
    {
        private readonly ILayerModel layerModel;
        private readonly IModelContextBuilder modelContextBuilder;
        private readonly ILayerBasedAuthorizationService layerBasedAuthorizationService;
        private readonly ICurrentUserService currentUserService;

        public LayerController(ILayerModel layerModel, IModelContextBuilder modelContextBuilder,
            ILayerBasedAuthorizationService layerBasedAuthorizationService, ICurrentUserService currentUserService)
        {
            this.layerModel = layerModel;
            this.modelContextBuilder = modelContextBuilder;
            this.layerBasedAuthorizationService = layerBasedAuthorizationService;
            this.currentUserService = currentUserService;
        }

        /// <summary>
        /// list of all layers
        /// </summary>
        /// <returns></returns>
        [HttpGet("getAllLayers")]
        public async Task<ActionResult<IEnumerable<LayerDTO>>> GetAllLayers()
        {
            var trans = modelContextBuilder.BuildImmediate();
            var user = await currentUserService.GetCurrentUser(trans);

            return Ok((await layerModel.GetLayers(trans))
                .Where(l => layerBasedAuthorizationService.CanUserReadFromLayer(user, l)) // authz filter
                .Select(l => LayerDTO.Build(l)));
        }
        /// <summary>
        /// get a layer by name
        /// </summary>
        /// <returns></returns>
        [HttpGet("getLayerByName")]
        public async Task<ActionResult<LayerDTO>> GetLayerByName([FromQuery, Required] string layerName)
        {
            var trans = modelContextBuilder.BuildImmediate();
            var user = await currentUserService.GetCurrentUser(trans);
            var layer = await layerModel.GetLayer(layerName, trans);

            if (layer == null)
                return NotFound($"Could not find layer with name {layerName}");
            if (!layerBasedAuthorizationService.CanUserReadFromLayer(user, layer))
                return Forbid($"User \"{user.Username}\" does not have permission to read from layer with ID {layer.ID}");
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
            var user = await currentUserService.GetCurrentUser(trans);
            var ret = new List<LayerDTO>();
            // TODO: better performance: use GetLayers()
            foreach (var layerName in layerNames)
            {
                var layer = await layerModel.GetLayer(layerName, trans);
                if (layer == null)
                    return NotFound($"Could not find layer with name {layerName}");
                if (!layerBasedAuthorizationService.CanUserReadFromLayer(user, layer))
                    return Forbid($"User \"{user.Username}\" does not have permission to read from layer with ID {layer.ID}");
                ret.Add(LayerDTO.Build(layer));
            }
            return Ok(ret);
        }
    }
}
