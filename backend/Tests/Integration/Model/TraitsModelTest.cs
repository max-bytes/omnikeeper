using Landscape.Base.Entity;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using LandscapeRegistry.Entity.AttributeValues;
using LandscapeRegistry.Model;
using LandscapeRegistry.Utils;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;
using Tests.Integration.Model.Mocks;

namespace Tests.Integration.Model
{
    partial class TraitsModelTest
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
            var (traitModel, layerset) = await BaseSetup(new MockedTraitsProvider(), conn);

            var timeThreshold = TimeThreshold.BuildLatest();

            var t0 = await traitModel.CalculateEffectiveTraitSetsForTraitName("invalid_trait", layerset, null, timeThreshold);
            Assert.AreEqual(null, t0);

            var t1 = await traitModel.CalculateEffectiveTraitSetsForTraitName("test_trait_1", layerset, null, timeThreshold);
            Assert.AreEqual(3, t1.Count());
            var t2 = await traitModel.CalculateEffectiveTraitSetsForTraitName("test_trait_2", layerset, null, timeThreshold);
            Assert.AreEqual(2, t2.Count());
            Assert.IsTrue(t2.All(t => t.EffectiveTraits["test_trait_2"].TraitAttributes.Any(ta => ta.Value.Attribute.Name == "a2") && t.EffectiveTraits["test_trait_2"].TraitAttributes.Any(ta => ta.Value.Attribute.Name == "a4")));
            var t3 = await traitModel.CalculateEffectiveTraitSetsForTraitName("test_trait_3", layerset, null, timeThreshold);
            Assert.AreEqual(2, t3.Count());
            Assert.IsTrue(t3.All(t => t.EffectiveTraits["test_trait_3"].TraitAttributes.Any(ta => ta.Value.Attribute.Name == "a1")));
        }


        [Test]
        public async Task TestDependentTraits()
        {
            var dbcb = new DBConnectionBuilder();
            using var conn = dbcb.Build(DBSetup.dbName, false, true);
            var (traitModel, layerset) = await BaseSetup(new MockedTraitsProvider(), conn);

            var timeThreshold = TimeThreshold.BuildLatest();

            var t1 = await traitModel.CalculateEffectiveTraitSetsForTraitName("test_trait_4", layerset, null, timeThreshold);
            Assert.AreEqual(2, t1.Count());
            var t2 = await traitModel.CalculateEffectiveTraitSetsForTraitName("test_trait_5", layerset, null, timeThreshold);
            Assert.AreEqual(1, t2.Count());
        }

        [Test]
        public async Task TestDependentTraitLoop()
        {
            var timeThreshold = TimeThreshold.BuildLatest();
            var dbcb = new DBConnectionBuilder();
            using var conn = dbcb.Build(DBSetup.dbName, false, true);
            var (traitModel, layerset) = await BaseSetup(new MockedTraitsProviderWithLoop(), conn);
            var t1 = await traitModel.CalculateEffectiveTraitSetsForTraitName("test_trait_1", layerset, null, timeThreshold);
            Assert.AreEqual(0, t1.Count());
        }

        private async Task<(TraitModel traitModel, LayerSet layerset)> BaseSetup(ITraitsProvider traitsProvider, NpgsqlConnection conn)
        {
            var attributeModel = new AttributeModel(MockedEmptyOnlineAccessProxy.O, conn);
            var ciModel = new CIModel(attributeModel, conn);
            var userModel = new UserInDatabaseModel(conn);
            var changesetModel = new ChangesetModel(userModel, conn);
            var predicateModel = new PredicateModel(conn);
            var relationModel = new RelationModel(MockedEmptyOnlineAccessProxy.O, predicateModel, conn);
            var layerModel = new LayerModel(conn);
            var traitModel = new TraitModel(ciModel, relationModel, traitsProvider, NullLogger<TraitModel>.Instance, conn);
            var user = await DBSetup.SetupUser(userModel);
            var ciid1 = await ciModel.CreateCI(null);
            var ciid2 = await ciModel.CreateCI(null);
            var ciid3 = await ciModel.CreateCI(null);
            var layer1 = await layerModel.CreateLayer("l1", null);

            using (var trans = conn.BeginTransaction())
            {
                var changeset = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);
                await attributeModel.InsertAttribute("a1", AttributeScalarValueText.Build("text1"), layer1.ID, ciid1, changeset, trans);
                await attributeModel.InsertAttribute("a2", AttributeScalarValueText.Build("text2"), layer1.ID, ciid1, changeset, trans);
                await attributeModel.InsertAttribute("a3", AttributeScalarValueText.Build("text3"), layer1.ID, ciid1, changeset, trans);
                await attributeModel.InsertAttribute("a4", AttributeScalarValueText.Build("text4"), layer1.ID, ciid1, changeset, trans);

                await attributeModel.InsertAttribute("a1", AttributeScalarValueText.Build("text1"), layer1.ID, ciid2, changeset, trans);
                await attributeModel.InsertAttribute("a4", AttributeScalarValueText.Build("text4"), layer1.ID, ciid2, changeset, trans);

                await attributeModel.InsertAttribute("a2", AttributeScalarValueText.Build("text2"), layer1.ID, ciid3, changeset, trans);
                await attributeModel.InsertAttribute("a3", AttributeScalarValueText.Build("text3"), layer1.ID, ciid3, changeset, trans);
                await attributeModel.InsertAttribute("a4", AttributeScalarValueText.Build("text4"), layer1.ID, ciid3, changeset, trans);

                trans.Commit();
            }

            var layerset = await layerModel.BuildLayerSet(new string[] { "l1" }, null);
            return (traitModel, layerset);
        }
    }
}
