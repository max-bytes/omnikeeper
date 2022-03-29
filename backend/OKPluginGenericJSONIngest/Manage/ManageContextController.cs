using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OKPluginGenericJSONIngest;
using OKPluginGenericJSONIngest.Transform.JMESPath;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Omnikeeper.Controllers.Ingest
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/ingest/genericJSON/manage/context")]
    [Authorize]
    [ApiExplorerSettings(GroupName = "OKPluginGenericJSONIngest")]
    public class ManageContextController : ControllerBase
    {
        private readonly ContextModel contextModel;
        private readonly ICurrentUserAccessor currentUserService;
        private readonly IManagementAuthorizationService managementAuthorizationService;
        private readonly IChangesetModel changesetModel;
        private readonly IModelContextBuilder modelContextBuilder;
        private readonly IMetaConfigurationModel metaConfigurationModel;

        public ManageContextController(ContextModel contextModel, ICurrentUserAccessor currentUserService, IManagementAuthorizationService managementAuthorizationService,
            IChangesetModel changesetModel, IModelContextBuilder modelContextBuilder, IMetaConfigurationModel metaConfigurationModel)
        {
            this.contextModel = contextModel;
            this.currentUserService = currentUserService;
            this.managementAuthorizationService = managementAuthorizationService;
            this.changesetModel = changesetModel;
            this.modelContextBuilder = modelContextBuilder;
            this.metaConfigurationModel = metaConfigurationModel;
        }

        [HttpGet()]
        public async Task<ActionResult<IEnumerable<Context>>> GetAllContexts()
        {
            var trans = modelContextBuilder.BuildImmediate();
            var user = await currentUserService.GetCurrentUser(trans);

            var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(trans);
            if (!managementAuthorizationService.CanReadManagement(user, metaConfiguration, out var message))
                return Forbid($"User \"{user.Username}\" does not have permission to read contexts: {message}");

            var contexts = await contextModel.GetAllByDataID(metaConfiguration.ConfigLayerset, trans, TimeThreshold.BuildLatest());
            return Ok(contexts.Values);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Context>> GetContext([FromRoute, Required] string id)
        {
            var trans = modelContextBuilder.BuildImmediate();
            var user = await currentUserService.GetCurrentUser(trans);

            var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(trans);
            if (!managementAuthorizationService.CanReadManagement(user, metaConfiguration, out var message))
                return Forbid($"User \"{user.Username}\" does not have permission to read contexts: {message}");

            var (context, _) = await contextModel.GetSingleByDataID(id, metaConfiguration.ConfigLayerset, trans, TimeThreshold.BuildLatest());
            if (context != null)
                return Ok(context);
            else
                return NotFound($"Could not find context with id {id}");
        }

        [HttpPost()]
        public async Task<ActionResult<Context>> UpsertContext([FromBody, Required] Context contextCandidate)
        {
            try
            {
                var trans = modelContextBuilder.BuildImmediate();
                var user = await currentUserService.GetCurrentUser(trans);

                var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(trans);
                if (!managementAuthorizationService.CanModifyManagement(user, metaConfiguration, out var message))
                    return Forbid($"User \"{user.Username}\" does not have permission to modify contexts: {message}");

                // validation
                switch (contextCandidate.TransformConfig)
                {
                    case TransformConfigJMESPath jmesPathConfig:
                        try
                        {
                            TransformerJMESPath.Build(jmesPathConfig); // if building fails, assume expression could not be parsed
                        }
                        catch (Exception e)
                        {
                            throw new Exception($"Invalid JMESPath configuration: {e.Message}", e);
                        }
                        break;
                    default:
                        throw new Exception("Invalid transform config");
                }
                var changesetProxy = new ChangesetProxy(user.InDatabase, TimeThreshold.BuildLatest(), changesetModel);
                var mc = modelContextBuilder.BuildDeferred();
                var updated = new Context(contextCandidate.ID, contextCandidate.ExtractConfig, contextCandidate.TransformConfig, contextCandidate.LoadConfig);
                var (context, _) = await contextModel.InsertOrUpdate(updated, metaConfiguration.ConfigLayerset, metaConfiguration.ConfigWriteLayer,
                    new Base.Entity.DataOrigin.DataOriginV1(Base.Entity.DataOrigin.DataOriginType.Manual),
                    changesetProxy, mc, MaskHandlingForRemovalApplyNoMask.Instance);
                mc.Commit();
                return Ok(context);
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<bool>> RemoveContext([FromRoute, Required] string id)
        {
            try
            {
                var trans = modelContextBuilder.BuildImmediate();
                var user = await currentUserService.GetCurrentUser(trans);

                var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(trans);
                if (!managementAuthorizationService.CanModifyManagement(user, metaConfiguration, out var message))
                    return Forbid($"User \"{user.Username}\" does not have permission to modify contexts: {message}");

                var changesetProxy = new ChangesetProxy(user.InDatabase, TimeThreshold.BuildLatest(), changesetModel);
                var mc = modelContextBuilder.BuildDeferred();
                var deleted = await contextModel.TryToDelete(id,
                    metaConfiguration.ConfigLayerset, metaConfiguration.ConfigWriteLayer,
                    new Base.Entity.DataOrigin.DataOriginV1(Base.Entity.DataOrigin.DataOriginType.Manual),
                    changesetProxy, mc, MaskHandlingForRemovalApplyNoMask.Instance);
                mc.Commit();
                return Ok(deleted);
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }

    }
}
