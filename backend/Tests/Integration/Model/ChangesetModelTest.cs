using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.Model;
using Omnikeeper.Model.Decorators;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Omnikeeper.Base.Model.IChangesetModel;

namespace Tests.Integration.Model
{
    class ChangesetModelTest : DBBackedTestBase
    {
        [Test]
        public async Task TestBasics()
        {
            var userModel = new UserInDatabaseModel();
            var changesetModel = new ChangesetModel(userModel);
            var baseAttributeModel = new BaseAttributeModel(new PartitionModel());
            var attributeModel = new AttributeModel(baseAttributeModel);
            var baseRelationModel = new BaseRelationModel(new PartitionModel());
            var relationModel = new RelationModel(baseRelationModel);
            var ciModel = new CIModel(attributeModel, new CIIDModel());
            //var baseConfigurationModel = new BaseConfigurationModel(NullLogger<BaseConfigurationModel>.Instance);
            //var effectiveTraitModel = new EffectiveTraitModel(ciModel, attributeModel, relationModel, null, NullLogger<EffectiveTraitModel>.Instance);
            //var predicateModel = new PredicateModel(effectiveTraitModel, ciModel, baseAttributeModel);
            var layerModel = new LayerModel();

            using var trans1 = ModelContextBuilder.BuildDeferred();
            var user = await DBSetup.SetupUser(userModel, trans1);
            var ciid1 = await ciModel.CreateCI(trans1);
            var ciid2 = await ciModel.CreateCI(trans1);
            var ciid3 = await ciModel.CreateCI(trans1);
            trans1.Commit();

            var t1 = DateTimeOffset.Now;

            using var trans2 = ModelContextBuilder.BuildDeferred();
            var layer1 = await layerModel.UpsertLayer("l1", trans2);
            var layerset = new LayerSet(new string[] { layer1.ID });
            var changeset1 = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
            await attributeModel.InsertAttribute("a1", new AttributeScalarValueText("textL1"), ciid2, layer1.ID, changeset1, new DataOriginV1(DataOriginType.Manual), trans2);
            trans2.Commit();

            Thread.Sleep(500);

            using var trans3 = ModelContextBuilder.BuildDeferred();
            var changeset2 = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
            await attributeModel.InsertAttribute("a2", new AttributeScalarValueText("textL1"), ciid3, layer1.ID, changeset2, new DataOriginV1(DataOriginType.Manual), trans3);
            trans3.Commit();

            var t2 = DateTimeOffset.Now;

            using var trans4 = ModelContextBuilder.BuildDeferred();
            var changeset3 = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
            await attributeModel.InsertAttribute("a3", new AttributeScalarValueText("textL1"), ciid3, layer1.ID, changeset3, new DataOriginV1(DataOriginType.Manual), trans4);
            trans4.Commit();

            var t3 = DateTimeOffset.Now;

            using var transI = ModelContextBuilder.BuildImmediate();
            var changesets = await changesetModel.GetChangesetsInTimespan(t1, t2, layerset, new ChangesetSelectionAllCIs(), transI);
            Assert.AreEqual(2, changesets.Count());

            var changesets2 = await changesetModel.GetChangesetsInTimespan(t1, t3, layerset, ChangesetSelectionMultipleCIs.Build(ciid3), transI);
            Assert.AreEqual(2, changesets2.Count());

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                await attributeModel.InsertAttribute("a3", new AttributeScalarValueText("textL1"), ciid2, layer1.ID, changeset3, new DataOriginV1(DataOriginType.Manual), trans);
                trans.Commit();
            }
            var t4 = DateTimeOffset.Now;

            var changesets3 = await changesetModel.GetChangesetsInTimespan(t1, t4, layerset, new ChangesetSelectionAllCIs(), transI);
            Assert.AreEqual(3, changesets3.Count());
            var changesets4 = await changesetModel.GetChangesetsInTimespan(t1, t4, layerset, new ChangesetSelectionAllCIs(), transI, 2);
            Assert.AreEqual(2, changesets4.Count());
            var changesets5 = await changesetModel.GetChangesetsInTimespan(t1, t4, layerset, ChangesetSelectionMultipleCIs.Build(ciid2), transI, 1);
            Assert.AreEqual(1, changesets5.Count());
        }



