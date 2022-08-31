using Microsoft.Extensions.Logging;
using OKPluginGenericJSONIngest.Extract;
using OKPluginGenericJSONIngest.Load;
using OKPluginGenericJSONIngest.Transform.JMESPath;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Threading.Tasks;
using Omnikeeper.Base.Authz;

namespace OKPluginGenericJSONIngest
{
    public class GenericJsonIngestService
    {
        private readonly IMetaConfigurationModel metaConfigurationModel;
        private readonly ContextModel contextModel;
        private readonly ILayerModel layerModel;
        private readonly ICurrentUserAccessor currentUserAccessor;
        private readonly IngestDataService ingestDataService;
        private readonly IAuthzFilterManager authzFilterManager;
        private readonly IChangesetModel changesetModel;
        private readonly IIssuePersister issuePersister;

        public GenericJsonIngestService(IMetaConfigurationModel metaConfigurationModel, ContextModel contextModel, 
            ILayerModel layerModel, ICurrentUserAccessor currentUserAccessor, IngestDataService ingestDataService,
            IAuthzFilterManager authzFilterManager, IChangesetModel changesetModel, IIssuePersister issuePersister)
        {
            this.metaConfigurationModel = metaConfigurationModel;
            this.contextModel = contextModel;
            this.layerModel = layerModel;
            this.currentUserAccessor = currentUserAccessor;
            this.ingestDataService = ingestDataService;
            this.authzFilterManager = authzFilterManager;
            this.changesetModel = changesetModel;
            this.issuePersister = issuePersister;
        }

        public async Task Ingest(string contextID, string inputJson, ILogger logger, IIssueAccumulator issueAccumulator, IModelContextBuilder modelContextBuilder)
        {
            var t = new StopTimer();

            using var mc = modelContextBuilder.BuildImmediate();

            var timeThreshold = TimeThreshold.BuildLatest();

            var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(mc);
            var (ctx, _) = await contextModel.GetSingleByDataID(contextID, metaConfiguration.ConfigLayerset, mc, timeThreshold);
            if (ctx == null)
                throw new Exception($"Context with name \"{contextID}\" not found");
            if (ctx.ExtractConfig is not ExtractConfigPassiveRESTFiles f)
                throw new Exception($"Context with name \"{contextID}\" does not accept files via REST API");

            var searchLayers = new LayerSet(ctx.LoadConfig.SearchLayerIDs);
            var writeLayer = await layerModel.GetLayer(ctx.LoadConfig.WriteLayerID, mc);
            if (writeLayer == null)
            {
                throw new Exception($"Cannot write to layer with ID {ctx.LoadConfig.WriteLayerID}: layer does not exist");
            }

            var user = await currentUserAccessor.GetCurrentUser(mc);

            if (await authzFilterManager.ApplyPreFilterForMutation(new PreMutateContextForCIs(), user, searchLayers, writeLayer.ID, mc, timeThreshold) is AuthzFilterResultDeny d)
                throw new UnauthorizedAccessException(d.Reason);

            // NOTE: we don't do any ci-based authorization here... its pretty hard to do because of all the temporary CIs
            // TODO: think about this!

            logger.LogInformation($"Transforming inbound data...");

            GenericInboundData genericInboundData;
            switch (ctx.TransformConfig)
            {
                case TransformConfigJMESPath jmesPathConfig:
                    var transformer = TransformerJMESPath.Build(jmesPathConfig);

                    // NOTE: by just concating the strings together, not actually parsing the JSON at all (at this step)
                    // we safe some performance
                    string genericInboundDataJson;
                    try
                    {
                        genericInboundDataJson = transformer.TransformJSON(inputJson);
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"Error transforming JSON: {e.Message}", e);
                    }

                    try
                    {
                        genericInboundData = transformer.DeserializeJson(genericInboundDataJson);
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"Error deserializing JSON to GenericInboundData: {e.Message}", e);
                    }
                    break;
                default:
                    throw new Exception("Encountered unknown transform config");
            }

            logger.LogInformation($"Done transforming inbound data");

            logger.LogInformation($"Converting to ingest data...");

