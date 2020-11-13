using FluentAssertions;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.Model;
using Omnikeeper.Service;
using Omnikeeper.Utils;
using Npgsql;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tests.Integration.Model.Mocks;
using Omnikeeper.Base.Utils.ModelContext;

namespace Tests.Integration.Model
{
    class CIModelTest : DBBackedTestBase
    {
        [Test]
        public async Task TestGetCIs()
        {
            var attributeModel = new AttributeModel(new BaseAttributeModel());
            var model = new CIModel(attributeModel);
            var userModel = new UserInDatabaseModel();
            var changesetModel = new ChangesetModel(userModel);
            var layerModel = new LayerModel();
            var user = await DBSetup.SetupUser(userModel, ModelContextBuilder.BuildImmediate());
            Guid ciid1;
            Guid ciid2;
            Guid ciid3;
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changesetID = await changesetModel.CreateChangeset(user.ID, trans);
                ciid1 = await model.CreateCI(trans);
                ciid2 = await model.CreateCI(trans);
                ciid3 = await model.CreateCI(trans);
                trans.Commit();
            }

            long layerID1;
            long layerID2;
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var layer1 = await layerModel.CreateLayer("l1", trans);
                var layer2 = await layerModel.CreateLayer("l2", trans);
                layerID1 = layer1.ID;
                layerID2 = layer2.ID;
                trans.Commit();
            }

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = new ChangesetProxy(user, DateTimeOffset.Now, changesetModel);
                var i1 = await attributeModel.InsertAttribute("a1", new AttributeScalarValueText("text1"), ciid1, layerID1, changeset, trans);
                var i2 = await attributeModel.InsertAttribute("a2", new AttributeScalarValueText("text1"), ciid2, layerID1, changeset, trans);
                var i3 = await attributeModel.InsertAttribute("a3", new AttributeScalarValueText("text1"), ciid1, layerID2, changeset, trans);

