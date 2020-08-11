﻿using Landscape.Base.Entity;
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
    public class AttributeDTO
    {
        [Key]
        public Guid CIID { get; set; }
        [Key]
        public string CIName { get; set; }
        [Key]
        public string AttributeName { get; set; }
        public string Value { get; set; }
    }

    public class InsertAttribute
    {
        public string CIID { get; set; }
        public string CIName { get; set; }
        public string AttributeName { get; set; }
        public string Value { get; set; }
    }

    [Authorize]
    public class AttributesController : ODataController
    {
        private readonly IBaseAttributeModel attributeModel;
        private readonly ICIModel ciModel;
        private readonly IChangesetModel changesetModel;
        private readonly ICISearchModel ciSearchModel;
        private readonly NpgsqlConnection conn;
        private readonly ICurrentUserService currentUserService;
        private readonly IRegistryAuthorizationService authorizationService;

        public AttributesController(IBaseAttributeModel attributeModel, ICIModel ciModel, IChangesetModel changesetModel, ICISearchModel ciSearchModel, ICurrentUserService currentUserService, IRegistryAuthorizationService authorizationService, NpgsqlConnection conn)
        {
            this.attributeModel = attributeModel;
            this.ciModel = ciModel;
            this.changesetModel = changesetModel;
            this.ciSearchModel = ciSearchModel;
            this.currentUserService = currentUserService;
            this.authorizationService = authorizationService;
            this.conn = conn;
        }

        private AttributeDTO Model2DTO(CIAttribute a, string ciName)
        {
            return new AttributeDTO() { CIID = a.CIID, CIName = ciName ?? "[Unnamed]", AttributeName = a.Name, Value = a.Value.Value2String() };
        }

        [EnableQuery]
        public async Task<AttributeDTO> GetAttributeDTO([FromODataUri,Required]Guid keyCIID, [FromODataUri]string keyAttributeName, [FromRoute]int layerID)
        {
            if (keyAttributeName.Equals(ICIModel.NameAttribute))
                throw new Exception("Cannot get name attribute directly");

            var timeThreshold = TimeThreshold.BuildLatest();
            var a = await attributeModel.GetAttribute(keyAttributeName, layerID, keyCIID, null, timeThreshold);
            var nameAttribute = await attributeModel.GetAttribute(ICIModel.NameAttribute, layerID, keyCIID, null, timeThreshold);
            return Model2DTO(a, nameAttribute?.Value.Value2String());
        }

        [EnableQuery]
        public async Task<IEnumerable<AttributeDTO>> GetAttributes([FromRoute]int layerID)
        {
            var attributes = await attributeModel.GetAttributes(new AllCIIDsSelection(), false, layerID, null, TimeThreshold.BuildLatest());

            var nameAttributes = attributes.Where(a => a.Name.Equals(ICIModel.NameAttribute)).ToDictionary(a => a.CIID, a => a.Value.Value2String());

            return attributes
                .Where(a => !a.Name.Equals(ICIModel.NameAttribute)) // filter out name attributes
                .Select(a => {
                nameAttributes.TryGetValue(a.CIID, out var name);
                return Model2DTO(a, name);
            });
        }

        public async Task<IActionResult> Patch([FromODataUri]Guid keyCIID, [FromODataUri]string keyCIName, [FromODataUri]string keyAttributeName, [FromBody] Delta<AttributeDTO> test, [FromRoute]int layerID)
        {
            var user = await currentUserService.GetCurrentUser(null);
            if (!authorizationService.CanUserWriteToLayer(user, layerID))
                return BadRequest($"User \"{user.Username}\" does not have permission to write to layer ID {layerID}");


            var old = await attributeModel.GetAttribute(keyAttributeName, layerID, keyCIID, null, TimeThreshold.BuildLatest());
            if (old == null) return BadRequest();
            var oldDTO = Model2DTO(old, keyCIName);

            test.CopyChangedValues(oldDTO);
            var @newDTO = oldDTO;
            using var trans = conn.BeginTransaction();
            var changesetProxy = ChangesetProxy.Build(user.InDatabase, DateTimeOffset.Now, changesetModel);
            var @new = await attributeModel.InsertAttribute(@newDTO.AttributeName, AttributeScalarValueText.Build(@newDTO.Value), layerID, @newDTO.CIID, changesetProxy, trans);

            trans.Commit();

            @newDTO = Model2DTO(@new, keyCIName);
            return Updated(@newDTO);
        }

        [EnableQuery]
        public async Task<IActionResult> Post([FromBody] InsertAttribute attribute, [FromRoute]int layerID)
        {
            if (attribute == null)
                return BadRequest($"Could not parse inserted attribute");
            if (attribute.AttributeName == null)
                return BadRequest($"Attribute Name must be set");
            if (attribute.Value == null)
                return BadRequest($"Attribute Value must be set");

            var user = await currentUserService.GetCurrentUser(null);
            if (!authorizationService.CanUserWriteToLayer(user, layerID))
                return BadRequest($"User \"{user.Username}\" does not have permission to write to layer ID {layerID}");

            using var trans = conn.BeginTransaction();
            var timeThreshold = TimeThreshold.BuildLatest();

            var finalCIID = Guid.NewGuid();
            if (attribute.CIID != null)
            {
                if (!Guid.TryParse(attribute.CIID, out finalCIID))
                    return BadRequest($"Malformed CI-ID");
            }
            else if (attribute.CIName != null && attribute.CIName != "")
            { // ciid not set, try to match using ci name, which is set
                var foundCIs = (await ciSearchModel.FindCIsWithName(attribute.CIName, new LayerSet(new long[] { layerID }), trans, timeThreshold)).ToList();
                if (foundCIs.Count == 0)
                { // ok case, continue
                }
                else if (foundCIs.Count == 1)
                { // found a single candidate that fits, set CIID to this
                    finalCIID = foundCIs[0].ID;
                } else
                {
                    return BadRequest($"Cannot insert attribute via its CI-Name: CI-Name is not unique");
                }
            }

            var changesetProxy = ChangesetProxy.Build(user.InDatabase, timeThreshold.Time, changesetModel);

            // check if the ciid exists, create if not
            if (!(await ciModel.CIIDExists(finalCIID, trans)))
            {
                await ciModel.CreateCI(trans, finalCIID);
                if (attribute.CIName != null && attribute.CIName != "")
                    await attributeModel.InsertCINameAttribute(attribute.CIName, layerID, finalCIID, changesetProxy, trans);
            } else
            { // ci exists already, make sure either name is not set or it matches already present name
                if (attribute.CIName != null && attribute.CIName != "")
                {
                    var currentNameAttribute = await attributeModel.GetAttribute(ICIModel.NameAttribute, layerID, finalCIID, null, timeThreshold);
                    if (currentNameAttribute == null || !attribute.CIName.Equals(currentNameAttribute.Value.Value2String()))
                        return BadRequest($"Cannot set new CI-Name on insert");
                }
            }

            var created = await attributeModel.InsertAttribute(attribute.AttributeName, AttributeScalarValueText.Build(attribute.Value), layerID, finalCIID, changesetProxy, trans);

            var nameAttribute = await attributeModel.GetAttribute(ICIModel.NameAttribute, layerID, finalCIID, trans, timeThreshold);

            trans.Commit();

            return Created(Model2DTO(created, nameAttribute?.Value.Value2String()));
        }

        [EnableQuery]
        public async Task<IActionResult> Delete([FromODataUri]Guid keyCIID, [FromODataUri]string keyAttributeName, [FromRoute]int layerID)
        {
            var user = await currentUserService.GetCurrentUser(null);
            if (!authorizationService.CanUserWriteToLayer(user, layerID))
                return BadRequest($"User \"{user.Username}\" does not have permission to write to layer ID {layerID}");

            using var trans = conn.BeginTransaction();
            var changesetProxy = ChangesetProxy.Build(user.InDatabase, DateTimeOffset.Now, changesetModel);
            var removed = await attributeModel.RemoveAttribute(keyAttributeName, layerID, keyCIID, changesetProxy, trans);

            if (removed == null)
                return NotFound();

            trans.Commit();

            return NoContent();
        }
    }
}
