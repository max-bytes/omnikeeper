using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OKPluginGenericJSONIngest;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Controllers.Ingest
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/ingest/genericJSON/files")]
    [Authorize]
    [ApiExplorerSettings(GroupName = "OKPluginGenericJSONIngest")]
    public class PassiveFilesController : ControllerBase
    {
        private readonly ILoggerFactory loggerFactory;
        private readonly GenericJsonIngestService ingestService;
        private readonly IModelContextBuilder modelContextBuilder;

        public PassiveFilesController(ILoggerFactory loggerFactory, GenericJsonIngestService ingestService, IModelContextBuilder modelContextBuilder)
        {
            this.loggerFactory = loggerFactory;
            this.ingestService = ingestService;
            this.modelContextBuilder = modelContextBuilder;
        }

        private string BuildJsonInput(IEnumerable<IFormFile> files)
        {
            // TODO: think about maybe requiring specific file(name)s and making that configurable
            var state = files.Select(f => {
                var filename = Path.GetFileName(f.FileName); // stripping path for security reasons: https://docs.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-3.1#upload-small-files-with-buffered-model-binding-to-physical-storage-1
                return (
                    f,
                    lengthInChars: (int)f.Length, // TODO: it seems like length-of-stream=num-of-characters-in-file... correct all the time?
                    itemStart: $"{{\"document\": \"{filename}\", \"data\": ",
                    itemEnd: $"}}"
                );
            }).ToArray();

            var totalLengthInChars =
                                2 + // array open+close
                                state.Sum(s => s.lengthInChars + s.itemStart.Length + s.itemEnd.Length) + // item itself
                                state.Length - 1; // item separators

            var text = string.Create(totalLengthInChars, state, (charBuffer, fs) =>
            {
                var offset = 0;

                "[".AsSpan().CopyTo(charBuffer.Slice(offset, 1));
                offset += 1;

                for (int i = 0; i < fs.Length; i++)
                {
                    var (formFile, lengthInChars, itemStart, itemEnd) = fs[i];

                    itemStart.AsSpan().CopyTo(charBuffer.Slice(offset, itemStart.Length));
                    offset += itemStart.Length;

                    var subByteBuffer = charBuffer.Slice(offset, lengthInChars);
                    using var reader = new StreamReader(formFile.OpenReadStream()); // StreamReader disposes the underlying stream as well
                    offset += reader.Read(subByteBuffer);

                    itemEnd.AsSpan().CopyTo(charBuffer.Slice(offset, itemEnd.Length));
                    offset += itemEnd.Length;

                    if (i < fs.Length - 1)
                    {
                        ",".AsSpan().CopyTo(charBuffer.Slice(offset, 1));
                        offset += 1;
                    }
                }

                "]".AsSpan().CopyTo(charBuffer.Slice(offset, 1));
                offset += 1;
            });

            return text;
        }

        [HttpPost("")]
        [DisableRequestSizeLimit]
        [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = int.MaxValue)]
        public async Task<ActionResult> Ingest([FromQuery, Required] string context, [FromForm, Required] IEnumerable<IFormFile> files)
        {
            var logger = loggerFactory.CreateLogger($"GenericJSONIngest_{context}");
            logger.LogInformation($"Starting ingest at context {context}");
            try
            {
                if (files.IsEmpty())
                    return BadRequest($"No files specified");

                var inputJson = BuildJsonInput(files);

                var issueAccumulator = new IssueAccumulator("DataIngest", $"GenericJsonIngest_{context}");

                await ingestService.Ingest(context, inputJson, logger, issueAccumulator, modelContextBuilder);

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
