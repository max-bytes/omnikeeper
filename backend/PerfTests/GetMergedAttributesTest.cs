using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
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
    [IterationCount(50)]
    public class GetMergedAttributesTest : Base
    {
        private IAttributeModel? attributeModel;
        private LayerSet? layerset;
        private ICIIDSelection? specificCIIDs;
        private ICIIDSelection? allExceptCIIDs;
        private readonly Consumer consumer = new Consumer();

        [ParamsSource(nameof(AttributeCITuples))]
        public (int numCIs, int numAttributeInserts, int numLayers, int numAttributeNames) AttributeCITuple { get; set; }
        public IEnumerable<(int numCIs, int numAttributeInserts, int numLayers, int numAttributeNames)> AttributeCITuples => new[] {
            (1000, 10000, 4, 50),
        };

        //[Params("all", "specific", "allExcept")]
        [Params("all")]
        public string? CIIDSelection { get; set; }

        //[Params("Text", "TextArray", "Integer", "IntegerArray", "JSON", "JSONArray")]
        [Params("Text")]
        public string? AttributeValueType { get; set; }

        [GlobalSetup(Target = nameof(GetMergedAttributes))]
        public async Task Setup()
        {
            Setup(false, true);

            await SetupData();

            var modelContextBuilder = ServiceProvider.GetRequiredService<IModelContextBuilder>();
            var layerModel = ServiceProvider.GetRequiredService<ILayerModel>();
            attributeModel = ServiceProvider.GetRequiredService<IAttributeModel>();
            using var mc = modelContextBuilder!.BuildImmediate();
            var layers = await layerModel.GetLayers(mc);
            layerset = layerModel!.BuildLayerSet(layers.Select(l => l.ID).ToArray(), mc).GetAwaiter().GetResult();

            var ciidModel = ServiceProvider.GetRequiredService<ICIIDModel>();
            var ciids = await ciidModel.GetCIIDs(mc);
            specificCIIDs = SpecificCIIDsSelection.Build(ciids.Take(ciids.Count / 3).ToHashSet());
            allExceptCIIDs = AllCIIDsExceptSelection.Build(ciids.Take(ciids.Count / 3).ToHashSet());
        }

        [Benchmark]
        public async Task GetMergedAttributes()
        {
            var modelContextBuilder = ServiceProvider.GetRequiredService<IModelContextBuilder>();
            using var mc = modelContextBuilder.BuildImmediate();

            var selection = CIIDSelection switch
            {
                "all" => AllCIIDsSelection.Instance,
                "specific" => specificCIIDs!,
                "allExcept" => allExceptCIIDs!,
                _ => throw new Exception() // must not happen
            };

            (await attributeModel!.GetMergedAttributes(selection, AllAttributeSelection.Instance, layerset!, mc, TimeThreshold.BuildLatest(), GeneratedDataHandlingInclude.Instance)).Consume(consumer);
        }

        [Test]
        public void RunBenchmark()
        {
            var config = new ManualConfig()
                .WithOptions(ConfigOptions.DisableOptimizationsValidator)
                .AddValidator(JitOptimizationsValidator.DontFailOnError)
                .AddLogger(ConsoleLogger.Default)
                .AddColumnProvider(DefaultColumnProviders.Instance);
            var summary = BenchmarkRunner.Run<GetMergedAttributesTest>(config);
        }


        [Test]
        public async Task RunDebuggable()
        {
            CIIDSelection = "specific";
            AttributeCITuple = AttributeCITuples.First();
            await Setup();
            await GetMergedAttributes();
            TearDown();
        }

        [GlobalCleanup(Target = nameof(GetMergedAttributes))]
        public void TearDownT() => TearDown();

        private async Task SetupData()
        {
            var changesetModel = ServiceProvider.GetRequiredService<IChangesetModel>();
            var modelContextBuilder = ServiceProvider.GetRequiredService<IModelContextBuilder>();
            var layerModel = ServiceProvider.GetRequiredService<ILayerModel>();
            var attributeModel = ServiceProvider.GetRequiredService<IAttributeModel>();
            var ciModel = ServiceProvider.GetRequiredService<ICIModel>();
            var userModel = ServiceProvider.GetRequiredService<IUserInDatabaseModel>();
            var user = await DBSetup.SetupUser(userModel, modelContextBuilder.BuildImmediate());

            var random = new Random(3);

            var ciids = Enumerable.Range(0, AttributeCITuple.numCIs).Select(i =>
            {
                return Guid.NewGuid();
            }).ToList();

            var layerNames = Enumerable.Range(0, AttributeCITuple.numLayers).Select(i =>
            {
                var identity = "l_" + RandomUtility.GenerateRandomString(8, random, "abcdefghijklmnopqrstuvwxy");
                return identity;
            }).ToList();

            var changeset = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel, new DataOriginV1(DataOriginType.Manual));

            using var mc = modelContextBuilder.BuildDeferred();
            var cis = ciids.Select(identity =>
            {
                return (ciModel.CreateCI(identity, mc).GetAwaiter().GetResult(), identity);
            }).ToList();

            var layers = layerNames.Select(identity =>
            {
                return (layerModel.CreateLayerIfNotExists(identity, mc).GetAwaiter().GetResult()).layer;
            }).ToList();

            var attributeNames = Enumerable.Range(0, AttributeCITuple.numAttributeNames).Select(i => "A" + RandomUtility.GenerateRandomString(32, random)).ToList();

            foreach (var layer in layers)
            {
                var usedAttributes = new HashSet<string>();
                var fragments = Enumerable.Range(0, AttributeCITuple.numAttributeInserts).Select(i =>
                {
                    var found = false;
                    string name = "";
                    Guid ciid = new Guid();
                    while (!found)
                    {
                        name = attributeNames.GetRandom(random)!;
                        ciid = cis.GetRandom(random).Item1;
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

                var data = new BulkCIAttributeDataCIAndAttributeNameScope(layer!.ID, fragments, AllCIIDsSelection.Instance, AllAttributeSelection.Instance);

                await attributeModel.BulkReplaceAttributes(data, changeset, mc, MaskHandlingForRemovalApplyNoMask.Instance, OtherLayersValueHandlingForceWrite.Instance);
            }

            mc.Commit();
        }

        private static readonly AttributeScalarValueJSON jsonScalarValue = (AttributeScalarValueJSON.BuildFromString(EXAMPLE_JSON, true) as AttributeScalarValueJSON)!;
        private static readonly AttributeArrayValueJSON jsonArrayValue = (AttributeScalarValueJSON.BuildFromString(EXAMPLE_ARRAY_JSON, true) as AttributeArrayValueJSON)!;

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
