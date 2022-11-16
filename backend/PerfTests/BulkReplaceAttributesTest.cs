using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tests.Integration;

namespace PerfTests
{
    [Explicit]
    [IterationCount(10)]
    public class BulkReplaceAttributesTest : Base
    {
        [ParamsSource(nameof(AttributeCITuples))]
        public (int numCIs, int numPrefilledAttributes, int numAttributeInserts, int numAttributeNames) AttributeCITuple { get; set; }
        public IEnumerable<(int numCIs, int numPrefilledAttributes, int numAttributeInserts, int numAttributeNames)> AttributeCITuples => new[] {
            (500, 5000, 5000, 50),
        };

        [Params("Text", "TextArray", "Integer", "IntegerArray", "JSON", "JSONArray")]
        //[Params("Text")]
        public string? AttributeValueType { get; set; }

        [GlobalSetup(Target = nameof(BulkReplaceAttributes))]
        public void GlobalSetup()
        {
            Setup(false, true);
        }
        [GlobalCleanup(Target = nameof(BulkReplaceAttributes))]
        public void GlobalCleanup()
        {
            TearDown();
        }

        [IterationSetup(Target = nameof(BulkReplaceAttributes))]
        public void Setup()
        {
            SetupData().GetAwaiter().GetResult();
        }

        [Benchmark]
        public async Task BulkReplaceAttributes()
        {
            var modelContextBuilder = ServiceProvider.GetRequiredService<IModelContextBuilder>();
            var changesetModel = ServiceProvider.GetRequiredService<IChangesetModel>();
            var attributeModel = ServiceProvider.GetRequiredService<IAttributeModel>();

            using var mc = modelContextBuilder.BuildDeferred();
            await PerformBulkReplace(AttributeCITuple.numAttributeInserts, random, changesetModel, attributeModel, user!, mc);
            mc.Commit();
        }

        private async Task SetupData()
        {
            var modelContextBuilder = ServiceProvider.GetRequiredService<IModelContextBuilder>();
            var userModel = ServiceProvider.GetRequiredService<IUserInDatabaseModel>();
            var changesetModel = ServiceProvider.GetRequiredService<IChangesetModel>();
            var attributeModel = ServiceProvider.GetRequiredService<IAttributeModel>();
            var ciModel = ServiceProvider.GetRequiredService<ICIModel>();
            var layerModel = ServiceProvider.GetRequiredService<ILayerModel>();
            var baseAttributeRevisionistModel = ServiceProvider.GetRequiredService<IBaseAttributeRevisionistModel>();
            user = await DBSetup.SetupUser(userModel, modelContextBuilder.BuildImmediate());

            var ciids = Enumerable.Range(0, AttributeCITuple.numCIs).Select(i =>
            {
                return Guid.NewGuid();
            }).ToList();

            using var mc = modelContextBuilder.BuildDeferred();

            cis = ciids.Select(identity =>
            {
                return ciModel.CreateCI(identity, mc).GetAwaiter().GetResult();
            }).ToList();

            // prepare empty layer
            await layerModel.CreateLayerIfNotExists(layerID, mc);
            var numDeletedAttributes = await baseAttributeRevisionistModel.DeleteAllAttributes(AllCIIDsSelection.Instance, layerID, mc);

            validAttributeNames = Enumerable.Range(0, AttributeCITuple.numAttributeNames).Select(i => "A" + RandomUtility.GenerateRandomString(32, random)).ToList();

            // pre-fill
            await PerformBulkReplace(AttributeCITuple.numPrefilledAttributes, random, changesetModel, attributeModel, user, mc);

            mc.Commit();
        }

        private async Task PerformBulkReplace(int numAttributeInserts, Random random, IChangesetModel changesetModel, IAttributeModel attributeModel, UserInDatabase user, IModelContext mc)
        {
            var changeset = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel, new DataOriginV1(DataOriginType.Manual));

            var usedAttributes = new HashSet<string>();
            var fragments = Enumerable.Range(0, numAttributeInserts).Select(i =>
            {
                var found = false;
                string name = "";
                Guid ciid = new Guid();
                while (!found)
                {
                    name = validAttributeNames!.GetRandom(random)!;
                    ciid = cis!.GetRandom(random);
                    var hash = CIAttribute.CreateInformationHash(name, ciid);
                    if (!usedAttributes.Contains(hash))
                    {
                        usedAttributes.Add(hash);
                        found = true;
                    }
                }

                var value = AttributeValueType switch
                {
                    "Text" => (IAttributeValue)new AttributeScalarValueText("V" + RandomUtility.GenerateRandomString(8, random)),
                    "Integer" => new AttributeScalarValueInteger(random.NextInt64()),
                    "JSON" => jsonScalarValue,
                    "TextArray" => AttributeArrayValueText.BuildFromString(Enumerable.Range(0, 5).Select(i => "V" + RandomUtility.GenerateRandomString(8, random)).ToArray(), false),
                    "IntegerArray" => AttributeArrayValueInteger.Build(Enumerable.Range(0, 5).Select(i => random.NextInt64()).ToArray()),
                    "JSONArray" => jsonArrayValue,
                    _ => throw new NotImplementedException(),
                };
                return new BulkCIAttributeDataCIAndAttributeNameScope.Fragment(ciid, name, value);
            });

