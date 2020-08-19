using Landscape.Base.Entity;
using Landscape.Base.Entity.DTO;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using LandscapeRegistry.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using static Landscape.Base.Model.IBaseAttributeModel;

namespace LandscapeRegistry.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class AttributeController : ControllerBase
    {
        private readonly IAttributeModel attributeModel;
        private readonly IChangesetModel changesetModel;
        private readonly ICurrentUserService currentUserService;
        private readonly IRegistryAuthorizationService authorizationService;
        private readonly NpgsqlConnection conn;

        public AttributeController(IAttributeModel attributeModel, IChangesetModel changesetModel, ICurrentUserService currentUserService, IRegistryAuthorizationService authorizationService, NpgsqlConnection conn)
        {
            this.conn = conn;
            this.changesetModel = changesetModel;
            this.attributeModel = attributeModel;
            this.authorizationService = authorizationService;
            this.currentUserService = currentUserService;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="layerIDs"></param>
        /// <param name="atTime"></param>
        /// <returns></returns>
        [HttpGet("getMergedAttributesWithName")]
        public async Task<ActionResult<IEnumerable<CIAttributeDTO>>> GetMergedAttributesWithName([FromQuery, Required]string name, [FromQuery, Required]long[] layerIDs, [FromQuery]DateTimeOffset? atTime = null)
        {
            var timeThreshold = (atTime.HasValue) ? TimeThreshold.BuildAtTime(atTime.Value) : TimeThreshold.BuildLatest();
            var layerset = new LayerSet(layerIDs);
            var attributes = await attributeModel.FindMergedAttributesByFullName(name, new AllCIIDsSelection(), layerset, null, timeThreshold);
            return Ok(attributes.Select(a => CIAttributeDTO.Build(a.Value)));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ciids"></param>
        /// <param name="layerIDs"></param>
        /// <param name="atTime"></param>
        /// <returns></returns>
        [HttpGet("getMergedAttributes")]
        public async Task<ActionResult<IEnumerable<CIAttributeDTO>>> GetMergedAttributes([FromQuery, Required]IEnumerable<Guid> ciids, [FromQuery, Required]long[] layerIDs, [FromQuery]DateTimeOffset? atTime = null)
        {
            if (ciids.IsEmpty())
                return BadRequest("Empty CIID list");

            var timeThreshold = (atTime.HasValue) ? TimeThreshold.BuildAtTime(atTime.Value) : TimeThreshold.BuildLatest();
            var layerset = new LayerSet(layerIDs);
            var attributes = await attributeModel.GetMergedAttributes(MultiCIIDsSelection.Build(ciids), layerset, null, timeThreshold);
            return Ok(attributes.SelectMany(t => t.Value.Select(a => CIAttributeDTO.Build(a.Value))));
        }

        /// <summary>
        /// bulk replace all attributes in specified layer
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost("bulkReplaceAttributesInLayer")]
        public async Task<ActionResult> BulkReplaceAttributesInLayer([FromBody, Required]BulkCIAttributeLayerScopeDTO dto)
        {
            var user = await currentUserService.GetCurrentUser(null);
            if (!authorizationService.CanUserWriteToLayer(user, dto.LayerID))
                return Forbid($"User \"{user.Username}\" does not have permission to write to layer ID {dto.LayerID}");

            using var trans = conn.BeginTransaction();
            var changesetProxy = ChangesetProxy.Build(user.InDatabase, DateTimeOffset.Now, changesetModel);
            var data = BulkCIAttributeDataLayerScope.BuildFromDTO(dto);
            var success = await attributeModel.BulkReplaceAttributes(data, changesetProxy, trans);
            if (success)
            {
                trans.Commit();
                return Ok();
            }
            else return BadRequest();
        }


    }
}
