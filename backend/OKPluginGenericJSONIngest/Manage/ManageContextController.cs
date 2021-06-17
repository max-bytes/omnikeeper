using DevLab.JmesPath;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OKPluginGenericJSONIngest;
using OKPluginGenericJSONIngest.Transform.JMESPath;
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
        private readonly IModelContextBuilder modelContextBuilder;

        public ManageContextController(IContextModel contextModel, IModelContextBuilder modelContextBuilder)
        {
            this.contextModel = contextModel;
            this.modelContextBuilder = modelContextBuilder;
        }

        [HttpGet()]
        public async Task<IActionResult> GetAllContexts()
        {
            // TODO: authorization

            var contexts = await contextModel.GetAllContexts(modelContextBuilder.BuildImmediate());
            return Ok(contexts);
        }

        [HttpGet("{name}")]
        public async Task<IActionResult> GetContextByName([FromRoute, Required] string name)
        {
            // TODO: authorization

            var context = await contextModel.GetContextByName(name, modelContextBuilder.BuildImmediate());
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
                // validation
                switch (contextCandidate.TransformConfig)
                {
                    case TransformConfigJMESPath jmesPathConfig:
                        var transformer = new TransformerJMESPath();
                        var validationException = transformer.ValidateConfig(jmesPathConfig);
                        if (validationException != null)
                            throw new Exception($"Invalid JMESPath configuration: {validationException.Message}", validationException);
                        break;
                    default:
                        throw new Exception("Invalid transform config");
                }
                // TODO: authorization
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
                // TODO: authorization
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
