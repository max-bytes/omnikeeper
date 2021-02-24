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
using OKPluginGenericJSONIngest.Transform;

namespace Omnikeeper.Controllers.Ingest
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/ingest/genericJSON/manage")]
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

        [HttpGet()]
        public async Task<IActionResult> GetContextByName([FromQuery, Required] string name)
        {
            // TODO: authorization

            var context = await contextModel.GetContextByName(name, modelContextBuilder.BuildImmediate());
            if (context != null)
                return Ok(context);
            else
                return NotFound($"Could not find context with name {name}");
        }

        [HttpPost()]
        public async Task<IActionResult> AddContext([FromBody, Required] string name, [FromBody, Required] IExtractConfig extractConfig, [FromBody, Required] ITransformConfig transformConfig, [FromBody, Required] ILoadConfig loadConfig)
        {
            try
            {
                // TODO: authorization

                var context = await contextModel.Upsert(name, extractConfig, transformConfig, loadConfig, modelContextBuilder.BuildImmediate());
                return Ok(context);
            } catch (Exception e)
            {
                return BadRequest(e);
            }
        }

    }
}
