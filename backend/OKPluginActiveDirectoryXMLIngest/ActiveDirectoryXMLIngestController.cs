using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Ingest.ActiveDirectoryXML;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Controllers.Ingest
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/Ingest/AD-XML")]
    [Authorize]
    public class ActiveDirectoryXMLIngestController : ControllerBase
    {
        private readonly IngestDataService ingestDataService;
        private readonly ICurrentUserAccessor currentUserService;
        private readonly ILayerModel layerModel;
        private readonly ActiveDirectoryXMLIngestService ingestActiveDirectoryXMLService;
        private readonly IModelContextBuilder modelContextBuilder;
        private readonly ILayerBasedAuthorizationService authorizationService;
        private readonly ILogger<ActiveDirectoryXMLIngestController> logger;
        private readonly IIssuePersister issuePersister;
        private readonly IChangesetModel changesetModel;

        public ActiveDirectoryXMLIngestController(IngestDataService ingestDataService, ICurrentUserAccessor currentUserService, ILayerModel layerModel, ActiveDirectoryXMLIngestService ingestActiveDirectoryXMLService,
            IModelContextBuilder modelContextBuilder, ILayerBasedAuthorizationService authorizationService, ILogger<ActiveDirectoryXMLIngestController> logger,
            IIssuePersister issuePersister, IChangesetModel changesetModel)
        {
            this.ingestDataService = ingestDataService;
            this.currentUserService = currentUserService;
            this.layerModel = layerModel;
            this.ingestActiveDirectoryXMLService = ingestActiveDirectoryXMLService;
            this.modelContextBuilder = modelContextBuilder;
            this.authorizationService = authorizationService;
            this.logger = logger;
            this.issuePersister = issuePersister;
            this.changesetModel = changesetModel;
        }

        // TODO: rework into a context based approach?
        [HttpPost("")]
        public async Task<ActionResult> IngestXML([FromForm, Required] string writeLayerID, [FromForm, Required] string[] searchLayerIDs, [FromForm, Required] IEnumerable<IFormFile> files)
        {
            var mc = modelContextBuilder.BuildImmediate();

            var writeLayer = await layerModel.GetLayer(writeLayerID, mc);
            if (writeLayer == null)
                return BadRequest("Invalid write layer ID configured");

            // authorization
            var user = await currentUserService.GetCurrentUser(mc);
            if (!authorizationService.CanUserWriteToLayer(user, writeLayer))
                return Forbid();
            // NOTE: we don't do any ci-based authorization here... its pretty hard to do because of all the temporary CIs
            // TODO: think about this!

            var searchLayers = new LayerSet(searchLayerIDs);

            try
            {
                var fileStreams = files.Select<IFormFile, (Func<Stream> stream, string filename)>(f => (
                   () => f.OpenReadStream(),
                   Path.GetFileName(f.FileName) // stripping path for security reasons: https://docs.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-3.1#upload-small-files-with-buffered-model-binding-to-physical-storage-1
               ));
                var (ciCandidates, relationCandidates) = ingestActiveDirectoryXMLService.Files2IngestCandidates(fileStreams, searchLayers, logger);
                var ingestData = new IngestData(ciCandidates, relationCandidates);
                var issueAccumulator = new IssueAccumulator("DataIngest", $"ActiveDirectoryXMLIngest_{writeLayerID}");
                var changesetProxy = new ChangesetProxy(user.InDatabase, TimeThreshold.BuildLatest(), changesetModel);

                using var transIngest = modelContextBuilder.BuildDeferred();
                var (numAffectedAttributes, numAffectedRelations) = await ingestDataService.Ingest(ingestData, writeLayer, changesetProxy, issueAccumulator, transIngest);
                transIngest.Commit();

                using var transUpdateIssues = modelContextBuilder.BuildDeferred();
                await issuePersister.Persist(issueAccumulator, transUpdateIssues, new DataOriginV1(DataOriginType.InboundIngest), changesetProxy);
                transUpdateIssues.Commit();

                logger.LogInformation($"Active Directory XML Ingest successful; affected {numAffectedAttributes} attributes, {numAffectedRelations} relations");

                return Ok();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Active Directory XML Ingest failed");
                return BadRequest(e);
            }
        }
    }

}
