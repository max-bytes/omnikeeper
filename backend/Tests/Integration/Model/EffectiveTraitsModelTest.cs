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
            var (traitModel, layerset, ciids) = await BaseSetup(new MockedTraitsProvider(), conn);

            var timeThreshold = TimeThreshold.BuildLatest();

            var t0 = await traitModel.CalculateEffectiveTraitsForTraitName("invalid_trait", layerset, null, timeThreshold);
            Assert.AreEqual(null, t0);

            var t1 = await traitModel.CalculateEffectiveTraitsForTraitName("test_trait_1", layerset, null, timeThreshold);
            Assert.AreEqual(3, t1.Count());
            var t2 = await traitModel.CalculateEffectiveTraitsForTraitName("test_trait_2", layerset, null, timeThreshold);
            Assert.AreEqual(2, t2.Count());
            Assert.IsTrue(t2.All(t => t.Value.TraitAttributes.Any(ta => ta.Value.Attribute.Name == "a2") && t.Value.TraitAttributes.Any(ta => ta.Value.Attribute.Name == "a4")));
            var t3 = await traitModel.CalculateEffectiveTraitsForTraitName("test_trait_3", layerset, null, timeThreshold);
            Assert.AreEqual(2, t3.Count());
            Assert.IsTrue(t3.All(t => t.Value.TraitAttributes.Any(ta => ta.Value.Attribute.Name == "a1")));

            var tt0 = await traitModel.CalculateMergedCIsWithTrait("invalid_trait", layerset, null, timeThreshold);
            Assert.AreEqual(null, tt0);


            var tt1 = await traitModel.CalculateMergedCIsWithTrait("test_trait_1", layerset, null, timeThreshold);
            Assert.AreEqual(3, tt1.Count());
            tt1.Select(c => c.ID).Should().BeEquivalentTo(new Guid[] { ciids[0], ciids[1], ciids[2] });

            var tt2 = await traitModel.CalculateMergedCIsWithTrait("test_trait_2", layerset, null, timeThreshold);
            Assert.AreEqual(2, tt2.Count());
            tt2.Select(c => c.ID).Should().BeEquivalentTo(new Guid[] { ciids[0], ciids[2] });
        }

        [Test]
        public async Task TestDependentTraits()
        {
            var dbcb = new DBConnectionBuilder();
            using var conn = dbcb.Build(DBSetup.dbName, false, true);
            var (traitModel, layerset, _) = await BaseSetup(new MockedTraitsProvider(), conn);

            var timeThreshold = TimeThreshold.BuildLatest();

            var t1 = await traitModel.CalculateEffectiveTraitsForTraitName("test_trait_4", layerset, null, timeThreshold);
            Assert.AreEqual(2, t1.Count());
            var t2 = await traitModel.CalculateEffectiveTraitsForTraitName("test_trait_5", layerset, null, timeThreshold);
            Assert.AreEqual(1, t2.Count());
        }

        [Test]
        public async Task TestDependentTraitLoop()
        {
            var timeThreshold = TimeThreshold.BuildLatest();
            var dbcb = new DBConnectionBuilder();
            using var conn = dbcb.Build(DBSetup.dbName, false, true);
            var (traitModel, layerset, _) = await BaseSetup(new MockedTraitsProviderWithLoop(), conn);
            var t1 = await traitModel.CalculateEffectiveTraitsForTraitName("test_trait_1", layerset, null, timeThreshold);
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
                await attributeModel.InsertAttribute("a1", AttributeScalarValueText.Build("text1"), ciid1, layer1.ID, changeset, trans);
                await attributeModel.InsertAttribute("a2", AttributeScalarValueText.Build("text2"), ciid1, layer1.ID, changeset, trans);
                await attributeModel.InsertAttribute("a3", AttributeScalarValueText.Build("text3"), ciid1, layer1.ID, changeset, trans);
                await attributeModel.InsertAttribute("a4", AttributeScalarValueText.Build("text4"), ciid1, layer1.ID, changeset, trans);

                await attributeModel.InsertAttribute("a1", AttributeScalarValueText.Build("text1"), ciid2, layer1.ID, changeset, trans);
                await attributeModel.InsertAttribute("a4", AttributeScalarValueText.Build("text4"), ciid2, layer1.ID, changeset, trans);

                await attributeModel.InsertAttribute("a2", AttributeScalarValueText.Build("text2"), ciid3, layer1.ID, changeset, trans);
                await attributeModel.InsertAttribute("a3", AttributeScalarValueText.Build("text3"), ciid3, layer1.ID, changeset, trans);
                await attributeModel.InsertAttribute("a4", AttributeScalarValueText.Build("text4"), ciid3, layer1.ID, changeset, trans);

                trans.Commit();
            }

            var layerset = await layerModel.BuildLayerSet(new string[] { "l1" }, null);
            return (traitModel, layerset, new Guid[] { ciid1, ciid2, ciid3 });
        }
    }
}