            var preparer = new Preparer();
            var ingestData = preparer.GenericInboundData2IngestData(genericInboundData, searchLayers, issueAccumulator);

            logger.LogInformation($"Done converting to ingest data");

            logger.LogInformation($"Performing ingest...");

            var changesetProxy = new ChangesetProxy(user.InDatabase, timeThreshold, changesetModel, new DataOriginV1(DataOriginType.InboundIngest));

            using var transIngest = modelContextBuilder.BuildDeferred();
            var (numAffectedAttributes, numAffectedRelations) = await ingestDataService.Ingest(ingestData, writeLayer, changesetProxy, issueAccumulator, transIngest);

            if (await authzFilterManager.ApplyPostFilterForMutation(new PostMutateContextForCIs(), user, searchLayers, changesetProxy.GetActiveChangeset(writeLayer.ID), transIngest, timeThreshold) is AuthzFilterResultDeny dPost)
                throw new UnauthorizedAccessException(dPost.Reason);

            transIngest.Commit();

            using var transUpdateIssues = modelContextBuilder.BuildDeferred();
            await issuePersister.Persist(issueAccumulator, transUpdateIssues, changesetProxy);
            transUpdateIssues.Commit();

            t.Stop((ts, elapsedTime) => logger.LogInformation($"Ingest successful, done in {elapsedTime}; affected {numAffectedAttributes} attributes, {numAffectedRelations} relations"));
        }

        public async Task IngestRaw(GenericInboundData genericInboundData, string[] searchLayerIDs, string writeLayerID, ILogger logger, IIssueAccumulator issueAccumulator, IModelContextBuilder modelContextBuilder)
        {
            var t = new StopTimer();

            using var mc = modelContextBuilder.BuildImmediate();

            var timeThreshold = TimeThreshold.BuildLatest();

            var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(mc);

            var searchLayers = new LayerSet(searchLayerIDs);
            var writeLayer = await layerModel.GetLayer(writeLayerID, mc);
            if (writeLayer == null)
            {
                throw new Exception($"Cannot write to layer with ID {writeLayerID}: layer does not exist");
            }

            var user = await currentUserAccessor.GetCurrentUser(mc);

            // authorization
            if (await authzFilterManager.ApplyPreFilterForMutation(new PreMutateContextForCIs(), user, searchLayers, writeLayer.ID, mc, timeThreshold) is AuthzFilterResultDeny d)
                throw new UnauthorizedAccessException(d.Reason);

            // NOTE: we don't do any ci-based authorization here... its pretty hard to do because of all the temporary CIs
            // TODO: think about this!

            logger.LogInformation($"Converting to ingest data...");

            var preparer = new Preparer();
            var ingestData = preparer.GenericInboundData2IngestData(genericInboundData, searchLayers, issueAccumulator);

            logger.LogInformation($"Done converting to ingest data");

            logger.LogInformation($"Performing ingest...");

            var changesetProxy = new ChangesetProxy(user.InDatabase, timeThreshold, changesetModel, new DataOriginV1(DataOriginType.InboundIngest));


            using var transIngest = modelContextBuilder.BuildDeferred();
            var (numAffectedAttributes, numAffectedRelations) = await ingestDataService.Ingest(ingestData, writeLayer, changesetProxy, issueAccumulator, transIngest);

            if (await authzFilterManager.ApplyPostFilterForMutation(new PostMutateContextForCIs(), user, searchLayers, changesetProxy.GetActiveChangeset(writeLayerID), transIngest, timeThreshold) is AuthzFilterResultDeny dPost)
                throw new UnauthorizedAccessException(dPost.Reason);

            transIngest.Commit();

            using var transUpdateIssues = modelContextBuilder.BuildDeferred();
            await issuePersister.Persist(issueAccumulator, transUpdateIssues, changesetProxy);
            transUpdateIssues.Commit();

            t.Stop((ts, elapsedTime) => logger.LogInformation($"Ingest successful, done in {elapsedTime}; affected {numAffectedAttributes} attributes, {numAffectedRelations} relations"));
        }
    }
}
