using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Omnikeeper.Base.AttributeValues;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Entity.DTO;
using Omnikeeper.Base.Generator;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class ImportExportLayerController : ControllerBase
    {
        private readonly IBaseAttributeModel baseAttributeModel;
        private readonly IChangesetModel changesetModel;
        private readonly ILayerModel layerModel;
        private readonly ICurrentUserAccessor currentUserService;
        private readonly ICIModel ciModel;
        private readonly IBaseRelationModel baseRelationModel;
        private readonly ILayerBasedAuthorizationService layerBasedAuthorizationService;
        private readonly ICIBasedAuthorizationService ciBasedAuthorizationService;
        private readonly IModelContextBuilder modelContextBuilder;
        private readonly ILayerStatisticsModel layerStatisticsModel;
        private readonly IRelationModel relationModel;

        public ImportExportLayerController(IBaseAttributeModel attributeModel, IChangesetModel changesetModel, ICurrentUserAccessor currentUserService, ICIModel ciModel, IBaseRelationModel baseRelationModel,
            ILayerBasedAuthorizationService layerBasedAuthorizationService, IModelContextBuilder modelContextBuilder, ICIBasedAuthorizationService ciBasedAuthorizationService, ILayerModel layerModel, ILayerStatisticsModel layerStatisticsModel, IRelationModel relationModel)
        {
            this.modelContextBuilder = modelContextBuilder;
            this.changesetModel = changesetModel;
            this.baseAttributeModel = attributeModel;
            this.layerBasedAuthorizationService = layerBasedAuthorizationService;
            this.currentUserService = currentUserService;
            this.ciModel = ciModel;
            this.baseRelationModel = baseRelationModel;
            this.ciBasedAuthorizationService = ciBasedAuthorizationService;
            this.layerModel = layerModel;
            this.layerStatisticsModel = layerStatisticsModel;
            this.relationModel = relationModel;
        }

        public class ExportedLayerDataV1
        {
            public readonly string LayerID;
            //public readonly DateTimeOffset Timestamp; move to "meta" information
            public readonly CIAttributeDTO[] Attributes;
            public readonly RelationDTO[] Relations;

            public ExportedLayerDataV1(string layerID, CIAttributeDTO[] attributes, RelationDTO[] relations)
            {
                LayerID = layerID;
                Attributes = attributes;
                Relations = relations;
            }

            public static MyJSONSerializer<ExportedLayerDataV1> Serializer = new MyJSONSerializer<ExportedLayerDataV1>(new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.None
            });
        }

        [HttpGet("exportLayer")]
        public async Task<ActionResult> ExportLayer([FromQuery, Required] string layerID, [FromQuery] Guid[] ciids)
        {
            // TODO: support for historic data

            var trans = modelContextBuilder.BuildImmediate();
            var user = await currentUserService.GetCurrentUser(trans);

            if (!layerBasedAuthorizationService.CanUserReadFromLayer(user, layerID))
                return Forbid($"User \"{user.Username}\" does not have permission to read from layer with ID {layerID}");

            var timeThreshold = TimeThreshold.BuildLatest();

            ICIIDSelection ciidSelection = new AllCIIDsSelection();
            if (ciids != null && ciids.Length > 0)
                ciidSelection = SpecificCIIDsSelection.Build(ciids);

            var attributesDict = (await baseAttributeModel.GetAttributes(ciidSelection, AllAttributeSelection.Instance, new string[] { layerID }, trans, timeThreshold)).First();
            var attributesDTO = attributesDict
                .Where(kv => ciBasedAuthorizationService.CanReadCI(kv.Key)) // TODO: refactor to use a method that queries all ciids at once, returning those that are readable
                .SelectMany(kv => kv.Value.Values)
                .Where(a => a.ChangesetID != GeneratorV1.StaticChangesetID) // HACK: skip generated attributes
                .Select(a => CIAttributeDTO.Build(a));

            var relations = (await baseRelationModel.GetRelations(RelationSelectionAll.Instance, new string[] { layerID }, trans, timeThreshold))[0];

            // TODO: because there is no proper "RelationSelectionFromAndToInList", we fetch all and select manually afterwards
            if (ciidSelection is SpecificCIIDsSelection specificCIIDsSelection)
            {
                relations = relations.Where(r => specificCIIDsSelection.Contains(r.FromCIID) && specificCIIDsSelection.Contains(r.ToCIID));
            }

            var relationsDTO = relations.Select(r => RelationDTO.BuildFromRelation(r));
            // TODO: ci authorization?

            var data = new ExportedLayerDataV1(layerID, attributesDTO.ToArray(), relationsDTO.ToArray());

            using var fs = new MemoryStream();
            using var textWriter = new StreamWriter(fs);
            ExportedLayerDataV1.Serializer.SerializeToTextWriter(data, textWriter);
            textWriter.Close(); // required for complete flushing
            var byteData = fs.ToArray();

            var zipFile = GetZipArchive(new List<InMemoryFile>() {
                new InMemoryFile("data.json", byteData)
            });

            // NOTE: this should be a safe filename in all circumstances, because layerID must be a valid layer-ID
            var filename = $"{layerID}-{timeThreshold.Time:yyyyMMddHHmmss}.okl1";
            return File(zipFile, "application/octet-stream", filename);
        }

        [HttpPost("importLayer")]
        [DisableRequestSizeLimit]
        [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = int.MaxValue)]
        public async Task<ActionResult> ImportLayer([FromForm, Required] IEnumerable<IFormFile> files, [FromQuery] string? overwriteLayerID = null)
        {
            try
            {
                if (files.IsEmpty())
                    return BadRequest($"No files specified");

                var fileStreams = files.Select<IFormFile, (Func<Stream> stream, string filename)>(f => (
                       () => f.OpenReadStream(),
                       Path.GetFileName(f.FileName) // stripping path for security reasons: https://docs.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-3.1#upload-small-files-with-buffered-model-binding-to-physical-storage-1
                   ));

                using var trans = modelContextBuilder.BuildDeferred();

                var user = await currentUserService.GetCurrentUser(trans);

                var timeThreshold = TimeThreshold.BuildLatest();

                var changesetProxy = new ChangesetProxy(user.InDatabase, timeThreshold, changesetModel);

                foreach (var (streamF, filename) in fileStreams)
                {
                    using var stream = streamF();
                    using var archive = new ZipArchive(stream, ZipArchiveMode.Read, false);

                    var dataFile = archive.GetEntry("data.json");
                    if (dataFile == null)
                        return BadRequest($"Invalid archive detected: no data.json found inside archive");

                    var dataStream = dataFile.Open();

                    var data = ExportedLayerDataV1.Serializer.Deserialize(dataStream);

                    var writeLayerID = overwriteLayerID ?? data.LayerID;

                    var writeLayer = await layerModel.GetLayer(writeLayerID, trans);
                    if (writeLayer == null)
                    {
                        return BadRequest($"Cannot write to layer with ID {data.LayerID}: layer does not exist");
                    }
                    // authorization
                    if (!layerBasedAuthorizationService.CanUserWriteToLayer(user, writeLayer))
                    {
                        return Forbid();
                    }

                    // check if layer is empty, if not -> error
                    if (!await layerStatisticsModel.IsLayerEmpty(writeLayer.ID, trans))
                    {
                        return BadRequest($"Cannot write to layer with ID {data.LayerID}: layer is not empty; consider truncating layer before import");
                    }

                    // layer import works as follows:
                    // timestamp, changeset, user, data-origin, state, attribute- and relation-id is different
                    // ciid and other data stays as it was exported

                    var cisToImport = data.Attributes.Select(ci => ci.CIID).Union(data.Relations.SelectMany(r => new Guid[] { r.FromCIID, r.ToCIID })).ToHashSet();
                    var existingCIIDs = await ciModel.GetCIIDs(trans);
                    var cisToCreate = cisToImport.Except(existingCIIDs);
                    await ciModel.BulkCreateCIs(cisToCreate, trans);

                    var maskHandling = MaskHandlingForRemovalApplyNoMask.Instance;

                    var attributeFragments = data.Attributes.Select(t => new BulkCIAttributeDataLayerScope.Fragment(t.Name, AttributeValueHelper.BuildFromDTO(t.Value), t.CIID));
                    var bulkUpdates = await baseAttributeModel.PrepareForBulkUpdate(new BulkCIAttributeDataLayerScope("", writeLayer.ID, attributeFragments), trans, maskHandling);
                    await baseAttributeModel.BulkUpdate(bulkUpdates.inserts, bulkUpdates.removes, writeLayer.ID, new DataOriginV1(DataOriginType.Manual), changesetProxy, trans);

                    var relationFragments = data.Relations.Select(t => new BulkRelationDataLayerScope.Fragment(t.FromCIID, t.ToCIID, t.PredicateID, t.Mask));
                    await baseRelationModel.BulkReplaceRelations(new BulkRelationDataLayerScope(writeLayer.ID, relationFragments), changesetProxy, new DataOriginV1(DataOriginType.Manual), trans, maskHandling);
                }

                trans.Commit();

                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }

        private byte[] GetZipArchive(List<InMemoryFile> files)
        {
            byte[] archiveFile;
            using (var archiveStream = new MemoryStream())
            {
                // NOTE: apparently we need to keep leaveOpen = true, see https://stackoverflow.com/questions/50720298/creating-zip-archive-in-memory-and-returning-it-from-a-web-api
                using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create, leaveOpen: true))
                {
                    foreach (var file in files)
                    {
                        var zipArchiveEntry = archive.CreateEntry(file.FileName, CompressionLevel.Fastest);
                        using var zipStream = zipArchiveEntry.Open();
                        zipStream.Write(file.Content, 0, file.Content.Length);
                    }
                }

                archiveFile = archiveStream.ToArray();
            }

            return archiveFile;
        }

        public class InMemoryFile
        {
            public InMemoryFile(string fileName, byte[] content)
            {
                FileName = fileName;
                Content = content;
            }

            public string FileName { get; }
            public byte[] Content { get; }
        }
    }
}
