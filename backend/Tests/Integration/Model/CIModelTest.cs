using FluentAssertions;
using Landscape.Base.Entity;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using LandscapeRegistry.Entity.AttributeValues;
using LandscapeRegistry.Model;
using LandscapeRegistry.Utils;
using Npgsql;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;
using Tests.Integration.Model.Mocks;

namespace Tests.Integration.Model
{
    class CIModelTest
    {
        private NpgsqlConnection conn;

        [SetUp]
        public void Setup()
        {
            DBSetup.Setup();

            var dbcb = new DBConnectionBuilder();
            conn = dbcb.Build(DBSetup.dbName, false, true);

        }

        [TearDown]
        public void TearDown()
        {
            conn.Close();
        }


        [Test]
        public async Task TestGetCIs()
        {
            var attributeModel = new AttributeModel(MockedEmptyOnlineAccessProxy.O, conn);
            var model = new CIModel(attributeModel, conn);
            var userModel = new UserInDatabaseModel(conn);
            var changesetModel = new ChangesetModel(userModel, conn);
            var layerModel = new LayerModel(conn);
            var user = await DBSetup.SetupUser(userModel);
            Guid ciid1;
            Guid ciid2;
            Guid ciid3;
            using (var trans = conn.BeginTransaction())
            {
                var changesetID = await changesetModel.CreateChangeset(user.ID, trans);
                ciid1 = await model.CreateCI(trans);
                ciid2 = await model.CreateCI(trans);
                ciid3 = await model.CreateCI(trans);
                trans.Commit();
            }

            long layerID1;
            long layerID2;
            using (var trans = conn.BeginTransaction())
            {
                var layer1 = await layerModel.CreateLayer("l1", trans);
                var layer2 = await layerModel.CreateLayer("l2", trans);
                layerID1 = layer1.ID;
                layerID2 = layer2.ID;
                trans.Commit();
            }

            using (var trans = conn.BeginTransaction())
            {
                var changeset = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);
                var i1 = await attributeModel.InsertAttribute("a1", AttributeScalarValueText.Build("text1"), layerID1, ciid1, changeset, trans);
                var i2 = await attributeModel.InsertAttribute("a2", AttributeScalarValueText.Build("text1"), layerID1, ciid2, changeset, trans);
                var i3 = await attributeModel.InsertAttribute("a3", AttributeScalarValueText.Build("text1"), layerID2, ciid1, changeset, trans);

                trans.Commit();
            }

            using (var trans = conn.BeginTransaction())
            {
                var changeset = await changesetModel.CreateChangeset(user.ID, trans);
                var cis1 = await model.GetCIs(layerID1, new AllCIIDsSelection(), false, trans, TimeThreshold.BuildLatest());
                Assert.AreEqual(2, cis1.Count());
                Assert.AreEqual(1, cis1.Count(c => c.ID == ciid1 && c.Attributes.Any(a => a.Name == "a1")));
                Assert.AreEqual(1, cis1.Count(c => c.ID == ciid2 && c.Attributes.Any(a => a.Name == "a2")));
                var cis2 = await model.GetCIs(layerID2, new AllCIIDsSelection(), false, trans, TimeThreshold.BuildLatest());
                Assert.AreEqual(1, cis2.Count());
                Assert.AreEqual(1, cis2.Count(c => c.ID == ciid1 && c.Attributes.Any(a => a.Name == "a3")));
                var cis3 = await model.GetCIs(layerID2, new AllCIIDsSelection(), true, trans, TimeThreshold.BuildLatest());
                Assert.AreEqual(3, cis3.Count());
                Assert.AreEqual(1, cis3.Count(c => c.ID == ciid1 && c.Attributes.Any(a => a.Name == "a3")));
                Assert.AreEqual(1, cis3.Count(c => c.ID == ciid2 && c.Attributes.Count() == 0));
                Assert.AreEqual(1, cis3.Count(c => c.ID == ciid3 && c.Attributes.Count() == 0));

                trans.Commit();
            }
        }

