using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.DotNet.PlatformAbstractions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OKPluginGenericJSONIngest;
using OKPluginGenericJSONIngest.Transform.JMESPath;
using OKPluginGenericJSONIngest.Load;
using Omnikeeper.Base.Entity;
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
using Microsoft.AspNetCore.Http;
using OKPluginGenericJSONIngest.Extract;

namespace Omnikeeper.Controllers.Ingest
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/ingest/genericJSON/files")]
    [Authorize]
    public class PassiveFilesController : ControllerBase
    {
        private readonly IngestDataService ingestDataService;
        private readonly ILayerModel layerModel;
        private readonly ILogger<PassiveFilesController> logger;
        private readonly ICurrentUserService currentUserService;
        private readonly IContextModel contextModel;
        private readonly IModelContextBuilder modelContextBuilder;
        private readonly ILayerBasedAuthorizationService authorizationService;

        public PassiveFilesController(IngestDataService ingestDataService, ILayerModel layerModel, ICurrentUserService currentUserService,
            IContextModel contextModel, IModelContextBuilder modelContextBuilder,
            ILayerBasedAuthorizationService authorizationService, ILogger<PassiveFilesController> logger)
        {
            this.ingestDataService = ingestDataService;
            this.layerModel = layerModel;
            this.logger = logger;
            this.currentUserService = currentUserService;
            this.contextModel = contextModel;
            this.modelContextBuilder = modelContextBuilder;
            this.authorizationService = authorizationService;
        }

        [HttpPost("")]
        public async Task<ActionResult> Ingest([FromQuery, Required]string context, [FromForm, Required] IEnumerable<IFormFile> files)
        {
            try
            {
                var ctx = contextModel.GetContextByName(context);
                if (ctx == null)
                    return BadRequest($"Context with name \"{context}\" not found");
                if (!(ctx.ExtractConfig is ExtractConfigPassiveRESTFiles f))
                    return BadRequest($"Context with name \"{context}\" does not accept files via REST API");
                if (files.IsEmpty())
                    return BadRequest($"No files specified");

                // TODO: think about maybe requiring specific file(name)s and making that configurable
                var fileStreams = files.Select<IFormFile, (Func<Stream> stream, string filename)>(f => (
                   () => f.OpenReadStream(),
                   Path.GetFileName(f.FileName) // stripping path for security reasons: https://docs.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-3.1#upload-small-files-with-buffered-model-binding-to-physical-storage-1
               ));

                using var mc = modelContextBuilder.BuildImmediate();
                var searchLayers = new LayerSet(ctx.LoadConfig.SearchLayerIDs);
                var writeLayer = await layerModel.GetLayer(ctx.LoadConfig.WriteLayerID, mc);
                if (writeLayer == null)
                {
                    return BadRequest($"Cannot write to layer with ID {ctx.LoadConfig.WriteLayerID}: layer does not exist");
                }

                var user = await currentUserService.GetCurrentUser(mc);

                // authorization
                if (!authorizationService.CanUserWriteToLayer(user, writeLayer))
                {
                    return Forbid();
                }
                // NOTE: we don't do any ci-based authorization here... its pretty hard to do because of all the temporary CIs
                // TODO: think about this!

                GenericInboundData genericInboundData;
                switch (ctx.TransformConfig)
                {
                    case TransformConfigJMESPath jmesPathConfig:
                        var transformer = new TransformerJMESPath();
                        var documents = new Dictionary<string, JToken>();
                        foreach (var fileStream in fileStreams)
                        {
                            using var stream = fileStream.stream();
                            using var reader = new StreamReader(stream);
                            using var jsonReader = new JsonTextReader(reader)
                            {
                                DateParseHandling = DateParseHandling.None // TODO: ensure that we always set this!
                            };
                            var json = JToken.ReadFrom(jsonReader);
                            documents.Add(fileStream.filename, json);
                        }
                        var inputJSON = transformer.Documents2JSON(documents);
                        var genericInboundDataJson = transformer.TransformJSON(inputJSON, jmesPathConfig);
                        genericInboundData = transformer.DeserializeJson(genericInboundDataJson);
                        break;
                    default:
                        throw new Exception("Encountered unknown transform config");
                }

                var preparer = new Preparer();
                var ingestData = preparer.GenericInboundData2IngestData(genericInboundData, searchLayers);

                var (numIngestedCIs, numIngestedRelations) = await ingestDataService.Ingest(ingestData, writeLayer, user);

                logger.LogInformation($"Ingest successful; ingested {numIngestedCIs} CIs, {numIngestedRelations} relations");

                return Ok();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Ingest failed");
                return BadRequest(e);
            }
        }
    }
}
