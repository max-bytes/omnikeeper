using Landscape.Base.Entity;
using Landscape.Base.Entity.DTO;
using Landscape.Base.Model;
using LandscapeRegistry.Model;
using LandscapeRegistry.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System;
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
    public class AttributeController : ControllerBase
    {
        private readonly IAttributeModel attributeModel;
        private readonly IChangesetModel changesetModel;
        private readonly CurrentUserService currentUserService;
        private readonly NpgsqlConnection conn;

        public AttributeController(IAttributeModel attributeModel, IChangesetModel changesetModel, CurrentUserService currentUserService, NpgsqlConnection conn)
        {
            this.conn = conn;
            this.changesetModel = changesetModel;
            this.attributeModel = attributeModel;
            this.currentUserService = currentUserService;
        }

        [HttpPost("bulkReplaceAttributesInLayer")]
        public async Task<ActionResult> BulkReplaceAttributesInLayer([FromBody, Required]BulkCIAttributeLayerScopeDTO dto)
        {
            var user = await currentUserService.GetCurrentUser(null);

            using (var trans = conn.BeginTransaction())
            {
                var changeset = await changesetModel.CreateChangeset(user.InDatabase.ID, trans);
                var data = BulkCIAttributeDataLayerScope.BuildFromDTO(dto);
                var success = await attributeModel.BulkReplaceAttributes(data, changeset.ID, trans);
                if (success)
                {
                    trans.Commit();
                    return Ok();
                }
                else return BadRequest();
            }
        }
    }
}