        [Test]
        public async Task TestLayerSets()
        {
            var attributeModel = new AttributeModel(MockedEmptyOnlineAccessProxy.O, conn);
            var userModel = new UserInDatabaseModel(conn);
            var changesetModel = new ChangesetModel(userModel, conn);
            var model = new CIModel(attributeModel, conn);
            var layerModel = new LayerModel(conn);
            using var trans = conn.BeginTransaction();
            var user = await DBSetup.SetupUser(userModel);

            var ciid1 = await model.CreateCI(trans);
            var layer1 = await layerModel.CreateLayer("l1", trans);
            var layer2 = await layerModel.CreateLayer("l2", trans);

            var layerset1 = new LayerSet(new long[] { layer1.ID });
            var layerset2 = new LayerSet(new long[] { layer2.ID });
            var layerset3 = new LayerSet(new long[] { layer1.ID, layer2.ID });
            var layerset4 = new LayerSet(new long[] { layer2.ID, layer1.ID });

            var changeset = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);
            await attributeModel.InsertAttribute("a1", AttributeScalarValueText.Build("textL1"), layer1.ID, ciid1, changeset, trans);
            await attributeModel.InsertAttribute("a1", AttributeScalarValueText.Build("textL2"), layer2.ID, ciid1, changeset, trans);

            var a1 = await attributeModel.GetMergedAttributes(ciid1, false, layerset1, trans, TimeThreshold.BuildLatest());
            Assert.AreEqual(1, a1.Count());
            Assert.AreEqual(AttributeScalarValueText.Build("textL1"), a1.First().Value.Attribute.Value);

            var a2 = await attributeModel.GetMergedAttributes(ciid1, false, layerset2, trans, TimeThreshold.BuildLatest());
            Assert.AreEqual(1, a2.Count());
            Assert.AreEqual(AttributeScalarValueText.Build("textL2"), a2.First().Value.Attribute.Value);

            var a3 = await attributeModel.GetMergedAttributes(ciid1, false, layerset3, trans, TimeThreshold.BuildLatest());
            Assert.AreEqual(1, a3.Count());
            Assert.AreEqual(AttributeScalarValueText.Build("textL1"), a3.First().Value.Attribute.Value);