            var data = new BulkCIAttributeDataCIAndAttributeNameScope(layerID, fragments, AllCIIDsSelection.Instance, AllAttributeSelection.Instance);
            await attributeModel.BulkReplaceAttributes(data, changeset, mc, MaskHandlingForRemovalApplyNoMask.Instance, OtherLayersValueHandlingForceWrite.Instance);
        }

        private static readonly AttributeScalarValueJSON jsonScalarValue = (AttributeScalarValueJSON.BuildFromString(EXAMPLE_JSON, true) as AttributeScalarValueJSON)!;
        private static readonly AttributeArrayValueJSON jsonArrayValue = (AttributeScalarValueJSON.BuildFromString(EXAMPLE_ARRAY_JSON, true) as AttributeArrayValueJSON)!;
        private List<string>? validAttributeNames = null;
        private List<Guid>? cis = null;
        private UserInDatabase? user = null;
        private readonly Random random = new Random(3);

        private readonly string layerID = "l1";

        private const string EXAMPLE_JSON = @"
          {
            ""_id"": ""6317085e16d25bd4dce7fbcb"",
            ""index"": 0,
            ""guid"": ""bedd1859-0e1b-483f-a92f-b7adcee1110a"",
            ""isActive"": false,
            ""balance"": ""$3,032.66"",
            ""picture"": ""http://placehold.it/32x32"",
            ""age"": 40,
            ""eyeColor"": ""blue"",
            ""name"": ""Pearlie Rhodes"",
            ""gender"": ""female"",
            ""company"": ""OULU"",
            ""email"": ""pearlierhodes@oulu.com"",
            ""phone"": ""+1 (812) 477-2626"",
            ""address"": ""767 Lake Place, Eagletown, District Of Columbia, 4753"",
            ""about"": ""Irure magna occaecat excepteur magna consectetur voluptate qui eu quis ad sit adipisicing. Dolor labore quis mollit aliqua sit adipisicing labore adipisicing. Dolor esse commodo cupidatat amet incididunt ex eiusmod sit laborum id anim est exercitation nisi. Pariatur eu occaecat eu cillum tempor. Consectetur voluptate ex officia commodo deserunt esse sint mollit adipisicing dolor ea est.\r\n"",
            ""registered"": ""2017-02-14T11:38:13 -01:00"",
            ""latitude"": 61.973359,
            ""longitude"": 122.721462,
            ""tags"": [
              ""amet"",
              ""qui"",
              ""ut"",
              ""laborum"",
              ""qui"",
              ""eu"",
              ""in""
            ],
            ""friends"": [
              {
                ""id"": 0,
                ""name"": ""Gilbert Humphrey""
              },
              {
            ""id"": 1,
                ""name"": ""Holland Aguirre""
              },
              {
            ""id"": 2,
                ""name"": ""Taylor Stein""
              }
            ]
            }
        ";

        private const string EXAMPLE_ARRAY_JSON = @"[
          {
            ""_id"": ""6317085e16d25bd4dce7fbcb"",
            ""index"": 0,
            ""guid"": ""bedd1859-0e1b-483f-a92f-b7adcee1110a"",
            ""isActive"": false,
            ""balance"": ""$3,032.66"",
            ""picture"": ""http://placehold.it/32x32"",
            ""age"": 40,
            ""eyeColor"": ""blue"",
            ""name"": ""Pearlie Rhodes"",
            ""gender"": ""female"",
            ""company"": ""OULU"",
            ""email"": ""pearlierhodes@oulu.com"",
            ""phone"": ""+1 (812) 477-2626"",
            ""address"": ""767 Lake Place, Eagletown, District Of Columbia, 4753"",
            ""about"": ""Irure magna occaecat excepteur magna consectetur voluptate qui eu quis ad sit adipisicing. Dolor labore quis mollit aliqua sit adipisicing labore adipisicing. Dolor esse commodo cupidatat amet incididunt ex eiusmod sit laborum id anim est exercitation nisi. Pariatur eu occaecat eu cillum tempor. Consectetur voluptate ex officia commodo deserunt esse sint mollit adipisicing dolor ea est.\r\n"",
            ""registered"": ""2017-02-14T11:38:13 -01:00"",
            ""latitude"": 61.973359,
            ""longitude"": 122.721462,
            ""tags"": [
              ""amet"",
              ""qui"",
              ""ut"",
              ""laborum"",
              ""qui"",
              ""eu"",
              ""in""
            ],
            ""friends"": [
              {
                ""id"": 0,
                ""name"": ""Gilbert Humphrey""
              },
              {
            ""id"": 1,
                ""name"": ""Holland Aguirre""
              },
              {
            ""id"": 2,
                ""name"": ""Taylor Stein""
              }
            ]
            }
        ]";
    }
}
