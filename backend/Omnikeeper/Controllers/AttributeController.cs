using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DTO;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Service;
using System;
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
    public class AttributeController : ControllerBase
    {
        private readonly IAttributeModel attributeModel;
        private readonly IChangesetModel changesetModel;
        private readonly ICurrentUserService currentUserService;
        private readonly ILayerBasedAuthorizationService layerBasedAuthorizationService;
        private readonly ICIBasedAuthorizationService ciBasedAuthorizationService;
        private readonly NpgsqlConnection conn;

        public AttributeController(IAttributeModel attributeModel, IChangesetModel changesetModel, ICurrentUserService currentUserService, ILayerBasedAuthorizationService authorizationService, NpgsqlConnection conn, ICIBasedAuthorizationService ciBasedAuthorizationService)
        {
            this.conn = conn;
            this.changesetModel = changesetModel;
            this.attributeModel = attributeModel;
            this.layerBasedAuthorizationService = authorizationService;
            this.currentUserService = currentUserService;
            this.ciBasedAuthorizationService = ciBasedAuthorizationService;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="layerIDs"></param>
        /// <param name="atTime"></param>
        /// <returns></returns>
        [HttpGet("getMergedAttributesWithName")]
        public async Task<ActionResult<IEnumerable<CIAttributeDTO>>> GetMergedAttributesWithName([FromQuery, Required] string name, [FromQuery, Required] long[] layerIDs, [FromQuery] DateTimeOffset? atTime = null)
        {
            var timeThreshold = (atTime.HasValue) ? TimeThreshold.BuildAtTime(atTime.Value) : TimeThreshold.BuildLatest();
            var layerset = new LayerSet(layerIDs);
            var attributesDict = await attributeModel.FindMergedAttributesByFullName(name, new AllCIIDsSelection(), layerset, null, timeThreshold);

            var attributes = attributesDict
                .Where(kv => ciBasedAuthorizationService.CanReadCI(kv.Key))
                .Select(kv => CIAttributeDTO.Build(kv.Value));

            return Ok(attributes);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ciids"></param>
        /// <param name="layerIDs"></param>
        /// <param name="atTime"></param>
        /// <returns></returns>
        [HttpGet("getMergedAttributes")]
        public async Task<ActionResult<IEnumerable<CIAttributeDTO>>> GetMergedAttributes([FromQuery, Required] IEnumerable<Guid> ciids, [FromQuery, Required] long[] layerIDs, [FromQuery] DateTimeOffset? atTime = null)
        {
            if (ciids.IsEmpty())
                return BadRequest("Empty CIID list");

            var user = await currentUserService.GetCurrentUser(null);
            if (!ciBasedAuthorizationService.CanReadAllCIs(ciids, out var notAllowedCI))
                return Forbid($"User \"{user.Username}\" does not have permission to read from CI {notAllowedCI}");

            var timeThreshold = (atTime.HasValue) ? TimeThreshold.BuildAtTime(atTime.Value) : TimeThreshold.BuildLatest();
            var layerset = new LayerSet(layerIDs);
            var attributes = await attributeModel.GetMergedAttributes(SpecificCIIDsSelection.Build(ciids), layerset, null, timeThreshold);
            return Ok(attributes.SelectMany(t => t.Value.Select(a => CIAttributeDTO.Build(a.Value))));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ciid"></param>
        /// <param name="name"></param>
        /// <param name="layerIDs"></param>
        /// <param name="atTime"></param>
        /// <returns></returns>
        [HttpGet("getMergedAttribute")]
        public async Task<ActionResult<CIAttributeDTO>> GetMergedAttribute([FromQuery, Required] Guid ciid, [FromQuery, Required] string name, [FromQuery, Required] long[] layerIDs, [FromQuery] DateTimeOffset? atTime = null)
        {
            var user = await currentUserService.GetCurrentUser(null);
            if (!ciBasedAuthorizationService.CanReadCI(ciid))
                return Forbid($"User \"{user.Username}\" does not have permission to write to CI {ciid}");

            var timeThreshold = (atTime.HasValue) ? TimeThreshold.BuildAtTime(atTime.Value) : TimeThreshold.BuildLatest();
            var attribute = await attributeModel.GetMergedAttribute(name, ciid, new LayerSet(layerIDs), null, timeThreshold);
            if (attribute == null)
                return NotFound();
            return Ok(CIAttributeDTO.Build(attribute));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="regex"></param>
        /// <param name="ciids"></param>
        /// <param name="layerIDs"></param>
        /// <param name="atTime"></param>
        /// <returns></returns>
        [HttpGet("findMergedAttributesByName")]
        public async Task<ActionResult<IEnumerable<CIAttributeDTO>>> FindMergedAttributesByName([FromQuery, Required] string regex, [FromQuery] IEnumerable<Guid> ciids, [FromQuery, Required] long[] layerIDs, [FromQuery] DateTimeOffset? atTime = null)
        {
            var user = await currentUserService.GetCurrentUser(null);
            ICIIDSelection selection;
            if (ciids == null)
                selection = new AllCIIDsSelection();
            else
            {
                if (!ciBasedAuthorizationService.CanReadAllCIs(ciids, out var notAllowedCI))
                    return Forbid($"User \"{user.Username}\" does not have permission to read from CI {notAllowedCI}");
                selection = SpecificCIIDsSelection.Build(ciids);
            }
            var timeThreshold = (atTime.HasValue) ? TimeThreshold.BuildAtTime(atTime.Value) : TimeThreshold.BuildLatest();
            var attributes = await attributeModel.FindMergedAttributesByName(regex, selection, new LayerSet(layerIDs), null, timeThreshold);

            if (selection is AllCIIDsSelection)
                attributes = attributes.Where(a => ciBasedAuthorizationService.CanReadCI(a.Attribute.CIID));

            return Ok(attributes.Select(a => CIAttributeDTO.Build(a)));
        }

        /// <summary>
        /// bulk replace all attributes in specified layer
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost("bulkReplaceAttributesInLayer")]
        public async Task<ActionResult> BulkReplaceAttributesInLayer([FromBody, Required] BulkCIAttributeLayerScopeDTO dto)
        {
            var user = await currentUserService.GetCurrentUser(null);
            if (!layerBasedAuthorizationService.CanUserWriteToLayer(user, dto.LayerID))
                return Forbid($"User \"{user.Username}\" does not have permission to write to layer ID {dto.LayerID}");

            var data = BulkCIAttributeDataLayerScope.BuildFromDTO(dto);

            if (!ciBasedAuthorizationService.CanWriteToAllCIs(data.Fragments.Select(f => data.GetCIID(f)), out var notAllowedCI))
                return Forbid($"User \"{user.Username}\" does not have permission to write to CI {notAllowedCI}");

            using var trans = conn.BeginTransaction();
            var changesetProxy = ChangesetProxy.Build(user.InDatabase, DateTimeOffset.Now, changesetModel);
            var inserted = await attributeModel.BulkReplaceAttributes(data, changesetProxy, trans);
            trans.Commit();
            return Ok();
        }
    }
}