            var a4 = await attributeModel.GetMergedAttributes(ciid1, false, layerset4, trans, TimeThreshold.BuildLatest());
            Assert.AreEqual(1, a4.Count());
            Assert.AreEqual(AttributeScalarValueText.Build("textL2"), a4.First().Value.Attribute.Value);
        }

        [Test]
        public async Task TestRemoveShowsLayerBelow()
        {
            var attributeModel = new AttributeModel(MockedEmptyOnlineAccessProxy.O, conn);
            var userModel = new UserInDatabaseModel(conn);
            var changesetModel = new ChangesetModel(userModel, conn);
            var model = new CIModel(attributeModel, conn);
            var layerModel = new LayerModel(conn);
            var user = await DBSetup.SetupUser(userModel);

            var ciid1 = await model.CreateCI(null);
            var layer1 = await layerModel.CreateLayer("l1", null);
            var layer2 = await layerModel.CreateLayer("l2", null);
            var layerset1 = new LayerSet(new long[] { layer2.ID, layer1.ID });

            using (var trans = conn.BeginTransaction())
            {

                var changeset1 = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);
                await attributeModel.InsertAttribute("a1", AttributeScalarValueText.Build("textL1"), layer1.ID, ciid1, changeset1, trans);

                var changeset2 = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);
                await attributeModel.InsertAttribute("a1", AttributeScalarValueText.Build("textL2"), layer2.ID, ciid1, changeset2, trans);

                trans.Commit();
            }

            using (var trans = conn.BeginTransaction())
            {
                var changeset3 = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);
                await attributeModel.RemoveAttribute("a1", layer2.ID, ciid1, changeset3, trans);
                trans.Commit();
            }

            var a1 = await attributeModel.GetMergedAttributes(ciid1, false, layerset1, null, TimeThreshold.BuildLatest());
            Assert.AreEqual(1, a1.Count()); // layerID1 shines through deleted
            Assert.AreEqual(AttributeScalarValueText.Build("textL1"), a1.First().Value.Attribute.Value);
        }


        [Test]
        public async Task TestGetCIIDsOfNonEmptyCIs()
        {
            var attributeModel = new AttributeModel(MockedEmptyOnlineAccessProxy.O, conn);
            var userModel = new UserInDatabaseModel(conn);
            var changesetModel = new ChangesetModel(userModel, conn);
            var model = new CIModel(attributeModel, conn);
            var layerModel = new LayerModel(conn);
            var predicateModel = new PredicateModel(conn);
            var relationModel = new RelationModel(MockedEmptyOnlineAccessProxy.O, predicateModel, conn);
            var user = await DBSetup.SetupUser(userModel);

            var ciid1 = await model.CreateCI(null);
            var ciid2 = await model.CreateCI(null);
            var layer1 = await layerModel.CreateLayer("l1", null);
            var layer2 = await layerModel.CreateLayer("l2", null);
            var layer3 = await layerModel.CreateLayer("l3", null);
            var layerset1 = new LayerSet(new long[] { layer2.ID, layer1.ID });
            var layerset2 = new LayerSet(new long[] { layer1.ID });
            var layerset3 = new LayerSet(new long[] { layer2.ID });
            var layerset4 = new LayerSet(new long[] { layer3.ID });

            using (var trans = conn.BeginTransaction())
            {

                var changeset1 = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);
                await attributeModel.InsertAttribute("a1", AttributeScalarValueText.Build("textL1"), layer1.ID, ciid1, changeset1, trans);

                var changeset2 = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);
                await attributeModel.InsertAttribute("a2", AttributeScalarValueText.Build("textL2"), layer2.ID, ciid1, changeset2, trans);

                var changeset3 = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);
                await attributeModel.InsertAttribute("a1", AttributeScalarValueText.Build("textL3"), layer2.ID, ciid2, changeset3, trans);

                trans.Commit();
            }

            (await model.GetCIIDsOfNonEmptyCIs(layerset1, null, TimeThreshold.BuildLatest())).Should().HaveCount(2).And.BeEquivalentTo(new Guid[] { ciid1, ciid2 });
            (await model.GetCIIDsOfNonEmptyCIs(layerset2, null, TimeThreshold.BuildLatest())).Should().HaveCount(1).And.BeEquivalentTo(new Guid[] { ciid1 });
            (await model.GetCIIDsOfNonEmptyCIs(layerset3, null, TimeThreshold.BuildLatest())).Should().HaveCount(2).And.BeEquivalentTo(new Guid[] { ciid1, ciid2 });

            using (var trans = conn.BeginTransaction())
            {
                var changeset3 = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);
                await attributeModel.RemoveAttribute("a2", layer2.ID, ciid1, changeset3, trans);
                trans.Commit();
            }

            (await model.GetCIIDsOfNonEmptyCIs(layerset1, null, TimeThreshold.BuildLatest())).Should().HaveCount(2).And.BeEquivalentTo(new Guid[] { ciid1, ciid2 });
            (await model.GetCIIDsOfNonEmptyCIs(layerset2, null, TimeThreshold.BuildLatest())).Should().HaveCount(1).And.BeEquivalentTo(new Guid[] { ciid1 });
            (await model.GetCIIDsOfNonEmptyCIs(layerset3, null, TimeThreshold.BuildLatest())).Should().HaveCount(1).And.BeEquivalentTo(new Guid[] { ciid2 });


            (await model.GetCIIDsOfNonEmptyCIs(layerset4, null, TimeThreshold.BuildLatest())).Should().HaveCount(0);
            using (var trans = conn.BeginTransaction())
            {
                await predicateModel.InsertOrUpdate("p1", "pw1", "pw1", AnchorState.Active, PredicateConstraints.Default, trans);
                var changeset1 = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);
                await relationModel.InsertRelation(ciid1, ciid2, "p1", layer3.ID, changeset1, trans);
                trans.Commit();
            }
            (await model.GetCIIDsOfNonEmptyCIs(layerset4, null, TimeThreshold.BuildLatest())).Should().HaveCount(2).And.BeEquivalentTo(new Guid[] { ciid1, ciid2 });
        }
    }
}
