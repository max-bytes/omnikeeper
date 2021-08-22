using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OKPluginGenericJSONIngest;
using OKPluginGenericJSONIngest.Transform.JMESPath;
using Omnikeeper.Base.Model;
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
        private readonly IContextModel contextModel;
        private readonly IContextWriteService contextWriteService;
        private readonly ICurrentUserService currentUserService;
        private readonly IManagementAuthorizationService managementAuthorizationService;
        private readonly IChangesetModel changesetModel;
        private readonly IModelContextBuilder modelContextBuilder;

        public ManageContextController(IContextModel contextModel, IContextWriteService contextWriteService, ICurrentUserService currentUserService, IManagementAuthorizationService managementAuthorizationService,
            IChangesetModel changesetModel, IModelContextBuilder modelContextBuilder)
        {
            this.contextModel = contextModel;
            this.contextWriteService = contextWriteService;
            this.currentUserService = currentUserService;
            this.managementAuthorizationService = managementAuthorizationService;
            this.changesetModel = changesetModel;
            this.modelContextBuilder = modelContextBuilder;
        }

        [HttpGet()]
        public async Task<ActionResult<IEnumerable<Context>>> GetAllContexts()
        {
            var trans = modelContextBuilder.BuildImmediate();
            var user = await currentUserService.GetCurrentUser(trans);

            if (!managementAuthorizationService.HasManagementPermission(user))
                return Forbid($"User \"{user.Username}\" does not have permission to access management");

            var contexts = await contextModel.GetContexts(TimeThreshold.BuildLatest(), trans);
            return Ok(contexts.Values);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Context>> GetContext([FromRoute, Required] string id)
        {
            var trans = modelContextBuilder.BuildImmediate();
            var user = await currentUserService.GetCurrentUser(trans);

            if (!managementAuthorizationService.HasManagementPermission(user))
                return Forbid($"User \"{user.Username}\" does not have permission to access management");

            var context = await contextModel.GetContext(id, TimeThreshold.BuildLatest(), trans);
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

                if (!managementAuthorizationService.HasManagementPermission(user))
                    return Forbid($"User \"{user.Username}\" does not have permission to access management");

                // validation
                switch (contextCandidate.TransformConfig)
                {
                    case TransformConfigJMESPath jmesPathConfig:
                        try
                        {
                            TransformerJMESPath.Build(jmesPathConfig); // if building fails, assume expression could not be parsed
                        } catch (Exception e)
                        {
                            throw new Exception($"Invalid JMESPath configuration: {e.Message}", e);
                        }
                        break;
                    default:
                        throw new Exception("Invalid transform config");
                }
                var changesetProxy = new ChangesetProxy(user.InDatabase, TimeThreshold.BuildLatest(), changesetModel);
                var mc = modelContextBuilder.BuildDeferred();
                var (context, _) = await contextWriteService.Upsert(contextCandidate.ID, 
                    contextCandidate.ExtractConfig, contextCandidate.TransformConfig, contextCandidate.LoadConfig, 
                    new Base.Entity.DataOrigin.DataOriginV1(Base.Entity.DataOrigin.DataOriginType.Manual), 
                    changesetProxy, user, mc);
                mc.Commit();
                return Ok(context);
            } catch (Exception e)
            {
                return BadRequest(e);
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<Context>> RemoveContext([FromRoute, Required] string id)
        {
            try
            {
                var trans = modelContextBuilder.BuildImmediate();
                var user = await currentUserService.GetCurrentUser(trans);

                if (!managementAuthorizationService.HasManagementPermission(user))
                    return Forbid($"User \"{user.Username}\" does not have permission to access management");

                var changesetProxy = new ChangesetProxy(user.InDatabase, TimeThreshold.BuildLatest(), changesetModel);
                var mc = modelContextBuilder.BuildDeferred();
                var context = await contextWriteService.Delete(id, 
                    new Base.Entity.DataOrigin.DataOriginV1(Base.Entity.DataOrigin.DataOriginType.Manual), 
                    changesetProxy, user,  mc);
                mc.Commit();
                return Ok(context);
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }

    }
}
