using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using OKPluginGenericJSONIngest.Extract;
using Omnikeeper.Base.Model.Config;
using System.Text;

namespace Omnikeeper.Controllers.Ingest
{
    //public class MultiStream : Stream
    //{
    //    private readonly Stream[] subStreams;
    //    private long position;
    //    private int currentStreamIndex;
    //    private Stream currentStream;
    //    private readonly int numSubStreams;

    //    public MultiStream(IEnumerable<Stream> subStreams)
    //    {
    //        this.subStreams = subStreams.ToArray();
    //        this.Length = subStreams.Sum(s => s.Length);
    //        this.position = 0;
    //        this.currentStreamIndex = 0;
    //        this.currentStream = this.subStreams[0];
    //        this.numSubStreams = subStreams.Count();
    //    }

    //    public override bool CanRead => true;

    //    public override bool CanSeek => false;

    //    public override bool CanWrite => false;

    //    public override long Length { get; }

    //    public override long Position
    //    {
    //        get { return position; }
    //        set { throw new NotImplementedException(); }
    //    }

    //    public override void Flush()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override int Read(byte[] buffer, int offset, int count)
    //    {
    //        int result = 0;
    //        while (count > 0)
    //        {
    //            // Read what we can from the current stream
    //            int numBytesRead = currentStream.Read(buffer, offset, count);
    //            count -= numBytesRead;
    //            offset += numBytesRead;
    //            result += numBytesRead;
    //            position += numBytesRead;

    //            // If we haven't satisfied the read request, we have exhausted the child stream.
    //            // Move on to the next stream and loop around to read more data.
    //            if (count > 0)
    //            {
    //                // If we run out of child streams to read from, we're at the end of the HugeStream, and there is no more data to read
    //                if (currentStreamIndex + 1 >= numSubStreams)
    //                    break;

    //                // Otherwise, go to the next substream
    //                currentStream = subStreams[++currentStreamIndex];
    //            }
    //        }

    //        return result;
    //    }

    //    public override long Seek(long offset, SeekOrigin origin)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override void SetLength(long value)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override void Write(byte[] buffer, int offset, int count)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}

    //public class StringStream : Stream
    //{
    //    private readonly MemoryStream _memory;
    //    public StringStream(string text)
    //    {
    //        _memory = new MemoryStream(Encoding.UTF8.GetBytes(text));
    //    }
    //    public StringStream()
    //    {
    //        _memory = new MemoryStream();
    //    }
    //    public StringStream(int capacity)
    //    {
    //        _memory = new MemoryStream(capacity);
    //    }
    //    public override void Flush()
    //    {
    //        _memory.Flush();
    //    }
    //    public override int Read(byte[] buffer, int offset, int count)
    //    {
    //        return _memory.Read(buffer, offset, count);
    //    }
    //    public override long Seek(long offset, SeekOrigin origin)
    //    {
    //        return _memory.Seek(offset, origin);
    //    }
    //    public override void SetLength(long value)
    //    {
    //        _memory.SetLength(value);
    //    }
    //    public override void Write(byte[] buffer, int offset, int count)
    //    {
    //        _memory.Write(buffer, offset, count);
    //    }
    //    public override bool CanRead => _memory.CanRead;
    //    public override bool CanSeek => _memory.CanSeek;
    //    public override bool CanWrite => _memory.CanWrite;
    //    public override long Length => _memory.Length;
    //    public override long Position
    //    {
    //        get => _memory.Position;
    //        set => _memory.Position = value;
    //    }
    //    public override string ToString()
    //    {
    //        return Encoding.UTF8.GetString(_memory.GetBuffer(), 0, (int)_memory.Length);
    //    }
    //    public override int ReadByte()
    //    {
    //        return _memory.ReadByte();
    //    }
    //    public override void WriteByte(byte value)
    //    {
    //        _memory.WriteByte(value);
    //    }
    //}


    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/ingest/genericJSON/files")]
    [Authorize]
    [ApiExplorerSettings(GroupName = "OKPluginGenericJSONIngest")]
    public class PassiveFilesController : ControllerBase
    {
        private readonly IngestDataService ingestDataService;
        private readonly ILayerModel layerModel;
        private readonly ILogger<PassiveFilesController> logger;
        private readonly ICurrentUserAccessor currentUserService;
        private readonly GenericTraitEntityModel<Context, string> contextModel;
        private readonly IModelContextBuilder modelContextBuilder;
        private readonly IMetaConfigurationModel metaConfigurationModel;
        private readonly ILayerBasedAuthorizationService authorizationService;

        public PassiveFilesController(IngestDataService ingestDataService, ILayerModel layerModel, ICurrentUserAccessor currentUserService,
            GenericTraitEntityModel<Context, string> contextModel, IModelContextBuilder modelContextBuilder, IMetaConfigurationModel metaConfigurationModel,
            ILayerBasedAuthorizationService authorizationService, ILogger<PassiveFilesController> logger)
        {
            this.ingestDataService = ingestDataService;
            this.layerModel = layerModel;
            this.logger = logger;
            this.currentUserService = currentUserService;
            this.contextModel = contextModel;
            this.modelContextBuilder = modelContextBuilder;
            this.metaConfigurationModel = metaConfigurationModel;
            this.authorizationService = authorizationService;
        }

