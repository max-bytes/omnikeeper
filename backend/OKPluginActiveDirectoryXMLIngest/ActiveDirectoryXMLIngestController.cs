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
using Omnikeeper.Base.Authz;

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
        private readonly IAuthzFilterManager authzFilterManager;
        private readonly ILogger<ActiveDirectoryXMLIngestController> logger;
        private readonly IIssuePersister issuePersister;
        private readonly IChangesetModel changesetModel;

        public ActiveDirectoryXMLIngestController(IngestDataService ingestDataService, ICurrentUserAccessor currentUserService, ILayerModel layerModel, ActiveDirectoryXMLIngestService ingestActiveDirectoryXMLService,
            IModelContextBuilder modelContextBuilder, IAuthzFilterManager authzFilterManager, ILogger<ActiveDirectoryXMLIngestController> logger,
            IIssuePersister issuePersister, IChangesetModel changesetModel)
        {
            this.ingestDataService = ingestDataService;
            this.currentUserService = currentUserService;
            this.layerModel = layerModel;
            this.ingestActiveDirectoryXMLService = ingestActiveDirectoryXMLService;
            this.modelContextBuilder = modelContextBuilder;
            this.authzFilterManager = authzFilterManager;
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

            var searchLayers = new LayerSet(searchLayerIDs);
            var timeThreshold = TimeThreshold.BuildLatest();

            // authorization
            var user = await currentUserService.GetCurrentUser(mc);
            if (await authzFilterManager.ApplyPreFilterForMutation(new PreMutateContextForCIs(), user, searchLayers, writeLayer.ID, mc, timeThreshold) is AuthzFilterResultDeny d)
                return Forbid(d.Reason);
            // NOTE: we don't do any ci-based authorization here... its pretty hard to do because of all the temporary CIs
            // TODO: think about this!

            try
            {
                var fileStreams = files.Select<IFormFile, (Func<Stream> stream, string filename)>(f => (
                   () => f.OpenReadStream(),
                   Path.GetFileName(f.FileName) // stripping path for security reasons: https://docs.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-3.1#upload-small-files-with-buffered-model-binding-to-physical-storage-1
               ));
                var (ciCandidates, relationCandidates) = ingestActiveDirectoryXMLService.Files2IngestCandidates(fileStreams, searchLayers, logger);
                var ingestData = new IngestData(ciCandidates, relationCandidates);
                var issueAccumulator = new IssueAccumulator("DataIngest", $"ActiveDirectoryXMLIngest_{writeLayerID}");
                var changesetProxy = new ChangesetProxy(user.InDatabase, timeThreshold, changesetModel, new DataOriginV1(DataOriginType.InboundIngest));

                using var transIngest = modelContextBuilder.BuildDeferred();
                var (numAffectedAttributes, numAffectedRelations) = await ingestDataService.Ingest(ingestData, writeLayer, changesetProxy, issueAccumulator, transIngest);

                if (await authzFilterManager.ApplyPostFilterForMutation(new PostMutateContextForCIs(), user, searchLayers, changesetProxy.GetActiveChangeset(writeLayerID), transIngest, timeThreshold) is AuthzFilterResultDeny dPost)
                    return Forbid(dPost.Reason);

                transIngest.Commit();

                using var transUpdateIssues = modelContextBuilder.BuildDeferred();
                await issuePersister.Persist(issueAccumulator, transUpdateIssues, changesetProxy);
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
