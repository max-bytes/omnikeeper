using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OKPluginGenericJSONIngest;
using OKPluginGenericJSONIngest.Load;
using OKPluginGenericJSONIngest.Transform.JMESPath;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Omnikeeper.Base.Authz;

namespace OKPluginAnsibleInventoryScanIngest
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/Ingest/AnsibleInventoryScan")]
    [Authorize]
    public class AnsibleInventoryScanIngestController : ControllerBase
    {
        private readonly IngestDataService ingestDataService;
        private readonly ILayerModel layerModel;
        private readonly ILogger<AnsibleInventoryScanIngestController> logger;
        private readonly ICurrentUserAccessor currentUserService;
        private readonly IModelContextBuilder modelContextBuilder;
        private readonly IChangesetModel changesetModel;
        private readonly IIssuePersister issuePersister;
        private readonly IAuthzFilterManager authzFilterManager;

        public AnsibleInventoryScanIngestController(IngestDataService ingestDataService, ILayerModel layerModel, ICurrentUserAccessor currentUserService,
            IModelContextBuilder modelContextBuilder, IChangesetModel changesetModel, IIssuePersister issuePersister,
            IAuthzFilterManager authzFilterManager, ILogger<AnsibleInventoryScanIngestController> logger)
        {
            this.ingestDataService = ingestDataService;
            this.layerModel = layerModel;
            this.logger = logger;
            this.currentUserService = currentUserService;
            this.modelContextBuilder = modelContextBuilder;
            this.changesetModel = changesetModel;
            this.issuePersister = issuePersister;
            this.authzFilterManager = authzFilterManager;
        }

        [HttpPost("")]
        public async Task<ActionResult> IngestAnsibleInventoryScan([FromQuery, Required] string writeLayerID, [FromQuery, Required] string[] searchLayerIDs, [FromBody, Required] AnsibleInventoryScanDTO data)
        {
            try
            {
                using var mc = modelContextBuilder.BuildImmediate();
                var searchLayers = new LayerSet(searchLayerIDs);
                var writeLayer = await layerModel.GetLayer(writeLayerID, mc);
                var user = await currentUserService.GetCurrentUser(mc);

                if (writeLayer == null)
                {
                    return BadRequest($"Cannot write to layer with ID {writeLayerID}: layer does not exist");
                }

                // authorization
                if (await authzFilterManager.ApplyPreFilterForMutation(MutationOperation.MutateCIs, user, searchLayerIDs, writeLayer.ID) is AuthzFilterResultDeny d)
                    return Forbid(d.Reason);
                // NOTE: we don't do any ci-based authorization here... its pretty hard to do because of all the temporary CIs
                // TODO: think about this!

                var transformer = TransformerJMESPath.Build(new TransformConfigJMESPath(AnsibleInventoryScanJMESPathExpression.Expression));

                var documents = new Dictionary<string, string>();
                foreach(var kv in data.SetupFacts)
                    documents.Add("setup_facts_" + kv.Key, kv.Value);
                foreach (var kv in data.YumInstalled)
                    documents.Add("yum_installed_" + kv.Key, kv.Value);
                foreach (var kv in data.YumRepos)
                    documents.Add("yum_repos_" + kv.Key, kv.Value);
                foreach (var kv in data.YumUpdates)
                    documents.Add("yum_updates_" + kv.Key, kv.Value);
                var inputJSON = transformer.Documents2JSON(documents);
                var genericInboundDataJson = transformer.TransformJSON(inputJSON);
                var genericInboundData = transformer.DeserializeJson(genericInboundDataJson);


                //System.IO.File.WriteAllText(Path.Combine(Directory.GetParent(ApplicationEnvironment.ApplicationBasePath).Parent.Parent.Parent.ToString(),
                //    "files", "output_intermediate.json"), JToken.Parse(genericInboundDataJson).ToString(Formatting.Indented));

                var issueAccumulator = new IssueAccumulator("DataIngest", $"AnsibleInventoryScanIngest_{writeLayerID}");

                var preparer = new Preparer();
                var ingestData = preparer.GenericInboundData2IngestData(genericInboundData, searchLayers, issueAccumulator);

                var changesetProxy = new ChangesetProxy(user.InDatabase, TimeThreshold.BuildLatest(), changesetModel);

                using var transIngest = modelContextBuilder.BuildDeferred();
                var (numAffectedAttributes, numAffectedRelations) = await ingestDataService.Ingest(ingestData, writeLayer, changesetProxy, issueAccumulator, transIngest);
                transIngest.Commit();

                using var transUpdateIssues = modelContextBuilder.BuildDeferred();
                await issuePersister.Persist(issueAccumulator, transUpdateIssues, new DataOriginV1(DataOriginType.InboundIngest), changesetProxy);
                transUpdateIssues.Commit();

                logger.LogInformation($"Ansible Ingest successful; affected {numAffectedAttributes} attributes, {numAffectedRelations} relations");

                return Ok();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Ansible Ingest failed");
                return BadRequest(e);
            }
        }
    }
}
