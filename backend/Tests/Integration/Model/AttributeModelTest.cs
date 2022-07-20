using FluentAssertions;
using Moq;
using Npgsql;
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

namespace Tests.Integration.Model
{
    class AttributeModelTest : DIServicedTestBase
    {
        [Test]
        public async Task TestAddingUpdatingRemovingAndRenewingOfAttributes()
        {
            var transI = ModelContextBuilder.BuildImmediate();
            Guid ciid1;
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                ciid1 = await GetService<ICIModel>().CreateCI(trans);
                trans.Commit();
            }

            // TODO: this shouldn't be tested here
            Assert.ThrowsAsync<PostgresException>(async () => await GetService<ICIModel>().CreateCI(ciid1, transI)); // cannot add same identity twice

            string layerID1;
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var (layer1, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("l1", trans);
                layerID1 = layer1.ID;
                Assert.AreEqual("l1", layerID1);
                trans.Commit();
            }

            var layerset = await GetService<ILayerModel>().BuildLayerSet(new string[] { "l1" }, transI);

            TimeThreshold insertTime1;
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = await CreateChangesetProxy();
                await GetService<IAttributeModel>().InsertAttribute("a1", new AttributeScalarValueText("text1"), ciid1, layerID1, changeset, new DataOriginV1(DataOriginType.Manual), trans, OtherLayersValueHandlingForceWrite.Instance);
                
                trans.Commit();

                insertTime1 = changeset.TimeThreshold;
            }

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = await CreateChangesetProxy();
                await GetService<IAttributeModel>().InsertAttribute("a1", new AttributeScalarValueText("text2"), ciid1, layerID1, changeset, new DataOriginV1(DataOriginType.Manual), trans, OtherLayersValueHandlingForceWrite.Instance);
                