        [HttpPost("")]
        [DisableRequestSizeLimit]
        [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = int.MaxValue)]
        public async Task<ActionResult> Ingest([FromQuery, Required]string context, [FromForm, Required] IEnumerable<IFormFile> files)
        {
            try
            {
                using var mc = modelContextBuilder.BuildImmediate();

                var timeThreshold = TimeThreshold.BuildLatest();

                var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(mc);
                var (ctx, _) = await contextModel.GetSingleByDataID(context, metaConfiguration.ConfigLayerset, mc, timeThreshold);
                if (ctx == null)
                    return BadRequest($"Context with name \"{context}\" not found");
                if (!(ctx.ExtractConfig is ExtractConfigPassiveRESTFiles f))
                    return BadRequest($"Context with name \"{context}\" does not accept files via REST API");
                if (files.IsEmpty())
                    return BadRequest($"No files specified");

                var searchLayers = new LayerSet(ctx.LoadConfig.SearchLayerIDs);
                var writeLayer = await layerModel.GetLayer(ctx.LoadConfig.WriteLayerID, mc, timeThreshold);
                if (writeLayer == null)
                {
                    return BadRequest($"Cannot write to layer with ID {ctx.LoadConfig.WriteLayerID}: layer does not exist");
                }

                var user = await currentUserService.GetCurrentUser(mc);

                // authorization
                if (!authorizationService.CanUserWriteToLayer(user, writeLayer))
                {
                    return Forbid();
                }
                // NOTE: we don't do any ci-based authorization here... its pretty hard to do because of all the temporary CIs
                // TODO: think about this!


                // TODO: think about maybe requiring specific file(name)s and making that configurable
                var fileStreams = files.Select<IFormFile, (Stream stream, string filename)>(f => (
                   f.OpenReadStream(),
                   Path.GetFileName(f.FileName) // stripping path for security reasons: https://docs.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-3.1#upload-small-files-with-buffered-model-binding-to-physical-storage-1
               )).ToArray();

                GenericInboundData genericInboundData;
                switch (ctx.TransformConfig)
                {
                    case TransformConfigJMESPath jmesPathConfig:
                        var transformer = TransformerJMESPath.Build(jmesPathConfig);

                        // NOTE: by just concating the strings together, not actually parsing the JSON at all (at this step)
                        // we safe some performance
                        JToken genericInboundDataJson;
                        try
                        {
                            var inputFilesSize = fileStreams.Sum(f => f.stream.Length);
                            using var ms = new MemoryStream((int)(inputFilesSize + fileStreams.Count() * 100 + 10));
                            using var sw = new StreamWriter(ms);
                            sw.Write("[");
                            var numFileStreams = fileStreams.Count();
                            for (int i = 0; i < numFileStreams; i++)
                            {
                                var item = fileStreams[i];
                                var stream = item.stream;
                                sw.Write($"{{\"document\":\"{item.filename}\",\"data\":");
                                sw.Flush();
                                stream.CopyTo(sw.BaseStream);
                                sw.Write($"}}");
                                if (i < numFileStreams - 1)
                                    sw.Write(",");

                                item.stream.Dispose();
                            }
                            sw.Write("]");
                            sw.Flush();

                            ms.Position = 0;

                            // alternative implementation using a "MultiStream"
                            //var subStreams = new List<Stream>();
                            //subStreams.Add(new StringStream("["));
                            //var numFileStreams = fileStreams.Count();
                            //for (int i = 0; i < numFileStreams; i++)
                            //{
                            //    var item = fileStreams[i];
                            //    subStreams.Add(new StringStream($"{{\"document\":\"{item.filename}\",\"data\":"));
                            //    subStreams.Add(item.stream);
                            //    subStreams.Add(new StringStream($"}}"));
                            //    if (i < numFileStreams - 1)
                            //        subStreams.Add(new StringStream(","));
                            //}
                            //subStreams.Add(new StringStream("]"));
                            //using var multiStream = new MultiStream(subStreams);

                            using var sr = new StreamReader(ms, Encoding.UTF8, true, 404800);
                            using var jsonTextReader = new JsonTextReader(sr);

                            var token = await JToken.ReadFromAsync(jsonTextReader);

                            foreach (var stream in fileStreams)
                                stream.stream.Dispose();

                            genericInboundDataJson = transformer.TransformJSON(token);
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

                var preparer = new Preparer();
                var ingestData = preparer.GenericInboundData2IngestData(genericInboundData, searchLayers);

                var (numIngestedCIs, numIngestedRelations) = await ingestDataService.Ingest(ingestData, writeLayer, user);

                logger.LogInformation($"Ingest successful; ingested {numIngestedCIs} CIs, {numIngestedRelations} relations");

                return Ok();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Ingest failed");
                return BadRequest(e);
            }
        }
    }
}
