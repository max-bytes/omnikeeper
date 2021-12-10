using FluentAssertions;
using Npgsql;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tests.Integration.Model
{
    class AttributeModelTest : DBBackedTestBase
    {
        [Test]
        public async Task TestAddingUpdatingRemovingAndRenewingOfAttributes()
        {
            var baseAttributeModel = new BaseAttributeModel(new PartitionModel(), new CIIDModel());
            var attributeModel = new AttributeModel(baseAttributeModel);
            var model = new CIModel(attributeModel, new CIIDModel());
            var userModel = new UserInDatabaseModel();
            var changesetModel = new ChangesetModel(userModel);
            var layerModel = new LayerModel();
            var transI = ModelContextBuilder.BuildImmediate();
            var user = await DBSetup.SetupUser(userModel, transI);
            Guid ciid1;
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                ciid1 = await model.CreateCI(trans);
                trans.Commit();
            }

            // TODO: this shouldn't be tested here
            Assert.ThrowsAsync<PostgresException>(async () => await model.CreateCI(ciid1, transI)); // cannot add same identity twice

            string layerID1;
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var layer1 = await layerModel.UpsertLayer("l1", trans);
                layerID1 = layer1.ID;
                Assert.AreEqual("l1", layerID1);
                trans.Commit();
            }

            var layerset = await layerModel.BuildLayerSet(new string[] { "l1" }, transI);

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
                var i1 = await attributeModel.InsertAttribute("a1", new AttributeScalarValueText("text1"), ciid1, layerID1, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                Assert.AreEqual("a1", i1.attribute.Name);

                trans.Commit();
            }

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
                var i2 = await attributeModel.InsertAttribute("a1", new AttributeScalarValueText("text2"), ciid1, layerID1, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                Assert.AreEqual("a1", i2.attribute.Name);

                var a1 = (await attributeModel.GetMergedAttributes(SpecificCIIDsSelection.Build(ciid1), AllAttributeSelection.Instance, layerset, trans, TimeThreshold.BuildLatest())).Values.First();
                Assert.AreEqual(1, a1.Count());
                var aa1 = a1.First().Value;
                Assert.AreEqual(ciid1, aa1.Attribute.CIID);
                Assert.AreEqual("a1", aa1.Attribute.Name);
                Assert.AreEqual(new AttributeScalarValueText("text2"), aa1.Attribute.Value);
                Assert.AreEqual((await changeset.GetChangeset(layerID1, new DataOriginV1(DataOriginType.Manual), trans)).ID, aa1.Attribute.ChangesetID);
                trans.Commit();
            }

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
                var r1 = await attributeModel.RemoveAttribute("a1", ciid1, layerID1, changeset, new DataOriginV1(DataOriginType.Manual), trans, MaskHandlingForRemovalApplyNoMask.Instance);
                Assert.AreEqual("a1", r1.attribute.Name);

                var a2 = (await attributeModel.GetMergedAttributes(SpecificCIIDsSelection.Build(ciid1), AllAttributeSelection.Instance, layerset, trans, TimeThreshold.BuildLatest())).Values;
                Assert.AreEqual(0, a2.Count());

                // compare fetching merged vs non-merged
                var ma3 = await attributeModel.GetMergedAttributes(SpecificCIIDsSelection.Build(ciid1), AllAttributeSelection.Instance, layerset, trans, TimeThreshold.BuildLatest());
                var a3 = await baseAttributeModel.GetAttributes(SpecificCIIDsSelection.Build(ciid1), AllAttributeSelection.Instance, layerset.LayerIDs, trans, TimeThreshold.BuildLatest());
                Assert.AreEqual(0, ma3.Count);
                Assert.AreEqual(0, a3[0].Count());

                trans.Commit();
            }

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
                var i3 = await attributeModel.InsertAttribute("a1", new AttributeScalarValueText("text3"), ciid1, layerID1, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                Assert.AreEqual("a1", i3.attribute.Name);

                var a4 = (await attributeModel.GetMergedAttributes(SpecificCIIDsSelection.Build(ciid1), AllAttributeSelection.Instance, layerset, trans, TimeThreshold.BuildLatest())).Values.First();
                Assert.AreEqual(1, a4.Count());
                var aa4 = a4.First().Value;
                Assert.AreEqual(new AttributeScalarValueText("text3"), aa4.Attribute.Value);
            }
        }


        [Test]
        public async Task TestAttributeValueMultiplicities()
        {
            var userModel = new UserInDatabaseModel();
            var changesetModel = new ChangesetModel(userModel);
            var attributeModel = new AttributeModel(new BaseAttributeModel(new PartitionModel(), new CIIDModel()));
            var model = new CIModel(attributeModel, new CIIDModel());
            var layerModel = new LayerModel();
            var transI = ModelContextBuilder.BuildImmediate();
            var user = await DBSetup.SetupUser(userModel, transI);

            var ciid1 = await model.CreateCI(transI);
            var layer1 = await layerModel.UpsertLayer("l1", transI);

            var layerset1 = new LayerSet(new string[] { layer1.ID });

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
                await attributeModel.InsertAttribute("a1", AttributeArrayValueText.BuildFromString(new string[] { "a", "b", "c" }), ciid1, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                var a1 = (await attributeModel.GetMergedAttributes(SpecificCIIDsSelection.Build(ciid1), AllAttributeSelection.Instance, layerset1, trans, TimeThreshold.BuildLatest())).Values.First();
                Assert.AreEqual(1, a1.Count());
                Assert.AreEqual(AttributeArrayValueText.BuildFromString(new string[] { "a", "b", "c" }), a1.First().Value.Attribute.Value);
            }

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
                await attributeModel.InsertAttribute("a1", AttributeArrayValueText.BuildFromString(new string[] { "a,", "b,b", ",c", "\\d", "\\,e" }), ciid1, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                var a1 = (await attributeModel.GetMergedAttributes(SpecificCIIDsSelection.Build(ciid1), AllAttributeSelection.Instance, layerset1, trans, TimeThreshold.BuildLatest())).Values.First();
                Assert.AreEqual(1, a1.Count());
                Assert.AreEqual(AttributeArrayValueText.BuildFromString(new string[] { "a,", "b,b", ",c", "\\d", "\\,e" }), a1.First().Value.Attribute.Value);
            }

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
                await attributeModel.InsertAttribute("a1", AttributeArrayValueInteger.Build(new long[] { 1, 2, 3, 4 }), ciid1, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                var a1 = (await attributeModel.GetMergedAttributes(SpecificCIIDsSelection.Build(ciid1), AllAttributeSelection.Instance, layerset1, trans, TimeThreshold.BuildLatest())).Values.First();
                Assert.AreEqual(1, a1.Count());
                Assert.AreEqual(AttributeArrayValueInteger.Build(new long[] { 1, 2, 3, 4 }), a1.First().Value.Attribute.Value);
            }
        }


        [Test]
        public async Task TestStringBasedAttributeValueTypes()
        {
            var userModel = new UserInDatabaseModel();
            var changesetModel = new ChangesetModel(userModel);
            var attributeModel = new AttributeModel(new BaseAttributeModel(new PartitionModel(), new CIIDModel()));
            var model = new CIModel(attributeModel, new CIIDModel());
            var layerModel = new LayerModel();
            var transI = ModelContextBuilder.BuildImmediate();
            var user = await DBSetup.SetupUser(userModel, transI);

            var ciid1 = await model.CreateCI(transI);
            var layer1 = await layerModel.UpsertLayer("l1", transI);

            var layerset1 = new LayerSet(new string[] { layer1.ID });

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
                await attributeModel.InsertAttribute("a1", AttributeArrayValueInteger.Build(new long[] { 4, 3, -2 }), ciid1, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                var a1 = (await attributeModel.GetMergedAttributes(SpecificCIIDsSelection.Build(ciid1), AllAttributeSelection.Instance, layerset1, trans, TimeThreshold.BuildLatest())).Values.First();
                Assert.AreEqual(1, a1.Count());
                Assert.AreEqual(AttributeArrayValueInteger.Build(new long[] { 4, 3, -2 }), a1.First().Value.Attribute.Value);
            }

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
                await attributeModel.InsertAttribute("a1", AttributeArrayValueJSON.BuildFromString(new string[] { "{}", "{\"foo\":\"var\" }" }), ciid1, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                var a1 = (await attributeModel.GetMergedAttributes(SpecificCIIDsSelection.Build(ciid1), AllAttributeSelection.Instance, layerset1, trans, TimeThreshold.BuildLatest())).Values.First();
                Assert.AreEqual(1, a1.Count());
                Assert.AreEqual(AttributeArrayValueJSON.BuildFromString(new string[] { "{}", "{\"foo\":\"var\" }" }), a1.First().Value.Attribute.Value);
            }
        }

        [Test]
        public async Task TestBinaryBasedAttributeValueTypes()
        {
            var userModel = new UserInDatabaseModel();
            var changesetModel = new ChangesetModel(userModel);
            var attributeModel = new AttributeModel(new BaseAttributeModel(new PartitionModel(), new CIIDModel()));
            var model = new CIModel(attributeModel, new CIIDModel());
            var layerModel = new LayerModel();
            var transI = ModelContextBuilder.BuildImmediate();
            var user = await DBSetup.SetupUser(userModel, transI);

            var ciid1 = await model.CreateCI(transI);
            var layer1 = await layerModel.UpsertLayer("l1", transI);

            var layerset1 = new LayerSet(new string[] { layer1.ID });

            var avProxy1 = BinaryScalarAttributeValueProxy.BuildFromHashAndFullData(new byte[] {
                0x00, 0x01, 0x03, 0x04, 0x04, 0x05, 0x06, 0x07,
                0x00, 0x01, 0x03, 0x04, 0x04, 0x05, 0x06, 0x07,
                0x00, 0x01, 0x03, 0x04, 0x04, 0x05, 0x06, 0x07,
                0x00, 0x01, 0x03, 0x04, 0x04, 0x05, 0x06, 0x07,
            }, "testmimetype", fullSize: 64, fullData: new byte[] {
                0x00, 0x01, 0x03, 0x04, 0x04, 0x05, 0x06, 0x07,
                0x00, 0x01, 0x03, 0x04, 0x04, 0x05, 0x06, 0x07,
                0x00, 0x01, 0x03, 0x04, 0x04, 0x05, 0x06, 0x07,
                0x00, 0x01, 0x03, 0x04, 0x04, 0x05, 0x06, 0x07,
                0x00, 0x01, 0x03, 0x04, 0x04, 0x05, 0x06, 0x07,
                0x00, 0x01, 0x03, 0x04, 0x04, 0x05, 0x06, 0x07,
                0x00, 0x01, 0x03, 0x04, 0x04, 0x05, 0x06, 0x07,
                0x00, 0x01, 0x03, 0x04, 0x04, 0x05, 0x06, 0x07,
            });
            var scalarImage = new AttributeScalarValueImage(avProxy1);

            var avProxy2 = BinaryScalarAttributeValueProxy.BuildFromHashAndFullData(new byte[] {
                0xFF, 0x01, 0x03, 0x04, 0x04, 0x05, 0x06, 0x07,
                0x00, 0x01, 0x03, 0x04, 0x04, 0x05, 0x06, 0x07,
                0x00, 0x01, 0x03, 0x04, 0x04, 0x05, 0x06, 0x07,
                0x00, 0x01, 0x03, 0x04, 0x04, 0x05, 0x06, 0x07,
            }, "testmimetype2", fullSize: 64, fullData: new byte[] {
                0xFF, 0x01, 0x03, 0x04, 0x04, 0x05, 0x06, 0x07,
                0x00, 0x01, 0x03, 0x04, 0x04, 0x05, 0x06, 0x07,
                0x00, 0x01, 0x03, 0x04, 0x04, 0x05, 0x06, 0x07,
                0x00, 0x01, 0x03, 0x04, 0x04, 0x05, 0x06, 0x07,
                0x00, 0x01, 0x03, 0x04, 0x04, 0x05, 0x06, 0x07,
                0x00, 0x01, 0x03, 0x04, 0x04, 0x05, 0x06, 0x07,
                0x00, 0x01, 0x03, 0x04, 0x04, 0x05, 0x06, 0x07,
                0x00, 0x01, 0x03, 0x04, 0x04, 0x05, 0x06, 0x07,
            });
            var imageArray = AttributeArrayValueImage.Build(new BinaryScalarAttributeValueProxy[] { avProxy1, avProxy2 });

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
                await attributeModel.InsertAttribute("a1", scalarImage, ciid1, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                var a1 = (await attributeModel.GetMergedAttributes(SpecificCIIDsSelection.Build(ciid1), AllAttributeSelection.Instance, layerset1, trans, TimeThreshold.BuildLatest())).Values.First();
                Assert.AreEqual(1, a1.Count());
                Assert.AreEqual(scalarImage, a1.First().Value.Attribute.Value);
                var returnedProxy = (a1.First().Value.Attribute.Value as AttributeScalarValueImage)!.Value;
                Assert.IsFalse(returnedProxy.HasFullData());
                Assert.AreEqual(returnedProxy.MimeType, "testmimetype");
            }
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
                await attributeModel.InsertAttribute("a1", imageArray, ciid1, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                var a1 = (await attributeModel.GetMergedAttributes(SpecificCIIDsSelection.Build(ciid1), AllAttributeSelection.Instance, layerset1, trans, TimeThreshold.BuildLatest())).Values.First();
                Assert.AreEqual(1, a1.Count());
                Assert.AreEqual(imageArray, a1.First().Value.Attribute.Value);
                var returnedProxy = (a1.First().Value.Attribute.Value as AttributeArrayValueImage)!.Values;
                Assert.IsFalse(returnedProxy.Any(p => p.Value.HasFullData()));
                Assert.AreEqual(returnedProxy[0].Value.MimeType, "testmimetype");
                Assert.AreEqual(returnedProxy[1].Value.MimeType, "testmimetype2");
            }
            // TODO: test full data fetch
        }

        [Test]
        public async Task TestEqualValueInserts()
        {
            var userModel = new UserInDatabaseModel();
            var changesetModel = new ChangesetModel(userModel);
            var attributeModel = new AttributeModel(new BaseAttributeModel(new PartitionModel(), new CIIDModel()));
            var model = new CIModel(attributeModel, new CIIDModel());
            var layerModel = new LayerModel();
            using var trans = ModelContextBuilder.BuildDeferred();
            var user = await DBSetup.SetupUser(userModel, trans);

            var ciid1 = await model.CreateCI(trans);
            var layer1 = await layerModel.UpsertLayer("l1", trans);

            var layerset1 = new LayerSet(new string[] { layer1.ID });

            var changeset1 = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
            var (aa1, changed1) = await attributeModel.InsertAttribute("a1", new AttributeScalarValueText("textL1"), ciid1, layer1.ID, changeset1, new DataOriginV1(DataOriginType.Manual), trans);
            Assert.IsTrue(changed1);

            var changeset2 = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
            var (aa2, changed2) = await attributeModel.InsertAttribute("a1", new AttributeScalarValueText("textL1"), ciid1, layer1.ID, changeset2, new DataOriginV1(DataOriginType.Manual), trans);
            Assert.IsFalse(changed2);

            var a1 = (await attributeModel.GetMergedAttributes(SpecificCIIDsSelection.Build(ciid1), AllAttributeSelection.Instance, layerset1, trans, TimeThreshold.BuildLatest())).Values.First();
            Assert.AreEqual(1, a1.Count());
            Assert.AreEqual(aa2.ID, a1.First().Value.Attribute.ID); // second insertAttribute() must not have changed the current entry
        }

        [Test]
        public async Task TestGetAttributesWithNameRegex()
        {
            var userModel = new UserInDatabaseModel();
            var changesetModel = new ChangesetModel(userModel);
            var baseAttributeModel = new BaseAttributeModel(new PartitionModel(), new CIIDModel());
            var attributeModel = new AttributeModel(baseAttributeModel);
            var model = new CIModel(attributeModel, new CIIDModel());
            var layerModel = new LayerModel();
            using var trans = ModelContextBuilder.BuildDeferred();
            var user = await DBSetup.SetupUser(userModel, trans);

            var ciid1 = await model.CreateCI(trans);
            var ciid2 = await model.CreateCI(trans);
            var layer1 = await layerModel.UpsertLayer("l1", trans);
            var layer2 = await layerModel.UpsertLayer("l2", trans);

            var layerset1 = new LayerSet(new string[] { layer2.ID, layer1.ID });

            var changeset1 = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
            await attributeModel.InsertAttribute("a1", new AttributeScalarValueText("textL1"), ciid1, layer1.ID, changeset1, new DataOriginV1(DataOriginType.Manual), trans);
            await attributeModel.InsertAttribute("a2", new AttributeScalarValueText("textL1"), ciid1, layer1.ID, changeset1, new DataOriginV1(DataOriginType.Manual), trans);

            var changeset2 = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
            await attributeModel.InsertAttribute("a1", new AttributeScalarValueText("textL2"), ciid1, layer2.ID, changeset2, new DataOriginV1(DataOriginType.Manual), trans);

            var changeset3 = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
            await attributeModel.InsertAttribute("a1", new AttributeScalarValueText("textL2"), ciid2, layer2.ID, changeset3, new DataOriginV1(DataOriginType.Manual), trans);
            await attributeModel.InsertAttribute("a3", new AttributeScalarValueText("textL2"), ciid2, layer2.ID, changeset3, new DataOriginV1(DataOriginType.Manual), trans);

            var a1 = (await baseAttributeModel.GetAttributes(new AllCIIDsSelection(), new RegexAttributeSelection("^a"), new string[] { layer1.ID }, trans: trans, atTime: TimeThreshold.BuildLatest())).SelectMany(t => t.Values.SelectMany(t => t.Values));
            Assert.AreEqual(2, a1.Count());

            var a2 = (await baseAttributeModel.GetAttributes(new AllCIIDsSelection(), new RegexAttributeSelection("^a2$"), new string[] { layer1.ID }, trans: trans, atTime: TimeThreshold.BuildLatest())).SelectMany(t => t.Values.SelectMany(t => t.Values));
            Assert.AreEqual(1, a2.Count());

            var a3 = (await baseAttributeModel.GetAttributes(new AllCIIDsSelection(), new RegexAttributeSelection("3$"), new string[] { layer2.ID }, trans: trans, atTime: TimeThreshold.BuildLatest())).SelectMany(t => t.Values.SelectMany(t => t.Values));
            Assert.AreEqual(1, a3.Count());

            var a4 = (await baseAttributeModel.GetAttributes(new AllCIIDsSelection(), new RegexAttributeSelection("^3"), new string[] { layer1.ID }, trans: trans, atTime: TimeThreshold.BuildLatest())).SelectMany(t => t.Values.SelectMany(t => t.Values));
            Assert.AreEqual(0, a4.Count());

            var a5 = (await baseAttributeModel.GetAttributes(SpecificCIIDsSelection.Build(ciid2), new RegexAttributeSelection("^a1$"), new string[] { layer2.ID }, trans: trans, atTime: TimeThreshold.BuildLatest())).SelectMany(t => t.Values.SelectMany(t => t.Values));
            Assert.AreEqual(1, a5.Count());
        }

        [Test]
        public async Task TestBulkReplace()
        {
            var userModel = new UserInDatabaseModel();
            var changesetModel = new ChangesetModel(userModel);
            var baseAttributeModel = new BaseAttributeModel(new PartitionModel(), new CIIDModel());
            var attributeModel = new AttributeModel(baseAttributeModel);
            var model = new CIModel(attributeModel, new CIIDModel());
            var layerModel = new LayerModel();
            using var trans = ModelContextBuilder.BuildDeferred();
            var user = await DBSetup.SetupUser(userModel, trans);

            var ciid1 = await model.CreateCI(trans);
            var ciid2 = await model.CreateCI(trans);
            var layer1 = await layerModel.UpsertLayer("l1", trans);

            var layerset1 = new LayerSet(new string[] { layer1.ID });

            var changeset1 = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
            await attributeModel.InsertAttribute("prefix1.a1", new AttributeScalarValueText("textL1"), ciid1, layer1.ID, changeset1, new DataOriginV1(DataOriginType.Manual), trans);
            await attributeModel.InsertAttribute("prefix1.a2", new AttributeScalarValueText("textL1"), ciid1, layer1.ID, changeset1, new DataOriginV1(DataOriginType.Manual), trans);

            var changeset2 = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
            await attributeModel.InsertAttribute("prefix1.a1", new AttributeScalarValueText("textL2"), ciid2, layer1.ID, changeset2, new DataOriginV1(DataOriginType.Manual), trans);
            await attributeModel.InsertAttribute("prefix2.a1", new AttributeScalarValueText("textL2"), ciid2, layer1.ID, changeset2, new DataOriginV1(DataOriginType.Manual), trans);
            await attributeModel.InsertAttribute("prefix1.a3", new AttributeScalarValueText("textL2"), ciid2, layer1.ID, changeset2, new DataOriginV1(DataOriginType.Manual), trans);

            trans.Commit();

            using var trans2 = ModelContextBuilder.BuildDeferred();
            var changeset3 = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
            await attributeModel.BulkReplaceAttributes(new BulkCIAttributeDataLayerScope("prefix1.", layer1.ID, new BulkCIAttributeDataLayerScope.Fragment[] {
                new BulkCIAttributeDataLayerScope.Fragment("a1", new AttributeScalarValueText("textNew"), ciid1),
                new BulkCIAttributeDataLayerScope.Fragment("a4", new AttributeScalarValueText("textNew"), ciid2),
                new BulkCIAttributeDataLayerScope.Fragment("a2", new AttributeScalarValueText("textNew"), ciid2),
            }), changeset3, new DataOriginV1(DataOriginType.Manual), trans2, MaskHandlingForRemovalApplyNoMask.Instance);
            trans2.Commit();

            using var trans3 = ModelContextBuilder.BuildImmediate();
            var a1 = (await baseAttributeModel.GetAttributes(new AllCIIDsSelection(), new RegexAttributeSelection("^prefix1"), new string[] { layer1.ID }, trans: trans3, atTime: TimeThreshold.BuildLatest())).SelectMany(t => t.Values.SelectMany(t => t.Values));
            Assert.AreEqual(3, a1.Count());
            Assert.AreEqual(1, a1.Where(a => a.Name == "prefix1.a2").Count());
            var a2 = (await baseAttributeModel.GetAttributes(new AllCIIDsSelection(), new RegexAttributeSelection("^prefix2"), new string[] { layer1.ID }, trans: trans3, atTime: TimeThreshold.BuildLatest())).SelectMany(t => t.Values.SelectMany(t => t.Values));
            Assert.AreEqual(1, a2.Count());
        }


        [Test]
        public async Task TestFindMergedAttributesByFullName()
        {
            var userModel = new UserInDatabaseModel();
            var changesetModel = new ChangesetModel(userModel);
            var attributeModel = new AttributeModel(new BaseAttributeModel(new PartitionModel(), new CIIDModel()));
            var model = new CIModel(attributeModel, new CIIDModel());
            var layerModel = new LayerModel();
            using var trans = ModelContextBuilder.BuildDeferred();
            var user = await DBSetup.SetupUser(userModel, trans);

            var ciid1 = await model.CreateCI(trans);
            var ciid2 = await model.CreateCI(trans);
            var layer1 = await layerModel.UpsertLayer("l1", trans);
            var layer2 = await layerModel.UpsertLayer("l2", trans);

            var layerset1 = new LayerSet(new string[] { layer2.ID, layer1.ID });

            var changeset1 = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
            var (a11, _) = await attributeModel.InsertAttribute("a1", new AttributeScalarValueText("textL1"), ciid1, layer1.ID, changeset1, new DataOriginV1(DataOriginType.Manual), trans);
            await attributeModel.InsertAttribute("a2", new AttributeScalarValueText("textL1"), ciid1, layer1.ID, changeset1, new DataOriginV1(DataOriginType.Manual), trans);

            var changeset2 = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
            var (a12, _) = await attributeModel.InsertAttribute("a1", new AttributeScalarValueText("textL2"), ciid1, layer2.ID, changeset2, new DataOriginV1(DataOriginType.Manual), trans);

            var changeset3 = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
            var (a13, _) = await attributeModel.InsertAttribute("a1", new AttributeScalarValueText("textL2"), ciid2, layer2.ID, changeset3, new DataOriginV1(DataOriginType.Manual), trans);
            await attributeModel.InsertAttribute("a3", new AttributeScalarValueText("textL2"), ciid2, layer2.ID, changeset3, new DataOriginV1(DataOriginType.Manual), trans);

            var a1 = await attributeModel.FindMergedAttributesByFullName("a1", new AllCIIDsSelection(), new LayerSet(layer1.ID, layer2.ID), trans, TimeThreshold.BuildLatest());
            a1.Should().BeEquivalentTo(new Dictionary<Guid, MergedCIAttribute>()
            {
                { ciid1, new MergedCIAttribute(a11, new List<string>() { layer1.ID, layer2.ID }) },
                { ciid2, new MergedCIAttribute(a13, new List<string>() { layer2.ID }) }
            }, options => options.WithStrictOrdering());
        }

        [Test]
        public async Task TestLayerSets()
        {
            var attributeModel = new AttributeModel(new BaseAttributeModel(new PartitionModel(), new CIIDModel()));
            var userModel = new UserInDatabaseModel();
            var changesetModel = new ChangesetModel(userModel);
            var model = new CIModel(attributeModel, new CIIDModel());
            var layerModel = new LayerModel();
            using var trans = ModelContextBuilder.BuildDeferred();
            var user = await DBSetup.SetupUser(userModel, trans);

            var ciid1 = await model.CreateCI(trans);
            var layer1 = await layerModel.UpsertLayer("l1", trans);
            var layer2 = await layerModel.UpsertLayer("l2", trans);

            var layerset1 = new LayerSet(new string[] { layer1.ID });
            var layerset2 = new LayerSet(new string[] { layer2.ID });
            var layerset3 = new LayerSet(new string[] { layer1.ID, layer2.ID });
            var layerset4 = new LayerSet(new string[] { layer2.ID, layer1.ID });

            var changeset = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
            await attributeModel.InsertAttribute("a1", new AttributeScalarValueText("textL1"), ciid1, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
            await attributeModel.InsertAttribute("a1", new AttributeScalarValueText("textL2"), ciid1, layer2.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);

            var a1 = (await attributeModel.GetMergedAttributes(SpecificCIIDsSelection.Build(ciid1), AllAttributeSelection.Instance, layerset1, trans, TimeThreshold.BuildLatest())).SelectMany(t => t.Value.Values);
            Assert.AreEqual(1, a1.Count());
            Assert.AreEqual(new AttributeScalarValueText("textL1"), a1.First().Attribute.Value);

            var a2 = (await attributeModel.GetMergedAttributes(SpecificCIIDsSelection.Build(ciid1), AllAttributeSelection.Instance, layerset2, trans, TimeThreshold.BuildLatest())).SelectMany(t => t.Value.Values);
            Assert.AreEqual(1, a2.Count());
            Assert.AreEqual(new AttributeScalarValueText("textL2"), a2.First().Attribute.Value);

            var a3 = (await attributeModel.GetMergedAttributes(SpecificCIIDsSelection.Build(ciid1), AllAttributeSelection.Instance, layerset3, trans, TimeThreshold.BuildLatest())).SelectMany(t => t.Value.Values);
            Assert.AreEqual(1, a3.Count());
            Assert.AreEqual(new AttributeScalarValueText("textL1"), a3.First().Attribute.Value);

            var a4 = (await attributeModel.GetMergedAttributes(SpecificCIIDsSelection.Build(ciid1), AllAttributeSelection.Instance, layerset4, trans, TimeThreshold.BuildLatest())).SelectMany(t => t.Value.Values);
            Assert.AreEqual(1, a4.Count());
            a4.First().LayerStackIDs.Should().BeEquivalentTo(new string[] { layer2.ID, layer1.ID }, config => config.WithStrictOrdering());
            Assert.AreEqual(new AttributeScalarValueText("textL2"), a4.First().Attribute.Value);
        }

        [Test]
        public async Task TestRemoveShowsLayerBelow()
        {
            var attributeModel = new AttributeModel(new BaseAttributeModel(new PartitionModel(), new CIIDModel()));
            var userModel = new UserInDatabaseModel();
            var changesetModel = new ChangesetModel(userModel);
            var model = new CIModel(attributeModel, new CIIDModel());
            var layerModel = new LayerModel();
            var transI = ModelContextBuilder.BuildImmediate();
            var user = await DBSetup.SetupUser(userModel, transI);

            var ciid1 = await model.CreateCI(transI);
            var layer1 = await layerModel.UpsertLayer("l1", transI);
            var layer2 = await layerModel.UpsertLayer("l2", transI);
            var layerset1 = new LayerSet(new string[] { layer2.ID, layer1.ID });

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset1 = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
                await attributeModel.InsertAttribute("a1", new AttributeScalarValueText("textL1"), ciid1, layer1.ID, changeset1, new DataOriginV1(DataOriginType.Manual), trans);

                var changeset2 = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
                await attributeModel.InsertAttribute("a1", new AttributeScalarValueText("textL2"), ciid1, layer2.ID, changeset2, new DataOriginV1(DataOriginType.Manual), trans);

                trans.Commit();
            }

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset3 = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
                await attributeModel.RemoveAttribute("a1", ciid1, layer2.ID, changeset3, new DataOriginV1(DataOriginType.Manual), trans, MaskHandlingForRemovalApplyNoMask.Instance);
                trans.Commit();
            }

            var a1 = (await attributeModel.GetMergedAttributes(SpecificCIIDsSelection.Build(ciid1), AllAttributeSelection.Instance, layerset1, transI, TimeThreshold.BuildLatest())).Values.First();
            Assert.AreEqual(1, a1.Count()); // layerID1 shines through deleted
            Assert.AreEqual(new AttributeScalarValueText("textL1"), a1.First().Value.Attribute.Value);
        }
    }
}