        [Test]
        public async Task TestRelations()
        {
            var userModel = new UserInDatabaseModel();
            var changesetModel = new ChangesetModel(userModel);
            var baseAttributeModel = new BaseAttributeModel(new PartitionModel());
            var attributeModel = new AttributeModel(baseAttributeModel);
            var baseRelationModel = new BaseRelationModel(new PartitionModel());
            var relationModel = new RelationModel(baseRelationModel);
            var ciModel = new CIModel(attributeModel, new CIIDModel());
            //var baseConfigurationModel = new BaseConfigurationModel(NullLogger<BaseConfigurationModel>.Instance);
            //var effectiveTraitModel = new EffectiveTraitModel(ciModel, attributeModel, relationModel, null, NullLogger<EffectiveTraitModel>.Instance);
            //var predicateModel = new PredicateModel(baseConfigurationModel, effectiveTraitModel);
            var layerModel = new LayerModel();

            using var trans = ModelContextBuilder.BuildDeferred();
            var user = await DBSetup.SetupUser(userModel, trans);
            var ciid1 = await ciModel.CreateCI(trans);
            var ciid2 = await ciModel.CreateCI(trans);
            var predicateID1 = "predicate_1";
            var predicateID2 = "predicate_2";
            trans.Commit();

            var t1 = DateTimeOffset.Now;

            using var trans2 = ModelContextBuilder.BuildDeferred();
            var layer1 = await layerModel.UpsertLayer("l1", trans2);
            var layerset = new LayerSet(new string[] { layer1.ID });
            var changeset1 = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
            await relationModel.InsertRelation(ciid1, ciid2, predicateID1, layer1.ID, changeset1, new DataOriginV1(DataOriginType.Manual), trans2);
            trans2.Commit();

            Thread.Sleep(500);
            var t2 = DateTimeOffset.Now;

            using var trans3 = ModelContextBuilder.BuildDeferred();
            var changeset2 = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
            await relationModel.InsertRelation(ciid2, ciid1, predicateID2, layer1.ID, changeset2, new DataOriginV1(DataOriginType.Manual), trans3);
            trans3.Commit();

            Thread.Sleep(500);
            var t3 = DateTimeOffset.Now;

            using var transI = ModelContextBuilder.BuildImmediate();
            var changesets1 = await changesetModel.GetChangesetsInTimespan(t1, t2, layerset, ChangesetSelectionMultipleCIs.Build(ciid1), transI);
            Assert.AreEqual(1, changesets1.Count());

            var changesets2 = await changesetModel.GetChangesetsInTimespan(t1, t3, layerset, ChangesetSelectionMultipleCIs.Build(ciid1), transI);
            Assert.AreEqual(2, changesets2.Count());
        }

