using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OKPluginGenericJSONIngest;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Omnikeeper.Controllers.Ingest
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/ingest/genericJSON/data")]
    [Authorize]
    [ApiExplorerSettings(GroupName = "OKPluginGenericJSONIngest")]
    public class PassiveDataController : ControllerBase
    {
        private readonly ILoggerFactory loggerFactory;
        private readonly GenericJsonIngestService ingestService;
        private readonly IModelContextBuilder modelContextBuilder;

        public PassiveDataController(ILoggerFactory loggerFactory, GenericJsonIngestService ingestService, IModelContextBuilder modelContextBuilder)
        {
            this.loggerFactory = loggerFactory;
            this.ingestService = ingestService;
            this.modelContextBuilder = modelContextBuilder;
        }

        [HttpPost("")]
        [DisableRequestSizeLimit]
        [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = int.MaxValue)]
        public async Task<ActionResult> Ingest([FromQuery, Required] string[] readLayerIDs, [FromQuery, Required] string writeLayerID, [FromBody, Required] GenericInboundData data)
        {
            var logger = loggerFactory.CreateLogger($"RawJSONIngest_{string.Join("-", readLayerIDs)}_{writeLayerID}");
            logger.LogInformation($"Starting ingest");
            try
            {
                var issueAccumulator = new IssueAccumulator("DataIngest", $"RawJsonIngest_{string.Join("-", readLayerIDs)}_{writeLayerID}");

                await ingestService.IngestRaw(data, readLayerIDs, writeLayerID, logger, issueAccumulator, modelContextBuilder);

                return Ok();
            }
            catch (UnauthorizedAccessException e)
            {
                logger.LogError(e, "Ingest failed");
                return Forbid();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Ingest failed");
                return BadRequest(e);
            }
        }
    }
}
