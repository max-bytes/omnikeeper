using DevLab.JmesPath;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OKPluginGenericJSONIngest;
using OKPluginGenericJSONIngest.Transform.JMESPath;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils.ModelContext;
using System;
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
        private readonly ICurrentUserService currentUserService;
        private readonly IManagementAuthorizationService managementAuthorizationService;
        private readonly IModelContextBuilder modelContextBuilder;

        public ManageContextController(IContextModel contextModel, ICurrentUserService currentUserService, IManagementAuthorizationService managementAuthorizationService,
            IModelContextBuilder modelContextBuilder)
        {
            this.contextModel = contextModel;
            this.currentUserService = currentUserService;
            this.managementAuthorizationService = managementAuthorizationService;
            this.modelContextBuilder = modelContextBuilder;
        }

        [HttpGet()]
        public async Task<IActionResult> GetAllContexts()
        {
            var trans = modelContextBuilder.BuildImmediate();
            var user = await currentUserService.GetCurrentUser(trans);

            if (!managementAuthorizationService.HasManagementPermission(user))
                return Forbid($"User \"{user.Username}\" does not have permission to access management");

            var contexts = await contextModel.GetAllContexts(trans);
            return Ok(contexts);
        }

        [HttpGet("{name}")]
        public async Task<IActionResult> GetContextByName([FromRoute, Required] string name)
        {
            var trans = modelContextBuilder.BuildImmediate();
            var user = await currentUserService.GetCurrentUser(trans);

            if (!managementAuthorizationService.HasManagementPermission(user))
                return Forbid($"User \"{user.Username}\" does not have permission to access management");

            var context = await contextModel.GetContextByName(name, trans);
            if (context != null)
                return Ok(context);
            else
                return NotFound($"Could not find context with name {name}");
        }

        [HttpPost()]
        public async Task<IActionResult> AddNewContext([FromBody, Required] Context contextCandidate)
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
                var mc = modelContextBuilder.BuildDeferred();
                var context = await contextModel.Upsert(contextCandidate.Name, contextCandidate.ExtractConfig, 
                    contextCandidate.TransformConfig, contextCandidate.LoadConfig, mc);
                mc.Commit();
                return Ok(context);
            } catch (Exception e)
            {
                return BadRequest(e);
            }
        }

        [HttpDelete("{name}")]
        public async Task<IActionResult> RemoveContext([FromRoute, Required] string name)
        {
            try
            {
                var trans = modelContextBuilder.BuildImmediate();
                var user = await currentUserService.GetCurrentUser(trans);

                if (!managementAuthorizationService.HasManagementPermission(user))
                    return Forbid($"User \"{user.Username}\" does not have permission to access management");

                var mc = modelContextBuilder.BuildDeferred();
                var context = await contextModel.Delete(name, mc);
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
