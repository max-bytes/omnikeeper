﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Omnikeeper.Base.Authz;
using Omnikeeper.Authz;

namespace Omnikeeper.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class AttributeValueImageController : ControllerBase
    {
        private readonly IAttributeModel attributeModel;
        private readonly IChangesetModel changesetModel;
        private readonly ICurrentUserAccessor currentUserService;
        private readonly IAuthzFilterManager authzFilterManager;
        private readonly IModelContextBuilder modelContextBuilder;

        public AttributeValueImageController(IAttributeModel attributeModel, ICurrentUserAccessor currentUserService,
            IModelContextBuilder modelContextBuilder, IChangesetModel changesetModel, IAuthzFilterManager authzFilterManager)
        {
            this.attributeModel = attributeModel;
            this.changesetModel = changesetModel;
            this.currentUserService = currentUserService;
            this.authzFilterManager = authzFilterManager;
            this.modelContextBuilder = modelContextBuilder;
        }

        [HttpGet("")]
        [AllowAnonymous] // TODO: implementing proper authentication for image loading is hard, see https://stackoverflow.com/questions/34096744/how-should-i-load-images-if-i-use-token-based-authentication
        public async Task<IActionResult> Get([FromQuery, Required] Guid ciid, [FromQuery, Required] string attributeName, [FromQuery, Required] string[] layerIDs, [FromQuery] int index = 0, [FromQuery] DateTimeOffset? atTime = null)
        {
            if (layerIDs.IsEmpty())
                return BadRequest("No layer IDs specified");

            using var trans = modelContextBuilder.BuildImmediate();
            var user = await currentUserService.GetCurrentUser(trans);
            var timeThreshold = (atTime.HasValue) ? TimeThreshold.BuildAtTime(atTime.Value) : TimeThreshold.BuildLatest();
            var layerset = new LayerSet(layerIDs);

            if (await authzFilterManager.ApplyFilterForQuery(new QueryOperationContext(), user, layerset, trans, timeThreshold) is AuthzFilterResultDeny d)
                return Forbid(d.Reason);

            var a = await attributeModel.GetFullBinaryMergedAttribute(attributeName, ciid, layerset, trans, timeThreshold);
            if (a == null)
                return NotFound($"Could not find attribute \"{attributeName}\" in CI {ciid}");
            if (a.Attribute.Value.Type != AttributeValueType.Image)
                return Problem($"Expected attribute value type to be image, encountered \"{a.Attribute.Value.Type}\"");
            if (a.Attribute.Value is AttributeScalarValueImage scalar)
            {
                if (index != 0)
                {
                    return BadRequest($"Requested index {index} of scalar attribute value image");
                }
                var mimeType = scalar.Value.MimeType;
                return File(scalar.Value.FullData!, mimeType);
            }
            else if (a.Attribute.Value is AttributeArrayValueImage array)
            {
                if (index < 0 || index >= array.Values.Length)
                {
                    return BadRequest($"Requested index {index} outside of valid boundaries (0-{array.Values.Length - 1})");
                }
                var mimeType = array.Values[index].Value.MimeType;
                return File(array.Values[index].Value.FullData!, mimeType);
            }
            else
            {
                return Problem($"Unexpected error parsing attribute value");
            }
        }

        [HttpPost("")]
        public async Task<IActionResult> Post([FromQuery, Required] Guid ciid, [FromQuery, Required] string attributeName, [FromQuery, Required] string layerID, [FromForm, Required] IEnumerable<IFormFile> files, [FromQuery] bool forceArray = false)
        {
            if (files.IsEmpty())
                return BadRequest("At least one image is required");
            if (files.Any(t => !t.ContentType.StartsWith("image/")))
                return BadRequest("Encountered file with invalid content-type. Only images are allowed");
            using var trans = modelContextBuilder.BuildDeferred();
            var user = await currentUserService.GetCurrentUser(trans);
            var timeThreshold = TimeThreshold.BuildLatest();

            if (await authzFilterManager.ApplyPreFilterForMutation(new PreMutateContextForCIs(), user, layerID, layerID, trans, timeThreshold) is AuthzFilterResultDeny d)
                return Forbid(d.Reason);

            var fileStreams = files.Select<IFormFile, (Func<Stream> stream, string contentType, string filename)>(f => (
                   () => f.OpenReadStream(),
                   f.ContentType,
                   Path.GetFileName(f.FileName) // stripping path for security reasons: https://docs.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-3.1#upload-small-files-with-buffered-model-binding-to-physical-storage-1
               ));

            var proxies = fileStreams.Select(t =>
            {
                using var stream = t.stream();
                using var memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream);
                var bytes = memoryStream.ToArray();
                return BinaryScalarAttributeValueProxy.BuildFromFullData(t.contentType, bytes);
            }).ToArray();

            IAttributeValue av;
            if (proxies.Length > 1 || forceArray)
            {
                av = AttributeArrayValueImage.Build(proxies);
            }
            else
            {
                av = new AttributeScalarValueImage(proxies[0]);
            }

            var changesetProxy = new ChangesetProxy(user.InDatabase, timeThreshold, changesetModel, new DataOriginV1(DataOriginType.Manual));
            await attributeModel.InsertAttribute(attributeName, av, ciid, layerID, changesetProxy, trans, OtherLayersValueHandlingForceWrite.Instance);

            if (await authzFilterManager.ApplyPostFilterForMutation(new PostMutateContextForCIs(), user, layerID, changesetProxy.GetActiveChangeset(layerID), trans, timeThreshold) is AuthzFilterResultDeny dPost)
                return Forbid(dPost.Reason);

            trans.Commit();
            return Ok();
        }
    }
}