                var a1 = (await GetService<IAttributeModel>().GetMergedAttributes(SpecificCIIDsSelection.Build(ciid1), AllAttributeSelection.Instance, layerset, trans, TimeThreshold.BuildLatest(), GeneratedDataHandlingInclude.Instance)).Values.First();
                Assert.AreEqual(1, a1.Count);
                var aa1 = a1.First().Value;
                Assert.AreEqual(ciid1, aa1.Attribute.CIID);
                Assert.AreEqual("a1", aa1.Attribute.Name);
                Assert.AreEqual(new AttributeScalarValueText("text2"), aa1.Attribute.Value);
                Assert.AreEqual((await changeset.GetChangeset(layerID1, new DataOriginV1(DataOriginType.Manual), trans)).ID, aa1.Attribute.ChangesetID);
                trans.Commit();
            }

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = await CreateChangesetProxy();
                await GetService<IAttributeModel>().RemoveAttribute("a1", ciid1, layerID1, changeset, new DataOriginV1(DataOriginType.Manual), trans, MaskHandlingForRemovalApplyNoMask.Instance);
                
                var a2 = (await GetService<IAttributeModel>().GetMergedAttributes(SpecificCIIDsSelection.Build(ciid1), AllAttributeSelection.Instance, layerset, trans, TimeThreshold.BuildLatest(), GeneratedDataHandlingInclude.Instance)).Values;
                Assert.AreEqual(0, a2.Count);

                // compare fetching merged vs non-merged
                var ma3 = await GetService<IAttributeModel>().GetMergedAttributes(SpecificCIIDsSelection.Build(ciid1), AllAttributeSelection.Instance, layerset, trans, TimeThreshold.BuildLatest(), GeneratedDataHandlingInclude.Instance);
                var a3 = await GetService<IBaseAttributeModel>().GetAttributes(SpecificCIIDsSelection.Build(ciid1), AllAttributeSelection.Instance, layerset.LayerIDs, trans, TimeThreshold.BuildLatest(), GeneratedDataHandlingInclude.Instance);
                Assert.AreEqual(0, ma3.Count);
                Assert.AreEqual(0, a3[0].Count);

                trans.Commit();
            }

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = await CreateChangesetProxy();
                await GetService<IAttributeModel>().InsertAttribute("a1", new AttributeScalarValueText("text3"), ciid1, layerID1, changeset, new DataOriginV1(DataOriginType.Manual), trans, OtherLayersValueHandlingForceWrite.Instance);
                
                var a4 = (await GetService<IAttributeModel>().GetMergedAttributes(SpecificCIIDsSelection.Build(ciid1), AllAttributeSelection.Instance, layerset, trans, TimeThreshold.BuildLatest(), GeneratedDataHandlingInclude.Instance)).Values.First();
                Assert.AreEqual(1, a4.Count);
                var aa4 = a4.First().Value;
                Assert.AreEqual(new AttributeScalarValueText("text3"), aa4.Attribute.Value);
            }

            // read at time of insertTime1
            using (var trans = ModelContextBuilder.BuildImmediate())
            {
                var atTime = TimeThreshold.BuildAtTime(insertTime1.Time);
                var a1InPast = (await GetService<IAttributeModel>().GetMergedAttributes(SpecificCIIDsSelection.Build(ciid1), AllAttributeSelection.Instance, layerset, trans, atTime, GeneratedDataHandlingInclude.Instance)).Values.First();
                Assert.AreEqual(1, a1InPast.Count);
                var aa = a1InPast.First().Value;
                Assert.AreEqual(new AttributeScalarValueText("text1"), aa.Attribute.Value);
            }
        }


        [Test]
        public async Task TestAttributeValueMultiplicities()
        {
            var transI = ModelContextBuilder.BuildImmediate();
            var ciid1 = await GetService<ICIModel>().CreateCI(transI);
            var (layer1, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("l1", transI);

            var layerset1 = new LayerSet(new string[] { layer1.ID });

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = await CreateChangesetProxy();
                await GetService<IAttributeModel>().InsertAttribute("a1", AttributeArrayValueText.BuildFromString(new string[] { "a", "b", "c" }), ciid1, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans, OtherLayersValueHandlingForceWrite.Instance);
                var a1 = (await GetService<IAttributeModel>().GetMergedAttributes(SpecificCIIDsSelection.Build(ciid1), AllAttributeSelection.Instance, layerset1, trans, TimeThreshold.BuildLatest(), GeneratedDataHandlingInclude.Instance)).Values.First();
                Assert.AreEqual(1, a1.Count);
                Assert.AreEqual(AttributeArrayValueText.BuildFromString(new string[] { "a", "b", "c" }), a1.First().Value.Attribute.Value);
            }

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = await CreateChangesetProxy();
                await GetService<IAttributeModel>().InsertAttribute("a1", AttributeArrayValueText.BuildFromString(new string[] { "a,", "b,b", ",c", "\\d", "\\,e" }), ciid1, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans, OtherLayersValueHandlingForceWrite.Instance);
                var a1 = (await GetService<IAttributeModel>().GetMergedAttributes(SpecificCIIDsSelection.Build(ciid1), AllAttributeSelection.Instance, layerset1, trans, TimeThreshold.BuildLatest(), GeneratedDataHandlingInclude.Instance)).Values.First();
                Assert.AreEqual(1, a1.Count);
                Assert.AreEqual(AttributeArrayValueText.BuildFromString(new string[] { "a,", "b,b", ",c", "\\d", "\\,e" }), a1.First().Value.Attribute.Value);
            }

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = await CreateChangesetProxy();
                await GetService<IAttributeModel>().InsertAttribute("a1", AttributeArrayValueInteger.Build(new long[] { 1, 2, 3, 4 }), ciid1, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans, OtherLayersValueHandlingForceWrite.Instance);
                var a1 = (await GetService<IAttributeModel>().GetMergedAttributes(SpecificCIIDsSelection.Build(ciid1), AllAttributeSelection.Instance, layerset1, trans, TimeThreshold.BuildLatest(), GeneratedDataHandlingInclude.Instance)).Values.First();
                Assert.AreEqual(1, a1.Count);
                Assert.AreEqual(AttributeArrayValueInteger.Build(new long[] { 1, 2, 3, 4 }), a1.First().Value.Attribute.Value);
            }
        }


        [Test]
        public async Task TestStringBasedAttributeValueTypes()
        {
            var transI = ModelContextBuilder.BuildImmediate();

            var ciid1 = await GetService<ICIModel>().CreateCI(transI);
            var (layer1, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("l1", transI);

            var layerset1 = new LayerSet(new string[] { layer1.ID });

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = await CreateChangesetProxy();
                await GetService<IAttributeModel>().InsertAttribute("a1", AttributeArrayValueInteger.Build(new long[] { 4, 3, -2 }), ciid1, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans, OtherLayersValueHandlingForceWrite.Instance);
                var a1 = (await GetService<IAttributeModel>().GetMergedAttributes(SpecificCIIDsSelection.Build(ciid1), AllAttributeSelection.Instance, layerset1, trans, TimeThreshold.BuildLatest(), GeneratedDataHandlingInclude.Instance)).Values.First();
                Assert.AreEqual(1, a1.Count);
                Assert.AreEqual(AttributeArrayValueInteger.Build(new long[] { 4, 3, -2 }), a1.First().Value.Attribute.Value);
            }

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = await CreateChangesetProxy();
                await GetService<IAttributeModel>().InsertAttribute("a1", AttributeArrayValueJSON.BuildFromString(new string[] { "{}", "{\"foo\":\"var\" }" }, true), ciid1, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans, OtherLayersValueHandlingForceWrite.Instance);
                var a1 = (await GetService<IAttributeModel>().GetMergedAttributes(SpecificCIIDsSelection.Build(ciid1), AllAttributeSelection.Instance, layerset1, trans, TimeThreshold.BuildLatest(), GeneratedDataHandlingInclude.Instance)).Values.First();
                Assert.AreEqual(1, a1.Count);
                Assert.AreEqual(AttributeArrayValueJSON.BuildFromString(new string[] { "{}", "{\"foo\":\"var\" }" }, true), a1.First().Value.Attribute.Value);
            }
        }

        [Test]
        public async Task TestBinaryBasedAttributeValueTypes()
        {
            var transI = ModelContextBuilder.BuildImmediate();

            var ciid1 = await GetService<ICIModel>().CreateCI(transI);
            var (layer1, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("l1", transI);

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
                var changeset = await CreateChangesetProxy();
                await GetService<IAttributeModel>().InsertAttribute("a1", scalarImage, ciid1, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans, OtherLayersValueHandlingForceWrite.Instance);
                var a1 = (await GetService<IAttributeModel>().GetMergedAttributes(SpecificCIIDsSelection.Build(ciid1), AllAttributeSelection.Instance, layerset1, trans, TimeThreshold.BuildLatest(), GeneratedDataHandlingInclude.Instance)).Values.First();
                Assert.AreEqual(1, a1.Count);
                Assert.AreEqual(scalarImage, a1.First().Value.Attribute.Value);
                var returnedProxy = (a1.First().Value.Attribute.Value as AttributeScalarValueImage)!.Value;
                Assert.IsFalse(returnedProxy.HasFullData());
                Assert.AreEqual(returnedProxy.MimeType, "testmimetype");
            }
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = await CreateChangesetProxy();
                await GetService<IAttributeModel>().InsertAttribute("a1", imageArray, ciid1, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans, OtherLayersValueHandlingForceWrite.Instance);
                var a1 = (await GetService<IAttributeModel>().GetMergedAttributes(SpecificCIIDsSelection.Build(ciid1), AllAttributeSelection.Instance, layerset1, trans, TimeThreshold.BuildLatest(), GeneratedDataHandlingInclude.Instance)).Values.First();
                Assert.AreEqual(1, a1.Count);
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
            using var trans = ModelContextBuilder.BuildDeferred();

            var ciid1 = await GetService<ICIModel>().CreateCI(trans);
            var (layer1, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("l1", trans);

            var layerset1 = new LayerSet(new string[] { layer1.ID });

            var changeset1 = await CreateChangesetProxy();
            var changed1 = await GetService<IAttributeModel>().InsertAttribute("a1", new AttributeScalarValueText("textL1"), ciid1, layer1.ID, changeset1, new DataOriginV1(DataOriginType.Manual), trans, OtherLayersValueHandlingForceWrite.Instance);
            Assert.IsTrue(changed1);

            var changeset2 = await CreateChangesetProxy();
            var changed2 = await GetService<IAttributeModel>().InsertAttribute("a1", new AttributeScalarValueText("textL1"), ciid1, layer1.ID, changeset2, new DataOriginV1(DataOriginType.Manual), trans, OtherLayersValueHandlingForceWrite.Instance);
            Assert.IsFalse(changed2);

            var a1 = (await GetService<IAttributeModel>().GetMergedAttributes(SpecificCIIDsSelection.Build(ciid1), AllAttributeSelection.Instance, layerset1, trans, TimeThreshold.BuildLatest(), GeneratedDataHandlingInclude.Instance)).Values.First();
            Assert.AreEqual(1, a1.Count);
        }

        [Test]
        public async Task TestGetAttributesWithNameRegex()
        {
            using var trans = ModelContextBuilder.BuildDeferred();

            var ciid1 = await GetService<ICIModel>().CreateCI(trans);
            var ciid2 = await GetService<ICIModel>().CreateCI(trans);
            var (layer1, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("l1", trans);
            var (layer2, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("l2", trans);

            var layerset1 = new LayerSet(new string[] { layer2.ID, layer1.ID });

            var changeset1 = await CreateChangesetProxy();
            await GetService<IAttributeModel>().InsertAttribute("a1", new AttributeScalarValueText("textL1"), ciid1, layer1.ID, changeset1, new DataOriginV1(DataOriginType.Manual), trans, OtherLayersValueHandlingForceWrite.Instance);
            await GetService<IAttributeModel>().InsertAttribute("a2", new AttributeScalarValueText("textL1"), ciid1, layer1.ID, changeset1, new DataOriginV1(DataOriginType.Manual), trans, OtherLayersValueHandlingForceWrite.Instance);

            var changeset2 = await CreateChangesetProxy();
            await GetService<IAttributeModel>().InsertAttribute("a1", new AttributeScalarValueText("textL2"), ciid1, layer2.ID, changeset2, new DataOriginV1(DataOriginType.Manual), trans, OtherLayersValueHandlingForceWrite.Instance);

            var changeset3 = await CreateChangesetProxy();
            await GetService<IAttributeModel>().InsertAttribute("a1", new AttributeScalarValueText("textL2"), ciid2, layer2.ID, changeset3, new DataOriginV1(DataOriginType.Manual), trans, OtherLayersValueHandlingForceWrite.Instance);
            await GetService<IAttributeModel>().InsertAttribute("a3", new AttributeScalarValueText("textL2"), ciid2, layer2.ID, changeset3, new DataOriginV1(DataOriginType.Manual), trans, OtherLayersValueHandlingForceWrite.Instance);
        }

        [Test]
        public async Task TestBulkReplace()
        {
            using var trans = ModelContextBuilder.BuildDeferred();

            var ciid1 = await GetService<ICIModel>().CreateCI(trans);
            var ciid2 = await GetService<ICIModel>().CreateCI(trans);
            var (layer1, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("l1", trans);

            var layerset1 = new LayerSet(new string[] { layer1.ID });

            var changeset1 = await CreateChangesetProxy();
            await GetService<IAttributeModel>().InsertAttribute("prefix1.a1", new AttributeScalarValueText("textL1"), ciid1, layer1.ID, changeset1, new DataOriginV1(DataOriginType.Manual), trans, OtherLayersValueHandlingForceWrite.Instance);
            await GetService<IAttributeModel>().InsertAttribute("prefix1.a2", new AttributeScalarValueText("textL1"), ciid1, layer1.ID, changeset1, new DataOriginV1(DataOriginType.Manual), trans, OtherLayersValueHandlingForceWrite.Instance);

            var changeset2 = await CreateChangesetProxy();
            await GetService<IAttributeModel>().InsertAttribute("prefix1.a1", new AttributeScalarValueText("textL2"), ciid2, layer1.ID, changeset2, new DataOriginV1(DataOriginType.Manual), trans, OtherLayersValueHandlingForceWrite.Instance);
            await GetService<IAttributeModel>().InsertAttribute("prefix2.a1", new AttributeScalarValueText("textL2"), ciid2, layer1.ID, changeset2, new DataOriginV1(DataOriginType.Manual), trans, OtherLayersValueHandlingForceWrite.Instance);
            await GetService<IAttributeModel>().InsertAttribute("prefix1.a3", new AttributeScalarValueText("textL2"), ciid2, layer1.ID, changeset2, new DataOriginV1(DataOriginType.Manual), trans, OtherLayersValueHandlingForceWrite.Instance);

            trans.Commit();

            using var trans2 = ModelContextBuilder.BuildDeferred();
            var changeset3 = await CreateChangesetProxy();
            await GetService<IAttributeModel>().BulkReplaceAttributes(new BulkCIAttributeDataLayerScope(layer1.ID, new BulkCIAttributeDataLayerScope.Fragment[] {
                new BulkCIAttributeDataLayerScope.Fragment("prefix1.a1", new AttributeScalarValueText("textNew"), ciid1),
                new BulkCIAttributeDataLayerScope.Fragment("prefix1.a4", new AttributeScalarValueText("textNew"), ciid2),
                new BulkCIAttributeDataLayerScope.Fragment("prefix1.a2", new AttributeScalarValueText("textNew"), ciid2),
            }), changeset3, new DataOriginV1(DataOriginType.Manual), trans2, MaskHandlingForRemovalApplyNoMask.Instance, OtherLayersValueHandlingForceWrite.Instance);
            trans2.Commit();

            using var trans3 = ModelContextBuilder.BuildImmediate();
            var a1 = (await GetService<IBaseAttributeModel>().GetAttributes(AllCIIDsSelection.Instance, AllAttributeSelection.Instance, new string[] { layer1.ID }, trans: trans3, atTime: TimeThreshold.BuildLatest(), GeneratedDataHandlingInclude.Instance)).SelectMany(t => t.Values.SelectMany(t => t.Values));
            Assert.AreEqual(3, a1.Count());
            Assert.AreEqual(1, a1.Where(a => a.Name == "prefix1.a2").Count());
            Assert.AreEqual(1, a1.Where(a => a.Name == "prefix1.a1").Count());
            Assert.AreEqual(1, a1.Where(a => a.Name == "prefix1.a4").Count());
        }


        [Test]
        public async Task TestFindMergedAttributesByFullName()
        {
            using var trans = ModelContextBuilder.BuildDeferred();

            var ciid1 = await GetService<ICIModel>().CreateCI(trans);
            var ciid2 = await GetService<ICIModel>().CreateCI(trans);
            var (layer1, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("l1", trans);
            var (layer2, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("l2", trans);

            var layerset1 = new LayerSet(new string[] { layer2.ID, layer1.ID });

            var changeset1 = await CreateChangesetProxy();
            await GetService<IAttributeModel>().InsertAttribute("a1", new AttributeScalarValueText("textL1"), ciid1, layer1.ID, changeset1, new DataOriginV1(DataOriginType.Manual), trans, OtherLayersValueHandlingForceWrite.Instance);
            await GetService<IAttributeModel>().InsertAttribute("a2", new AttributeScalarValueText("textL1"), ciid1, layer1.ID, changeset1, new DataOriginV1(DataOriginType.Manual), trans, OtherLayersValueHandlingForceWrite.Instance);

            var changeset2 = await CreateChangesetProxy();
            await GetService<IAttributeModel>().InsertAttribute("a1", new AttributeScalarValueText("textL2"), ciid1, layer2.ID, changeset2, new DataOriginV1(DataOriginType.Manual), trans, OtherLayersValueHandlingForceWrite.Instance);

            var changeset3 = await CreateChangesetProxy();
            await GetService<IAttributeModel>().InsertAttribute("a1", new AttributeScalarValueText("textL2"), ciid2, layer2.ID, changeset3, new DataOriginV1(DataOriginType.Manual), trans, OtherLayersValueHandlingForceWrite.Instance);
            await GetService<IAttributeModel>().InsertAttribute("a3", new AttributeScalarValueText("textL2"), ciid2, layer2.ID, changeset3, new DataOriginV1(DataOriginType.Manual), trans, OtherLayersValueHandlingForceWrite.Instance);

            var a1 = await GetService<IAttributeModel>().FindMergedAttributesByFullName("a1", AllCIIDsSelection.Instance, new LayerSet(layer1.ID, layer2.ID), trans, TimeThreshold.BuildLatest());
            a1.Keys.Should().BeEquivalentTo(new List<Guid>() { ciid1, ciid2 }, options => options.WithStrictOrdering());
            a1.Values.Select(a => a.Attribute.Value).Should().BeEquivalentTo(new List<IAttributeValue>()
            {
                new AttributeScalarValueText("textL1"),
                new AttributeScalarValueText("textL2")
            }, options => options.WithStrictOrdering());
        }

        [Test]
        public async Task TestLayerSets()
        {
            using var trans = ModelContextBuilder.BuildDeferred();

            var ciid1 = await GetService<ICIModel>().CreateCI(trans);
            var (layer1, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("l1", trans);
            var (layer2, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("l2", trans);

            var layerset1 = new LayerSet(new string[] { layer1.ID });
            var layerset2 = new LayerSet(new string[] { layer2.ID });
            var layerset3 = new LayerSet(new string[] { layer1.ID, layer2.ID });
            var layerset4 = new LayerSet(new string[] { layer2.ID, layer1.ID });

            var changeset = await CreateChangesetProxy();
            await GetService<IAttributeModel>().InsertAttribute("a1", new AttributeScalarValueText("textL1"), ciid1, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans, OtherLayersValueHandlingForceWrite.Instance);
            await GetService<IAttributeModel>().InsertAttribute("a1", new AttributeScalarValueText("textL2"), ciid1, layer2.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans, OtherLayersValueHandlingForceWrite.Instance);

            var a1 = (await GetService<IAttributeModel>().GetMergedAttributes(SpecificCIIDsSelection.Build(ciid1), AllAttributeSelection.Instance, layerset1, trans, TimeThreshold.BuildLatest(), GeneratedDataHandlingInclude.Instance)).SelectMany(t => t.Value.Values);
            Assert.AreEqual(1, a1.Count());
            Assert.AreEqual(new AttributeScalarValueText("textL1"), a1.First().Attribute.Value);

            var a2 = (await GetService<IAttributeModel>().GetMergedAttributes(SpecificCIIDsSelection.Build(ciid1), AllAttributeSelection.Instance, layerset2, trans, TimeThreshold.BuildLatest(), GeneratedDataHandlingInclude.Instance)).SelectMany(t => t.Value.Values);
            Assert.AreEqual(1, a2.Count());
            Assert.AreEqual(new AttributeScalarValueText("textL2"), a2.First().Attribute.Value);

            var a3 = (await GetService<IAttributeModel>().GetMergedAttributes(SpecificCIIDsSelection.Build(ciid1), AllAttributeSelection.Instance, layerset3, trans, TimeThreshold.BuildLatest(), GeneratedDataHandlingInclude.Instance)).SelectMany(t => t.Value.Values);
            Assert.AreEqual(1, a3.Count());
            Assert.AreEqual(new AttributeScalarValueText("textL1"), a3.First().Attribute.Value);

            var a4 = (await GetService<IAttributeModel>().GetMergedAttributes(SpecificCIIDsSelection.Build(ciid1), AllAttributeSelection.Instance, layerset4, trans, TimeThreshold.BuildLatest(), GeneratedDataHandlingInclude.Instance)).SelectMany(t => t.Value.Values);
            Assert.AreEqual(1, a4.Count());
            a4.First().LayerStackIDs.Should().BeEquivalentTo(new string[] { layer2.ID, layer1.ID }, config => config.WithStrictOrdering());
            Assert.AreEqual(new AttributeScalarValueText("textL2"), a4.First().Attribute.Value);
        }

        [Test]
        public async Task TestRemoveShowsLayerBelow()
        {
            var transI = ModelContextBuilder.BuildImmediate();

            var ciid1 = await GetService<ICIModel>().CreateCI(transI);
            var (layer1, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("l1", transI);
            var (layer2, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("l2", transI);
            var layerset1 = new LayerSet(new string[] { layer2.ID, layer1.ID });

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset1 = await CreateChangesetProxy();
                await GetService<IAttributeModel>().InsertAttribute("a1", new AttributeScalarValueText("textL1"), ciid1, layer1.ID, changeset1, new DataOriginV1(DataOriginType.Manual), trans, OtherLayersValueHandlingForceWrite.Instance);

                var changeset2 = await CreateChangesetProxy();
                await GetService<IAttributeModel>().InsertAttribute("a1", new AttributeScalarValueText("textL2"), ciid1, layer2.ID, changeset2, new DataOriginV1(DataOriginType.Manual), trans, OtherLayersValueHandlingForceWrite.Instance);

                trans.Commit();
            }

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset3 = await CreateChangesetProxy();
                await GetService<IAttributeModel>().RemoveAttribute("a1", ciid1, layer2.ID, changeset3, new DataOriginV1(DataOriginType.Manual), trans, MaskHandlingForRemovalApplyNoMask.Instance);
                trans.Commit();
            }

            var a1 = (await GetService<IAttributeModel>().GetMergedAttributes(SpecificCIIDsSelection.Build(ciid1), AllAttributeSelection.Instance, layerset1, transI, TimeThreshold.BuildLatest(), GeneratedDataHandlingInclude.Instance)).Values.First();
            Assert.AreEqual(1, a1.Count); // layerID1 shines through deleted
            Assert.AreEqual(new AttributeScalarValueText("textL1"), a1.First().Value.Attribute.Value);
        }


        [Test]
        public async Task TestAttributeValueDouble()
        {
            var transI = ModelContextBuilder.BuildImmediate();
            Guid ciid1;
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                ciid1 = await GetService<ICIModel>().CreateCI(trans);
                trans.Commit();
            }

            string layerID1;
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var (layer1, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("l1", trans);
                layerID1 = layer1.ID;
                Assert.AreEqual("l1", layerID1);
                trans.Commit();
            }

            var layerset = await GetService<ILayerModel>().BuildLayerSet(new string[] { "l1" }, transI);

            var a1Value = new AttributeScalarValueDouble(1.1);
            var a2Value = AttributeArrayValueDouble.Build(new double[] { -0.0, 2.1, double.MaxValue, double.MinValue, 0, -double.Epsilon });

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = await CreateChangesetProxy();
                await GetService<IAttributeModel>().InsertAttribute("a1", a1Value, ciid1, layerID1, changeset, new DataOriginV1(DataOriginType.Manual), trans, OtherLayersValueHandlingForceWrite.Instance);
                await GetService<IAttributeModel>().InsertAttribute("a2", a2Value, ciid1, layerID1, changeset, new DataOriginV1(DataOriginType.Manual), trans, OtherLayersValueHandlingForceWrite.Instance);

                trans.Commit();
            }

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var cis = await GetService<IAttributeModel>().GetMergedAttributes(SpecificCIIDsSelection.Build(ciid1), AllAttributeSelection.Instance, layerset, trans, TimeThreshold.BuildLatest(), GeneratedDataHandlingInclude.Instance);
                Assert.AreEqual(1, cis.Count);
                var attributes = cis.First().Value;
                Assert.AreEqual(2, attributes.Count);
                
                Assert.AreEqual(a1Value, attributes["a1"].Attribute.Value);
                Assert.AreEqual(a2Value, attributes["a2"].Attribute.Value);
                trans.Commit();
            }
        }



        [Test]
        public async Task TestAttributeValueBoolean()
        {
            var transI = ModelContextBuilder.BuildImmediate();
            Guid ciid1;
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                ciid1 = await GetService<ICIModel>().CreateCI(trans);
                trans.Commit();
            }

            string layerID1;
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var (layer1, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("l1", trans);
                layerID1 = layer1.ID;
                Assert.AreEqual("l1", layerID1);
                trans.Commit();
            }

            var layerset = await GetService<ILayerModel>().BuildLayerSet(new string[] { "l1" }, transI);

            var a1Value = new AttributeScalarValueBoolean(true);
            var a2Value = AttributeArrayValueBoolean.Build(new bool[] { false, true, true, false });

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = await CreateChangesetProxy();
                await GetService<IAttributeModel>().InsertAttribute("a1", a1Value, ciid1, layerID1, changeset, new DataOriginV1(DataOriginType.Manual), trans, OtherLayersValueHandlingForceWrite.Instance);
                await GetService<IAttributeModel>().InsertAttribute("a2", a2Value, ciid1, layerID1, changeset, new DataOriginV1(DataOriginType.Manual), trans, OtherLayersValueHandlingForceWrite.Instance);

                trans.Commit();
            }

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var cis = await GetService<IAttributeModel>().GetMergedAttributes(SpecificCIIDsSelection.Build(ciid1), AllAttributeSelection.Instance, layerset, trans, TimeThreshold.BuildLatest(), GeneratedDataHandlingInclude.Instance);
                Assert.AreEqual(1, cis.Count);
                var attributes = cis.First().Value;
                Assert.AreEqual(2, attributes.Count);

                Assert.AreEqual(a1Value, attributes["a1"].Attribute.Value);
                Assert.AreEqual(a2Value, attributes["a2"].Attribute.Value);
                trans.Commit();
            }
        }
    }
}
