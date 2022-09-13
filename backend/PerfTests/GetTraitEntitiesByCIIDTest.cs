using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Linq;
using System.Threading.Tasks;
using Tests.Integration;

namespace PerfTests
{
    [Explicit]
    [IterationCount(50)]
    public class GetTraitEntitiesByCIIDTest : Base
    {
        private string layerID = "layer1";
        private LayerSet layerset;
        private ICIIDSelection? specificCIIDs;
        private ICIIDSelection? allExceptCIIDs;
        private GenericTraitEntityModel<TestTraitEntity, long>? traitEntityModel;
        private readonly Consumer consumer = new Consumer();

        [Params(10000)]
        public int NumTraitEntities { get; set; }

        [Params(10, 0)]
        public int MaxRelatedCIs { get; set; }

        public bool PerformDataSetup { get; set; } = true;

        //[Params("all", "specific", "allExcept")]
        [Params("all")]
        public string? CIIDSelection { get; set; }

        [GlobalSetup(Target = nameof(GetTraitEntitiesByCIID))]
        public async Task Setup()
        {
            Setup(false, PerformDataSetup);

            layerset = new LayerSet(layerID);
            traitEntityModel = ServiceProvider.GetRequiredService<GenericTraitEntityModel<TestTraitEntity, long>>();

            if (PerformDataSetup)
                await SetupData();

            var modelContextBuilder = ServiceProvider.GetRequiredService<IModelContextBuilder>();

            var ciidModel = ServiceProvider.GetRequiredService<ICIIDModel>();
            using var mc = modelContextBuilder!.BuildImmediate();
            var ciids = await ciidModel.GetCIIDs(mc);
            specificCIIDs = SpecificCIIDsSelection.Build(ciids.Take(ciids.Count / 3).ToHashSet());
            allExceptCIIDs = AllCIIDsExceptSelection.Build(ciids.Take(ciids.Count / 3).ToHashSet());
        }

        [Benchmark]
        public async Task GetTraitEntitiesByCIID()
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

            (await traitEntityModel!.GetByCIID(selection, layerset!, mc, TimeThreshold.BuildLatest())).Consume(consumer);
        }

        [Test]
        public void RunBenchmark()
        {
            var summary = BenchmarkRunner.Run<GetMergedAttributesTest>();
        }

        [GlobalCleanup(Target = nameof(GetTraitEntitiesByCIID))]
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

            var changeset = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel, new DataOriginV1(DataOriginType.Manual));

            using var mc = modelContextBuilder.BuildDeferred();

            await layerModel.CreateLayerIfNotExists(layerID, mc);

            var relatedCIIDs = Enumerable.Range(0, NumTraitEntities).Select(i => // NOTE: creating as many related CIs as trait entities is just a heuristic
            {
                return Guid.NewGuid();
            }).ToList();
            await ciModel.BulkCreateCIs(relatedCIIDs, mc);

            var data = Enumerable.Range(0, NumTraitEntities).Select(i =>
            {
                var numRelated = random.Next(0, Math.Min(relatedCIIDs.Count, MaxRelatedCIs + 1));
                var related = RandomUtility.TakeRandom(relatedCIIDs, numRelated, random).ToArray();
                return new TestTraitEntity("foo", i, "message", related);
            }).ToDictionary(t => t.ID);

            await traitEntityModel!.BulkReplace(AllCIIDsSelection.Instance, data, layerset!, layerID, changeset, mc, MaskHandlingForRemovalApplyNoMask.Instance);

            mc.Commit();
        }

        [Test]
        public async Task RunDebuggable()
        {
            NumTraitEntities = 100000;
            MaxRelatedCIs = 10;
            CIIDSelection = "all";
            PerformDataSetup = false;
            await Setup();
            await GetTraitEntitiesByCIID();
            TearDown();
        }

        [TraitEntity("test_trait_entity", TraitOriginType.Core)]
        public class TestTraitEntity : TraitEntity
        {
            [TraitAttribute("type", "type")]
            public string Type = "";

            [TraitAttribute("id", "id")]
            [TraitEntityID]
            public long ID = 0L;

            [TraitAttribute("message", "message")]
            public string Message = "";

            [TraitAttribute("name", "__name", optional: true)]
            public string? Name = null;

            [TraitRelation("affectedCIs", "affects_ci", true)]
            public Guid[] AffectedCIs = Array.Empty<Guid>();

            public TestTraitEntity() { }

            public TestTraitEntity(string type, long iD, string message, Guid[] affectedCIs)
            {
                Type = type;
                ID = iD;
                Message = message;
                Name = $"TestTraitEntity - {type} - {iD}";
                AffectedCIs = affectedCIs;
            }
        }
    }
}