                trans.Commit();
            }

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = await changesetModel.CreateChangeset(user.ID, trans);
                var cis1 = await model.GetMergedCIs(new AllCIIDsSelection(), new LayerSet(layerID1), false, trans, TimeThreshold.BuildLatest());
                Assert.AreEqual(2, cis1.Count());
                Assert.AreEqual(1, cis1.Count(c => c.ID == ciid1 && c.MergedAttributes.ContainsKey("a1")));
                Assert.AreEqual(1, cis1.Count(c => c.ID == ciid2 && c.MergedAttributes.ContainsKey("a2")));
                var cis2 = await model.GetMergedCIs(new AllCIIDsSelection(), new LayerSet(layerID2), false, trans, TimeThreshold.BuildLatest());
                Assert.AreEqual(1, cis2.Count());
                Assert.AreEqual(1, cis2.Count(c => c.ID == ciid1 && c.MergedAttributes.ContainsKey("a3")));
                var cis3 = await model.GetMergedCIs(new AllCIIDsSelection(), new LayerSet(layerID2), true, trans, TimeThreshold.BuildLatest());
                Assert.AreEqual(3, cis3.Count());
                Assert.AreEqual(1, cis3.Count(c => c.ID == ciid1 && c.MergedAttributes.ContainsKey("a3")));
                Assert.AreEqual(1, cis3.Count(c => c.ID == ciid2 && c.MergedAttributes.Count() == 0));
                Assert.AreEqual(1, cis3.Count(c => c.ID == ciid3 && c.MergedAttributes.Count() == 0));

                trans.Commit();
            }
        }

        [Test]
        public async Task TestLayerSets()
        {
            var attributeModel = new AttributeModel(new BaseAttributeModel());
            var userModel = new UserInDatabaseModel();
            var changesetModel = new ChangesetModel(userModel);
            var model = new CIModel(attributeModel);
            var layerModel = new LayerModel();
            using var trans = ModelContextBuilder.BuildDeferred();
            var user = await DBSetup.SetupUser(userModel, trans);

            var ciid1 = await model.CreateCI(trans);
            var layer1 = await layerModel.CreateLayer("l1", trans);
            var layer2 = await layerModel.CreateLayer("l2", trans);

            var layerset1 = new LayerSet(new long[] { layer1.ID });
            var layerset2 = new LayerSet(new long[] { layer2.ID });
            var layerset3 = new LayerSet(new long[] { layer1.ID, layer2.ID });
            var layerset4 = new LayerSet(new long[] { layer2.ID, layer1.ID });

            var changeset = new ChangesetProxy(user, DateTimeOffset.Now, changesetModel);
            await attributeModel.InsertAttribute("a1", new AttributeScalarValueText("textL1"), ciid1, layer1.ID, changeset, trans);
            await attributeModel.InsertAttribute("a1", new AttributeScalarValueText("textL2"), ciid1, layer2.ID, changeset, trans);

            var a1 = await attributeModel.GetMergedAttributes(ciid1, layerset1, trans, TimeThreshold.BuildLatest());
            Assert.AreEqual(1, a1.Count());
            Assert.AreEqual(new AttributeScalarValueText("textL1"), a1.First().Value.Attribute.Value);

            var a2 = await attributeModel.GetMergedAttributes(ciid1, layerset2, trans, TimeThreshold.BuildLatest());
            Assert.AreEqual(1, a2.Count());
            Assert.AreEqual(new AttributeScalarValueText("textL2"), a2.First().Value.Attribute.Value);

            var a3 = await attributeModel.GetMergedAttributes(ciid1, layerset3, trans, TimeThreshold.BuildLatest());
            Assert.AreEqual(1, a3.Count());
            Assert.AreEqual(new AttributeScalarValueText("textL1"), a3.First().Value.Attribute.Value);

            var a4 = await attributeModel.GetMergedAttributes(ciid1, layerset4, trans, TimeThreshold.BuildLatest());
            Assert.AreEqual(1, a4.Count());
            Assert.AreEqual(new AttributeScalarValueText("textL2"), a4.First().Value.Attribute.Value);
        }

        [Test]
        public async Task TestRemoveShowsLayerBelow()
        {
            var attributeModel = new AttributeModel(new BaseAttributeModel());
            var userModel = new UserInDatabaseModel();
            var changesetModel = new ChangesetModel(userModel);
            var model = new CIModel(attributeModel);
            var layerModel = new LayerModel();
            var transI = ModelContextBuilder.BuildImmediate();
            var user = await DBSetup.SetupUser(userModel, transI);

            var ciid1 = await model.CreateCI(transI);
            var layer1 = await layerModel.CreateLayer("l1", transI);
            var layer2 = await layerModel.CreateLayer("l2", transI);
            var layerset1 = new LayerSet(new long[] { layer2.ID, layer1.ID });

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset1 = new ChangesetProxy(user, DateTimeOffset.Now, changesetModel);
                await attributeModel.InsertAttribute("a1", new AttributeScalarValueText("textL1"), ciid1, layer1.ID, changeset1, trans);

                var changeset2 = new ChangesetProxy(user, DateTimeOffset.Now, changesetModel);
                await attributeModel.InsertAttribute("a1", new AttributeScalarValueText("textL2"), ciid1, layer2.ID, changeset2, trans);

                trans.Commit();
            }

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset3 = new ChangesetProxy(user, DateTimeOffset.Now, changesetModel);
                await attributeModel.RemoveAttribute("a1", ciid1, layer2.ID, changeset3, trans);
                trans.Commit();
            }

            var a1 = await attributeModel.GetMergedAttributes(ciid1, layerset1, transI, TimeThreshold.BuildLatest());
            Assert.AreEqual(1, a1.Count()); // layerID1 shines through deleted
            Assert.AreEqual(new AttributeScalarValueText("textL1"), a1.First().Value.Attribute.Value);
        }


        [Test]
        public async Task TestGetCIIDsOfNonEmptyCIs()
        {
            var attributeModel = new AttributeModel(new BaseAttributeModel());
            var userModel = new UserInDatabaseModel();
            var changesetModel = new ChangesetModel(userModel);
            var model = new CIModel(attributeModel);
            var layerModel = new LayerModel();
            var predicateModel = new PredicateModel();
            var relationModel = new RelationModel(new BaseRelationModel(predicateModel));
            var transI = ModelContextBuilder.BuildImmediate();
            var user = await DBSetup.SetupUser(userModel, transI);

            var ciid1 = await model.CreateCI(transI);
            var ciid2 = await model.CreateCI(transI);
            var layer1 = await layerModel.CreateLayer("l1", transI);
            var layer2 = await layerModel.CreateLayer("l2", transI);
            var layer3 = await layerModel.CreateLayer("l3", transI);
            var layerset1 = new LayerSet(new long[] { layer2.ID, layer1.ID });
            var layerset2 = new LayerSet(new long[] { layer1.ID });
            var layerset3 = new LayerSet(new long[] { layer2.ID });
            var layerset4 = new LayerSet(new long[] { layer3.ID });

            using (var trans = ModelContextBuilder.BuildDeferred())
            {

                var changeset1 = new ChangesetProxy(user, DateTimeOffset.Now, changesetModel);
                await attributeModel.InsertAttribute("a1", new AttributeScalarValueText("textL1"), ciid1, layer1.ID, changeset1, trans);

                var changeset2 = new ChangesetProxy(user, DateTimeOffset.Now, changesetModel);
                await attributeModel.InsertAttribute("a2", new AttributeScalarValueText("textL2"), ciid1, layer2.ID, changeset2, trans);

                var changeset3 = new ChangesetProxy(user, DateTimeOffset.Now, changesetModel);
                await attributeModel.InsertAttribute("a1", new AttributeScalarValueText("textL3"), ciid2, layer2.ID, changeset3, trans);

                trans.Commit();
            }

            (await model.GetCIIDsOfNonEmptyCIs(layerset1, transI, TimeThreshold.BuildLatest())).Should().HaveCount(2).And.BeEquivalentTo(new Guid[] { ciid1, ciid2 });
            (await model.GetCIIDsOfNonEmptyCIs(layerset2, transI, TimeThreshold.BuildLatest())).Should().HaveCount(1).And.BeEquivalentTo(new Guid[] { ciid1 });
            (await model.GetCIIDsOfNonEmptyCIs(layerset3, transI, TimeThreshold.BuildLatest())).Should().HaveCount(2).And.BeEquivalentTo(new Guid[] { ciid1, ciid2 });

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset3 = new ChangesetProxy(user, DateTimeOffset.Now, changesetModel);
                await attributeModel.RemoveAttribute("a2", ciid1, layer2.ID, changeset3, trans);
                trans.Commit();
            }

            (await model.GetCIIDsOfNonEmptyCIs(layerset1, transI, TimeThreshold.BuildLatest())).Should().HaveCount(2).And.BeEquivalentTo(new Guid[] { ciid1, ciid2 });
            (await model.GetCIIDsOfNonEmptyCIs(layerset2, transI, TimeThreshold.BuildLatest())).Should().HaveCount(1).And.BeEquivalentTo(new Guid[] { ciid1 });
            (await model.GetCIIDsOfNonEmptyCIs(layerset3, transI, TimeThreshold.BuildLatest())).Should().HaveCount(1).And.BeEquivalentTo(new Guid[] { ciid2 });


            (await model.GetCIIDsOfNonEmptyCIs(layerset4, transI, TimeThreshold.BuildLatest())).Should().HaveCount(0);
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                await predicateModel.InsertOrUpdate("p1", "pw1", "pw1", AnchorState.Active, PredicateConstraints.Default, trans);
                var changeset1 = new ChangesetProxy(user, DateTimeOffset.Now, changesetModel);
                await relationModel.InsertRelation(ciid1, ciid2, "p1", layer3.ID, changeset1, trans);
                trans.Commit();
            }
            (await model.GetCIIDsOfNonEmptyCIs(layerset4, transI, TimeThreshold.BuildLatest())).Should().HaveCount(2).And.BeEquivalentTo(new Guid[] { ciid1, ciid2 });
        }
    }
}
