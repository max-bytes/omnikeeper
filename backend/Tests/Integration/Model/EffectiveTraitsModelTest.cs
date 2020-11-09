using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.Model;
using Omnikeeper.Utils;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;
using Tests.Integration.Model.Mocks;
using Omnikeeper.Base.Inbound;
using Moq;
using FluentAssertions;

namespace Tests.Integration.Model
{
    partial class EffectiveTraitsModelTest
    {
        [SetUp]
        public void Setup()
        {
            DBSetup.Setup();
        }

        [Test]
        public async Task TestTraitAttributes()
        {
            var dbcb = new DBConnectionBuilder();
            using var conn = dbcb.Build(DBSetup.dbName, false, true);
            var traitsProvider = new MockedTraitsProvider();
            var (traitModel, layerset, ciids) = await BaseSetup(traitsProvider, conn);

            var timeThreshold = TimeThreshold.BuildLatest();

            // TODO: move test for TraitsProvider to its own test-class
            var invalidTrait = await traitsProvider.GetActiveTrait("invalid_trait", null, timeThreshold);
            Assert.AreEqual(null, invalidTrait);

            var testTrait1 = await traitsProvider.GetActiveTrait("test_trait_1", null, timeThreshold);
            var testTrait2 = await traitsProvider.GetActiveTrait("test_trait_2", null, timeThreshold);
            var testTrait3 = await traitsProvider.GetActiveTrait("test_trait_3", null, timeThreshold);

            var et1 = await traitModel.CalculateEffectiveTraitsForTrait(testTrait1, layerset, null, timeThreshold);
            Assert.AreEqual(3, et1.Count());
            var et2 = await traitModel.CalculateEffectiveTraitsForTrait(testTrait2, layerset, null, timeThreshold);
            Assert.AreEqual(2, et2.Count());
            Assert.IsTrue(et2.All(t => t.Value.et.TraitAttributes.Any(ta => ta.Value.Attribute.Name == "a2") && t.Value.et.TraitAttributes.Any(ta => ta.Value.Attribute.Name == "a4")));
            var et3 = await traitModel.CalculateEffectiveTraitsForTrait(testTrait3, layerset, null, timeThreshold);
            Assert.AreEqual(2, et3.Count());
            Assert.IsTrue(et3.All(t => t.Value.et.TraitAttributes.Any(ta => ta.Value.Attribute.Name == "a1")));

            var cis1 = await traitModel.GetMergedCIsWithTrait(testTrait1, layerset, null, timeThreshold);
            Assert.AreEqual(3, cis1.Count());
            cis1.Select(c => c.ID).Should().BeEquivalentTo(new Guid[] { ciids[0], ciids[1], ciids[2] });

            var cis2 = await traitModel.GetMergedCIsWithTrait(testTrait2, layerset, null, timeThreshold);
            Assert.AreEqual(2, cis2.Count());
            cis2.Select(c => c.ID).Should().BeEquivalentTo(new Guid[] { ciids[0], ciids[2] });
        }

        [Test]
        public async Task TestDependentTraits()
        {
            var dbcb = new DBConnectionBuilder();
            using var conn = dbcb.Build(DBSetup.dbName, false, true);
            var traitsProvider = new MockedTraitsProvider();
            var (traitModel, layerset, _) = await BaseSetup(traitsProvider, conn);

            var timeThreshold = TimeThreshold.BuildLatest();

            var testTrait4 = await traitsProvider.GetActiveTrait("test_trait_4", null, timeThreshold);
            var testTrait5 = await traitsProvider.GetActiveTrait("test_trait_5", null, timeThreshold);

            var t1 = await traitModel.CalculateEffectiveTraitsForTrait(testTrait4, layerset, null, timeThreshold);
            Assert.AreEqual(2, t1.Count());
            var t2 = await traitModel.CalculateEffectiveTraitsForTrait(testTrait5, layerset, null, timeThreshold);
            Assert.AreEqual(1, t2.Count());
        }

        [Test]
        public async Task TestDependentTraitLoop()
        {
            var timeThreshold = TimeThreshold.BuildLatest();
            var dbcb = new DBConnectionBuilder();
            using var conn = dbcb.Build(DBSetup.dbName, false, true);
            var traitsProvider = new MockedTraitsProviderWithLoop();
            var (traitModel, layerset, _) = await BaseSetup(traitsProvider, conn);
            var testTrait1 = await traitsProvider.GetActiveTrait("test_trait_1", null, timeThreshold);
            var t1 = await traitModel.CalculateEffectiveTraitsForTrait(testTrait1, layerset, null, timeThreshold);
            Assert.AreEqual(0, t1.Count());
        }

        private async Task<(EffectiveTraitModel traitModel, LayerSet layerset, Guid[])> BaseSetup(ITraitsProvider traitsProvider, NpgsqlConnection conn)
        {
            var oap = new Mock<IOnlineAccessProxy>();
            oap.Setup(_ => _.IsOnlineInboundLayer(It.IsAny<long>(), It.IsAny<NpgsqlTransaction>())).ReturnsAsync(false);
            var attributeModel = new AttributeModel(new BaseAttributeModel(conn));
            var ciModel = new CIModel(attributeModel, conn);
            var userModel = new UserInDatabaseModel(conn);
            var changesetModel = new ChangesetModel(userModel, conn);
            var predicateModel = new PredicateModel(conn);
            var relationModel = new RelationModel(new BaseRelationModel(predicateModel, conn));
            var layerModel = new LayerModel(conn);
            var traitModel = new EffectiveTraitModel(ciModel, relationModel, traitsProvider, oap.Object, NullLogger<EffectiveTraitModel>.Instance, conn);
            var user = await DBSetup.SetupUser(userModel);
            var ciid1 = await ciModel.CreateCI(null);
            var ciid2 = await ciModel.CreateCI(null);
            var ciid3 = await ciModel.CreateCI(null);
            var layer1 = await layerModel.CreateLayer("l1", null);

            using (var trans = conn.BeginTransaction())
            {
                var changeset = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);
                await attributeModel.InsertAttribute("a1", AttributeScalarValueText.BuildFromString("text1"), ciid1, layer1.ID, changeset, trans);
                await attributeModel.InsertAttribute("a2", AttributeScalarValueText.BuildFromString("text2"), ciid1, layer1.ID, changeset, trans);
                await attributeModel.InsertAttribute("a3", AttributeScalarValueText.BuildFromString("text3"), ciid1, layer1.ID, changeset, trans);
                await attributeModel.InsertAttribute("a4", AttributeScalarValueText.BuildFromString("text4"), ciid1, layer1.ID, changeset, trans);

                await attributeModel.InsertAttribute("a1", AttributeScalarValueText.BuildFromString("text1"), ciid2, layer1.ID, changeset, trans);
                await attributeModel.InsertAttribute("a4", AttributeScalarValueText.BuildFromString("text4"), ciid2, layer1.ID, changeset, trans);

                await attributeModel.InsertAttribute("a2", AttributeScalarValueText.BuildFromString("text2"), ciid3, layer1.ID, changeset, trans);
                await attributeModel.InsertAttribute("a3", AttributeScalarValueText.BuildFromString("text3"), ciid3, layer1.ID, changeset, trans);
                await attributeModel.InsertAttribute("a4", AttributeScalarValueText.BuildFromString("text4"), ciid3, layer1.ID, changeset, trans);

                trans.Commit();
            }

            var layerset = await layerModel.BuildLayerSet(new string[] { "l1" }, null);
            return (traitModel, layerset, new Guid[] { ciid1, ciid2, ciid3 });
        }
    }
}