        [Test]
        public async Task DeleteEmptyTest()
        {
            var userModel = new UserInDatabaseModel();
            var changesetModel = new ChangesetModel(userModel);
            var baseAttributeModel = new BaseAttributeModel(new PartitionModel());
            var attributeModel = new AttributeModel(baseAttributeModel);
            var baseRelationModel = new BaseRelationModel(new PartitionModel());
            var relationModel = new RelationModel(baseRelationModel);
            var ciModel = new CIModel(attributeModel, new CIIDModel());
            var layerModel = new LayerModel();
            var baseAttributeRevisionistModel = new BaseAttributeRevisionistModel();

            using var trans1 = ModelContextBuilder.BuildDeferred();
            var user = await DBSetup.SetupUser(userModel, trans1);
            var ciid1 = await ciModel.CreateCI(trans1);
            var ciid2 = await ciModel.CreateCI(trans1);
            var layer1 = await layerModel.UpsertLayer("l1", trans1);
            var layer2 = await layerModel.UpsertLayer("l2", trans1);
            var layerset1 = new LayerSet(new string[] { layer1.ID });
            var changeset1 = new ChangesetProxy(user, TimeThreshold.BuildAtTime(DateTimeOffset.FromUnixTimeSeconds(100)), changesetModel);
            await attributeModel.InsertAttribute("a1", new AttributeScalarValueText("foo"), ciid1, layer1.ID, changeset1, new DataOriginV1(DataOriginType.Manual), trans1);
            trans1.Commit();

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                Assert.AreEqual(0, await changesetModel.DeleteEmptyChangesets(trans));
                trans.Commit();
            }

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                await baseAttributeRevisionistModel.DeleteAllAttributes(layer1.ID, trans);
                trans.Commit();
            }

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                Assert.AreEqual(1, await changesetModel.DeleteEmptyChangesets(trans));
                trans.Commit();
            }
        }


        [Test]
        [Obsolete]
        public async Task ArchiveOldTest()
        {
            var userModel = new UserInDatabaseModel();
            var changesetModel = new ChangesetModel(userModel);
            var baseAttributeModel = new BaseAttributeModel(new PartitionModel());
            var attributeModel = new AttributeModel(baseAttributeModel);
            var baseRelationModel = new BaseRelationModel(new PartitionModel());
            var relationModel = new RelationModel(baseRelationModel);
            var ciModel = new CIModel(attributeModel, new CIIDModel());
            var layerModel = new LayerModel();

            using var trans = ModelContextBuilder.BuildDeferred();
            var user = await DBSetup.SetupUser(userModel, trans);
            var ciid1 = await ciModel.CreateCI(trans);
            var ciid2 = await ciModel.CreateCI(trans);
            var predicateID1 = "predicate_1";
            trans.Commit();

            using var trans2 = ModelContextBuilder.BuildDeferred();
            var layer1 = await layerModel.UpsertLayer("l1", trans2);
            var layerset1 = new LayerSet(new string[] { layer1.ID });
            var changeset1 = new ChangesetProxy(user, TimeThreshold.BuildAtTime(DateTimeOffset.FromUnixTimeSeconds(100)), changesetModel);
            await relationModel.InsertRelation(ciid1, ciid2, predicateID1, layer1.ID, changeset1, new DataOriginV1(DataOriginType.Manual), trans2);
            trans2.Commit();

            using var transI = ModelContextBuilder.BuildImmediate();
            Assert.AreEqual(0, await changesetModel.ArchiveUnusedChangesetsOlderThan(DateTimeOffset.FromUnixTimeSeconds(50), transI));


            using var trans3 = ModelContextBuilder.BuildDeferred();
            var changeset2 = new ChangesetProxy(user, TimeThreshold.BuildAtTime(DateTimeOffset.FromUnixTimeSeconds(150)), changesetModel);
            await attributeModel.InsertAttribute("a1", new AttributeScalarValueText("foo"), ciid1, layer1.ID, changeset2, new DataOriginV1(DataOriginType.Manual), trans3);
            await attributeModel.InsertAttribute("a2", new AttributeScalarValueText("bar"), ciid1, layer1.ID, changeset2, new DataOriginV1(DataOriginType.Manual), trans3);
            trans3.Commit();

            Assert.AreEqual(0, await changesetModel.ArchiveUnusedChangesetsOlderThan(DateTimeOffset.FromUnixTimeSeconds(50), transI));

            Assert.AreEqual(0, await changesetModel.ArchiveUnusedChangesetsOlderThan(DateTimeOffset.FromUnixTimeSeconds(100), transI));

            // changeset1 is now old "enough", but still cannot be deleted because its relation is the latest
            Assert.AreEqual(0, await changesetModel.ArchiveUnusedChangesetsOlderThan(DateTimeOffset.FromUnixTimeSeconds(101), transI));

            // delete relation again
            using var trans4 = ModelContextBuilder.BuildDeferred();
            var changeset3 = new ChangesetProxy(user, TimeThreshold.BuildAtTime(DateTimeOffset.FromUnixTimeSeconds(200)), changesetModel);
            await relationModel.RemoveRelation(ciid1, ciid2, predicateID1, layer1.ID, changeset3, new DataOriginV1(DataOriginType.Manual), trans4);
            trans4.Commit();

            // changeset1 is now old "enough", and can be deleted
            Assert.AreEqual(1, await changesetModel.ArchiveUnusedChangesetsOlderThan(DateTimeOffset.FromUnixTimeSeconds(101), transI));

            // overwrite attribute a1
            using var trans5 = ModelContextBuilder.BuildDeferred();
            var changeset4 = new ChangesetProxy(user, TimeThreshold.BuildAtTime(DateTimeOffset.FromUnixTimeSeconds(200)), changesetModel);
            await attributeModel.InsertAttribute("a1", new AttributeScalarValueText("new foo"), ciid1, layer1.ID, changeset4, new DataOriginV1(DataOriginType.Manual), trans5);
            trans5.Commit();

            // changeset2 is now old "enough", but still cannot be deleted because one of its attributes (a2) is the latest
            Assert.AreEqual(0, await changesetModel.ArchiveUnusedChangesetsOlderThan(DateTimeOffset.FromUnixTimeSeconds(151), transI));


            // delete attribute a2
            using var trans6 = ModelContextBuilder.BuildDeferred();
            var changeset5 = new ChangesetProxy(user, TimeThreshold.BuildAtTime(DateTimeOffset.FromUnixTimeSeconds(250)), changesetModel);
            await attributeModel.RemoveAttribute("a2", ciid1, layer1.ID, changeset5, new DataOriginV1(DataOriginType.Manual), trans6);
            trans6.Commit();

            // changeset2 is now old "enough", and can be deleted
            Assert.AreEqual(1, await changesetModel.ArchiveUnusedChangesetsOlderThan(DateTimeOffset.FromUnixTimeSeconds(151), transI));


            // other changeset can be deleted, if threshold is large enough
            Assert.AreEqual(2, await changesetModel.ArchiveUnusedChangesetsOlderThan(DateTimeOffset.FromUnixTimeSeconds(9999), transI));

            Assert.IsNull(await changesetModel.GetChangeset((await changeset1.GetChangeset(layer1.ID, new DataOriginV1(DataOriginType.Manual), transI)).ID, transI));
            Assert.IsNull(await changesetModel.GetChangeset((await changeset2.GetChangeset(layer1.ID, new DataOriginV1(DataOriginType.Manual), transI)).ID, transI));
            Assert.IsNull(await changesetModel.GetChangeset((await changeset3.GetChangeset(layer1.ID, new DataOriginV1(DataOriginType.Manual), transI)).ID, transI));
            Assert.IsNotNull(await changesetModel.GetChangeset((await changeset4.GetChangeset(layer1.ID, new DataOriginV1(DataOriginType.Manual), transI)).ID, transI));
            Assert.IsNull(await changesetModel.GetChangeset((await changeset5.GetChangeset(layer1.ID, new DataOriginV1(DataOriginType.Manual), transI)).ID, transI));
        }
    }
}
