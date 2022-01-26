using Microsoft.AspNet.OData;
using Microsoft.AspNetCore.Mvc;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.Service;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Controllers.OData
{
    public class AttributeDTO
    {
        public AttributeDTO(Guid cIID, string cIName, string attributeName, string value)
        {
            CIID = cIID;
            CIName = cIName;
            AttributeName = attributeName;
            Value = value;
        }

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
        public InsertAttribute(string cIID, string cIName, string attributeName, string value)
        {
            CIID = cIID;
            CIName = cIName;
            AttributeName = attributeName;
            Value = value;
        }

        public string CIID { get; set; }
        public string CIName { get; set; }
        public string AttributeName { get; set; }
        public string Value { get; set; }
    }

    // TODO: ci based authorization
    // TODO: layer based authorization
    //[Authorize]
    //[ApiVersion("1.0")]
    public class AttributesController : ODataController
    {
        private readonly IAttributeModel attributeModel;
        private readonly ICIModel ciModel;
        private readonly IChangesetModel changesetModel;
        private readonly IODataAPIContextModel oDataAPIContextModel;
        private readonly IModelContextBuilder modelContextBuilder;
        private readonly ICurrentUserAccessor currentUserService;
        private readonly ILayerBasedAuthorizationService authorizationService;

        public AttributesController(IAttributeModel attributeModel, ICIModel ciModel, IChangesetModel changesetModel, IODataAPIContextModel oDataAPIContextModel,
            ICurrentUserAccessor currentUserService, ILayerBasedAuthorizationService authorizationService, IModelContextBuilder modelContextBuilder)
        {
            this.attributeModel = attributeModel;
            this.ciModel = ciModel;
            this.changesetModel = changesetModel;
            this.oDataAPIContextModel = oDataAPIContextModel;
            this.currentUserService = currentUserService;
            this.authorizationService = authorizationService;
            this.modelContextBuilder = modelContextBuilder;
        }

        private AttributeDTO Model2DTO(MergedCIAttribute a, string? ciName)
        {
            return new AttributeDTO(a.Attribute.CIID, ciName ?? "[Unnamed]", a.Attribute.Name, a.Attribute.Value.Value2String());
        }

        [EnableQuery]
        public async Task<AttributeDTO> GetAttributeDTO([FromODataUri, Required] Guid keyCIID, [FromODataUri] string keyAttributeName, [FromRoute] string context)
        {
            if (keyAttributeName.Equals(ICIModel.NameAttribute))
                throw new Exception("Cannot get name attribute directly");

            var trans = modelContextBuilder.BuildImmediate();
            var layerset = await ODataAPIContextService.GetReadLayersetFromContext(oDataAPIContextModel, context, trans);
            var timeThreshold = TimeThreshold.BuildLatest();
            var ci = await ciModel.GetMergedCI(keyCIID, layerset, NamedAttributesSelection.Build(keyAttributeName), trans, timeThreshold);
            if (ci.MergedAttributes.TryGetValue(keyAttributeName, out var a))
            {
                return Model2DTO(a, ci.CIName);
            }
            else
            {
                throw new Exception("Could not get attribute");
            }
        }

        [EnableQuery]
        public async Task<IEnumerable<AttributeDTO>> GetAttributes([FromRoute] string context)
        {
            var trans = modelContextBuilder.BuildImmediate();
            var layerset = await ODataAPIContextService.GetReadLayersetFromContext(oDataAPIContextModel, context, trans);
            var attributesDict = await attributeModel.GetMergedAttributes(new AllCIIDsSelection(), AllAttributeSelection.Instance, layerset, trans, TimeThreshold.BuildLatest());

            var attributes = attributesDict.SelectMany(a => a.Value.Values);

            var nameAttributes = attributes.Where(a => a.Attribute.Name.Equals(ICIModel.NameAttribute)).ToDictionary(a => a.Attribute.CIID, a => a.Attribute.Value.Value2String());

            return attributes
                .Where(a => !a.Attribute.Name.Equals(ICIModel.NameAttribute)) // filter out name attributes
                .Select(a =>
                {
                    nameAttributes.TryGetValue(a.Attribute.CIID, out var name);
                    return Model2DTO(a, name);
                });
        }

        public async Task<IActionResult> Patch([FromODataUri] Guid keyCIID, [FromODataUri] string keyCIName, [FromODataUri] string keyAttributeName, [FromBody] Delta<AttributeDTO> test, [FromRoute] string context)
        {
            using var trans = modelContextBuilder.BuildDeferred();
            var writeLayerID = await ODataAPIContextService.GetWriteLayerIDFromContext(oDataAPIContextModel, context, trans);
            var readLayerset = await ODataAPIContextService.GetReadLayersetFromContext(oDataAPIContextModel, context, trans);

            var user = await currentUserService.GetCurrentUser(trans);
            if (!authorizationService.CanUserWriteToLayer(user, writeLayerID))
                return Forbid($"User \"{user.Username}\" does not have permission to write to layer ID {writeLayerID}");

            var oldCI = await ciModel.GetMergedCI(keyCIID, readLayerset, NamedAttributesSelection.Build(keyAttributeName), trans, TimeThreshold.BuildLatest());
            if (oldCI == null) return BadRequest();
            var old = oldCI.MergedAttributes.GetOrWithClass(keyAttributeName, null);
            if (old == null) return BadRequest();
            var oldDTO = Model2DTO(old, keyCIName);

            test.CopyChangedValues(oldDTO);
            var @newDTO = oldDTO;
            var changesetProxy = new ChangesetProxy(user.InDatabase, TimeThreshold.BuildLatest(), changesetModel);
            await attributeModel.InsertAttribute(@newDTO.AttributeName, new AttributeScalarValueText(@newDTO.Value), @newDTO.CIID, writeLayerID, changesetProxy, new DataOriginV1(DataOriginType.Manual), trans);

            var newMergedCI = await ciModel.GetMergedCI(keyCIID, readLayerset, NamedAttributesSelection.Build(keyAttributeName), trans, TimeThreshold.BuildLatest());
            if (newMergedCI == null) return BadRequest();
            var newMerged = newMergedCI.MergedAttributes.GetOrWithClass(keyAttributeName, null);
            if (newMerged == null) return BadRequest();
            trans.Commit();

            @newDTO = Model2DTO(newMerged, keyCIName);
            return Updated(@newDTO);
        }

        [EnableQuery]
        public async Task<IActionResult> Post([FromBody] InsertAttribute attribute, [FromRoute] string context)
        {
            if (attribute == null)
                return BadRequest($"Could not parse inserted attribute");
            if (attribute.AttributeName == null)
                return BadRequest($"Attribute Name must be set");
            if (attribute.Value == null)
                return BadRequest($"Attribute Value must be set");

            using var trans = modelContextBuilder.BuildDeferred();
            var writeLayerID = await ODataAPIContextService.GetWriteLayerIDFromContext(oDataAPIContextModel, context, trans);
            var readLayerset = await ODataAPIContextService.GetReadLayersetFromContext(oDataAPIContextModel, context, trans);

            var user = await currentUserService.GetCurrentUser(trans);
            if (!authorizationService.CanUserWriteToLayer(user, writeLayerID))
                return Forbid($"User \"{user.Username}\" does not have permission to write to layer ID {writeLayerID}");

            var timeThreshold = TimeThreshold.BuildLatest();

            var finalCIID = Guid.NewGuid();
            if (attribute.CIID != null)
            {
                if (!Guid.TryParse(attribute.CIID, out finalCIID))
                    return BadRequest($"Malformed CI-ID");
            }
            else if (attribute.CIName != null && attribute.CIName != "")
            { // ciid not set, try to match using ci name, which is set

                // TODO: performance improvements
                var ciNamesFromNameAttributes = await attributeModel.GetMergedCINames(new AllCIIDsSelection(), readLayerset, trans, timeThreshold);
                var foundCIIDs = ciNamesFromNameAttributes.Where(a => a.Value.Equals(attribute.CIName)).Select(a => a.Key).ToList();
                if (foundCIIDs.Count == 0)
                { // ok case, continue
                }
                else if (foundCIIDs.Count == 1)
                { // found a single candidate that fits, set CIID to this
                    finalCIID = foundCIIDs[0];
                }
                else
                {
                    return BadRequest($"Cannot insert attribute via its CI-Name: CI-Name is not unique");
                }
            }

            var changesetProxy = new ChangesetProxy(user.InDatabase, timeThreshold, changesetModel);

            // check if the ciid exists, create if not
            if (!(await ciModel.CIIDExists(finalCIID, trans)))
            {
                await ciModel.CreateCI(finalCIID, trans);
                if (attribute.CIName != null && attribute.CIName != "")
                    await attributeModel.InsertCINameAttribute(attribute.CIName, finalCIID, writeLayerID, changesetProxy, new DataOriginV1(DataOriginType.Manual), trans);
            }
            else
            { // ci exists already, make sure either name is not set or it matches already present name
                if (attribute.CIName != null && attribute.CIName != "")
                {
                    var tmpCI = await ciModel.GetMergedCI(finalCIID, readLayerset, NamedAttributesSelection.Build(ICIModel.NameAttribute), trans, timeThreshold);
                    if (tmpCI == null || tmpCI.CIName == null || !attribute.CIName.Equals(tmpCI.CIName))
                        return BadRequest($"Cannot set new CI-Name on insert");
                }
            }

            await attributeModel.InsertAttribute(attribute.AttributeName, new AttributeScalarValueText(attribute.Value), finalCIID, writeLayerID, changesetProxy, new DataOriginV1(DataOriginType.Manual), trans);

            var timeThresholdAfter = TimeThreshold.BuildLatest();
            var finalCI = await ciModel.GetMergedCI(finalCIID, readLayerset, NamedAttributesSelection.Build(attribute.AttributeName), trans, timeThresholdAfter);
            if (finalCI == null) return BadRequest();
            var createdMerged = finalCI.MergedAttributes.GetOrWithClass(attribute.AttributeName, null);
            if (createdMerged == null) return BadRequest();
            trans.Commit();

            return Created(Model2DTO(createdMerged, finalCI.CIName));
        }

        [EnableQuery]
        public async Task<IActionResult> Delete([FromODataUri] Guid keyCIID, [FromODataUri] string keyAttributeName, [FromRoute] string context)
        {
            try
            {
                using var trans = modelContextBuilder.BuildDeferred();
                var writeLayerID = await ODataAPIContextService.GetWriteLayerIDFromContext(oDataAPIContextModel, context, trans);

                var user = await currentUserService.GetCurrentUser(trans);
                if (!authorizationService.CanUserWriteToLayer(user, writeLayerID))
                    return Forbid($"User \"{user.Username}\" does not have permission to write to layer ID {writeLayerID}");

                var changesetProxy = new ChangesetProxy(user.InDatabase, TimeThreshold.BuildLatest(), changesetModel);
                await attributeModel.RemoveAttribute(keyAttributeName, keyCIID, writeLayerID, changesetProxy, new DataOriginV1(DataOriginType.Manual), trans, MaskHandlingForRemovalApplyNoMask.Instance);
                trans.Commit();
            }
            catch (Exception)
            {
                return BadRequest();
            }

            return NoContent();
        }
    }
}
