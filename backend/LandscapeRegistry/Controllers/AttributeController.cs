using Landscape.Base.Entity;
using Landscape.Base.Entity.DTO;
using Landscape.Base.Model;
using LandscapeRegistry.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.ComponentModel.DataAnnotations;
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
        /// bulk replace all attributes in specified layer
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost("bulkReplaceAttributesInLayer")]
        public async Task<ActionResult> BulkReplaceAttributesInLayer([FromBody, Required]BulkCIAttributeLayerScopeDTO dto)
        {
            var user = await currentUserService.GetCurrentUser(null);
            if (!authorizationService.CanUserWriteToLayer(user, dto.LayerID))
                return BadRequest($"User \"{user.Username}\" does not have permission to write to layer ID {dto.LayerID}");

            using var trans = conn.BeginTransaction();
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
