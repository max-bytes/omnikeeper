﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Entity.DTO;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
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
    public class AttributeController : ControllerBase
    {
        private readonly IAttributeModel attributeModel;
        private readonly IChangesetModel changesetModel;
        private readonly ICurrentUserService currentUserService;
        private readonly ILayerBasedAuthorizationService layerBasedAuthorizationService;
        private readonly ICIBasedAuthorizationService ciBasedAuthorizationService;
        private readonly IModelContextBuilder modelContextBuilder;

        public AttributeController(IAttributeModel attributeModel, IChangesetModel changesetModel, ICurrentUserService currentUserService,
            ILayerBasedAuthorizationService layerBasedAuthorizationService, IModelContextBuilder modelContextBuilder, ICIBasedAuthorizationService ciBasedAuthorizationService)
        {
            this.modelContextBuilder = modelContextBuilder;
            this.changesetModel = changesetModel;
            this.attributeModel = attributeModel;
            this.layerBasedAuthorizationService = layerBasedAuthorizationService;
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
        public async Task<ActionResult<IEnumerable<CIAttributeDTO>>> GetMergedAttributesWithName([FromQuery, Required] string name, [FromQuery, Required] string[] layerIDs, [FromQuery] DateTimeOffset? atTime = null)
        {
            var trans = modelContextBuilder.BuildImmediate();
            var user = await currentUserService.GetCurrentUser(trans);

            if (!layerBasedAuthorizationService.CanUserReadFromAllLayers(user, layerIDs))
                return Forbid($"User \"{user.Username}\" does not have permission to read from at least one of the following layerIDs: {string.Join(',', layerIDs)}");

            var timeThreshold = (atTime.HasValue) ? TimeThreshold.BuildAtTime(atTime.Value) : TimeThreshold.BuildLatest();
            var layerset = new LayerSet(layerIDs);
            var attributesDict = await attributeModel.FindMergedAttributesByFullName(name, new AllCIIDsSelection(), layerset, trans, timeThreshold);

            var attributes = attributesDict
                .Where(kv => ciBasedAuthorizationService.CanReadCI(kv.Key)) // TODO: refactor to use a method that queries all ciids at once, returning those that are readable
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
        public async Task<ActionResult<IEnumerable<CIAttributeDTO>>> GetMergedAttributes([FromQuery, Required] IEnumerable<Guid> ciids, [FromQuery, Required] string[] layerIDs, [FromQuery] DateTimeOffset? atTime = null)
        {
            if (ciids.IsEmpty())
                return BadRequest("Empty CIID list");

            ISet<Guid> ciidSet = ciids.ToHashSet(); // TODO: needed

            // TODO: add support for nameRegexFilter, as attributeModel.GetMergedAttributes supports it and the OIAOmnikeeper needs it

            var trans = modelContextBuilder.BuildImmediate();
            var user = await currentUserService.GetCurrentUser(trans);
            if (!layerBasedAuthorizationService.CanUserReadFromAllLayers(user, layerIDs))
                return Forbid($"User \"{user.Username}\" does not have permission to read from at least one of the following layerIDs: {string.Join(',', layerIDs)}");
            if (!ciBasedAuthorizationService.CanReadAllCIs(ciidSet, out var notAllowedCI))
                return Forbid($"User \"{user.Username}\" does not have permission to read from CI {notAllowedCI}");

            var timeThreshold = (atTime.HasValue) ? TimeThreshold.BuildAtTime(atTime.Value) : TimeThreshold.BuildLatest();
            var layerset = new LayerSet(layerIDs);
            var attributes = await attributeModel.GetMergedAttributes(SpecificCIIDsSelection.Build(ciidSet), AllAttributeSelection.Instance, layerset, trans, timeThreshold);
            return Ok(attributes.SelectMany(t => t.Value.Select(a => CIAttributeDTO.Build(a.Value))));
        }

        /// <summary>
        /// bulk replace all attributes in specified layer
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost("bulkReplaceAttributesInLayer")]
        public async Task<ActionResult> BulkReplaceAttributesInLayer([FromBody, Required] BulkCIAttributeLayerScopeDTO dto)
        {
            using var trans = modelContextBuilder.BuildDeferred();
            var user = await currentUserService.GetCurrentUser(trans);
            if (!layerBasedAuthorizationService.CanUserWriteToLayer(user, dto.LayerID))
                return Forbid($"User \"{user.Username}\" does not have permission to write to layer ID {dto.LayerID}");

            var data = BulkCIAttributeDataLayerScope.BuildFromDTO(dto);

            if (!ciBasedAuthorizationService.CanWriteToAllCIs(data.Fragments.Select(f => data.GetCIID(f)), out var notAllowedCI))
                return Forbid($"User \"{user.Username}\" does not have permission to write to CI {notAllowedCI}");

            var changesetProxy = new ChangesetProxy(user.InDatabase, TimeThreshold.BuildLatest(), changesetModel);
            var inserted = await attributeModel.BulkReplaceAttributes(data, changesetProxy, new DataOriginV1(DataOriginType.Manual), trans);
            trans.Commit();
            return Ok();
        }
    }
}
