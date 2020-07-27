using Landscape.Base.Entity;
using Landscape.Base.Entity.DTO;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using LandscapeRegistry.Model;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.OData.Query;
using LandscapeRegistry.Entity.AttributeValues;
using Npgsql;
using LandscapeRegistry.Service;

namespace LandscapeRegistry.Controllers.OData
{
    public class Attribute
    {
        [Key]
        public Guid CIID { get; set; }
        [Key]
        public string Name { get; set; }
        public string Value { get; set; }
    }

    [Authorize]
    public class AttributesController : ODataController
    {
        private readonly IAttributeModel attributeModel;
        private readonly IChangesetModel changesetModel;
        private readonly NpgsqlConnection conn;
        private readonly ICurrentUserService currentUserService;
        private readonly IRegistryAuthorizationService authorizationService;

        public AttributesController(IAttributeModel attributeModel, IChangesetModel changesetModel, ICurrentUserService currentUserService, IRegistryAuthorizationService authorizationService, NpgsqlConnection conn)
        {
            this.attributeModel = attributeModel;
            this.changesetModel = changesetModel;
            this.currentUserService = currentUserService;
            this.authorizationService = authorizationService;
            this.conn = conn;
        }

        private Attribute Model2DTO(CIAttribute a)
        {
            return new Attribute() { CIID = a.CIID, Name = a.Name, Value = a.Value.Value2String() };
        }

        [EnableQuery]
        public async Task<Attribute> GetAttribute([FromODataUri,Required]Guid keyCIID, [FromODataUri]string keyName, [FromRoute]int layerID)
        {
            var a = await attributeModel.GetAttribute(keyName, layerID, keyCIID, null, TimeThreshold.BuildLatest());
            return Model2DTO(a);
        }

        [EnableQuery]
        public async Task<IEnumerable<Attribute>> GetAttributes([FromRoute]int layerID)
        {
            var attributes = await attributeModel.GetAttributes(new AllCIIDsSelection(), false, layerID, null, TimeThreshold.BuildLatest());
            return attributes.Select(a => Model2DTO(a));
        }

        public async Task<IActionResult> Patch([FromODataUri]Guid keyCIID, [FromODataUri]string keyName, [FromBody] Delta<Attribute> test, [FromRoute]int layerID)
        {
            var user = await currentUserService.GetCurrentUser(null);
            if (!authorizationService.CanUserWriteToLayer(user, layerID))
                return BadRequest($"User \"{user.Username}\" does not have permission to write to layer ID {layerID}");


            var old = await attributeModel.GetAttribute(keyName, layerID, keyCIID, null, TimeThreshold.BuildLatest());
            if (old == null) return BadRequest();
            var oldDTO = Model2DTO(old);

            test.CopyChangedValues(oldDTO);
            var @newDTO = oldDTO;
            using var trans = conn.BeginTransaction();
            var changesetProxy = ChangesetProxy.Build(user.InDatabase, DateTimeOffset.Now, changesetModel);
            var @new = await attributeModel.InsertAttribute(@newDTO.Name, AttributeScalarValueText.Build(@newDTO.Value), layerID, @newDTO.CIID, changesetProxy, trans);

            trans.Commit();

            @newDTO = Model2DTO(@new);
            return Updated(@newDTO);
        }

        [EnableQuery]
        public async Task<IActionResult> Post([FromBody] Attribute attribute, [FromRoute]int layerID)
        {
            var user = await currentUserService.GetCurrentUser(null);
            if (!authorizationService.CanUserWriteToLayer(user, layerID))
                return BadRequest($"User \"{user.Username}\" does not have permission to write to layer ID {layerID}");

            using var trans = conn.BeginTransaction();
            var changesetProxy = ChangesetProxy.Build(user.InDatabase, DateTimeOffset.Now, changesetModel);
            var created = await attributeModel.InsertAttribute(attribute.Name, AttributeScalarValueText.Build(attribute.Value), layerID, attribute.CIID, changesetProxy, trans);

            trans.Commit();

            return Created(Model2DTO(created));
        }

        [EnableQuery]
        public async Task<IActionResult> Delete([FromODataUri]Guid keyCIID, [FromODataUri]string keyName, [FromRoute]int layerID)
        {
            var user = await currentUserService.GetCurrentUser(null);
            if (!authorizationService.CanUserWriteToLayer(user, layerID))
                return BadRequest($"User \"{user.Username}\" does not have permission to write to layer ID {layerID}");

            using var trans = conn.BeginTransaction();
            var changesetProxy = ChangesetProxy.Build(user.InDatabase, DateTimeOffset.Now, changesetModel);
            var removed = await attributeModel.RemoveAttribute(keyName, layerID, keyCIID, changesetProxy, trans);

            if (removed == null)
                return NotFound();

            trans.Commit();

            return NoContent();
        }
    }
}
