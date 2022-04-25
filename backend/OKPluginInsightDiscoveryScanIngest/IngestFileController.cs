﻿using Insight.Discovery.InfoClasses;
using Insight.Discovery.Tools;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OKPluginGenericJSONIngest;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OKPluginInsightDiscoveryScanIngest
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/ingest/insight-discovery/file")]
    [Authorize]
    [ApiExplorerSettings(GroupName = "OKPluginInsightDiscoveryIngest")]
    public class IngestFileController : Controller
    {
        private readonly GenericJsonIngestService ingestService;
        private readonly ILoggerFactory loggerFactory;

        public IngestFileController(GenericJsonIngestService ingestService, ILoggerFactory loggerFactory)
        {
            this.ingestService = ingestService;
            this.loggerFactory = loggerFactory;
        }

        // TODO: get from model
        private readonly IDictionary<string, Context> contexts = new Dictionary<string, Context>()
        {
            { "insight_discovery", new Context("insight_discovery", "insight_discovery") },
        };

        [HttpPost("")]
        [DisableRequestSizeLimit]
        [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = int.MaxValue)]
        public async Task<ActionResult> Ingest([FromQuery, Required] string context, [FromForm, Required] IFormFile file)
        {
            var logger = loggerFactory.CreateLogger($"AnsibleIngest_{context}");
            logger.LogInformation($"Starting ingest at context {context}");
            try
            {
                if (!contexts.TryGetValue(context, out var ctx))
                    return BadRequest($"Unknown context {context}");
                await _Ingest(ctx, file, logger);

                return Ok();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Ingest failed");
                return BadRequest(e);
            }
        }

        private async Task _Ingest(Context context, IFormFile file, ILogger logger)
        {
            using var fileStream = file.OpenReadStream();
            using var streamReader = new StreamReader(fileStream);
            
            var xml = await streamReader.ReadToEndAsync();

            if (xml == null)
                throw new Exception("Could not read input file");

            // deserialize XML, serialize to JSON
            // TODO: get log service to not write anything
            LogService.Instance.Initialize("/tmp", "tmp.log", Insight.Discovery.Tools.LogLevel.Error, Insight.Discovery.Tools.LogLevel.Error);
            var hostList = ObjectSerializer.Instance.XMLDeserializeObject<List<HostInfo>>(xml);
            var inputJson = JsonSerializer.Serialize(hostList, new JsonSerializerOptions() { });

            await ingestService.Ingest(context.GenericJsonIngestContextID, inputJson, logger);
        }

        //public class HostInfoConverter : JsonConverter<HostInfo>
        //{
        //    public override HostInfo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        //    {
        //        throw new NotImplementedException();
        //    }

        //    public override void Write(Utf8JsonWriter writer, HostInfo value, JsonSerializerOptions options)
        //    {
        //        writer.WriteStartObject();

        //        writer.WriteString(nameof(FooBar.Bar), value.Bar);
        //        // your condition
        //        writer.WriteString(nameof(FooBar.Foo), value.Foo);

        //        writer.WriteEndObject();
        //    }
        //}
    }
}