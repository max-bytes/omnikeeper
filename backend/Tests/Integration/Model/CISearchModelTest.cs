using Castle.Core.Logging;
using FluentAssertions;
using Landscape.Base.Entity;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using LandscapeRegistry.Entity.AttributeValues;
using LandscapeRegistry.Model;
using LandscapeRegistry.Utils;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualBasic;
using Npgsql;
using NUnit.Framework;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Tests.Integration.Model.Mocks;

namespace Tests.Integration.Model
{
    class CISearchModelTest
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
        public async Task TestBasics()
        {
            var attributeModel = new AttributeModel(new BaseAttributeModel(conn));
            var ciModel = new CIModel(attributeModel, conn);
            var predicateModel = new PredicateModel(conn);
            var traitsProvider = new MockedTraitsProvider();
            var relationModel = new RelationModel(new BaseRelationModel(predicateModel, conn));
            var userModel = new UserInDatabaseModel(conn);
            var changesetModel = new ChangesetModel(userModel, conn);
            var layerModel = new LayerModel(conn);
            var traitModel = new EffectiveTraitModel(ciModel, relationModel, traitsProvider, NullLogger<EffectiveTraitModel>.Instance, conn);
            var searchModel = new CISearchModel(attributeModel, ciModel, traitModel, layerModel);
            var user = await DBSetup.SetupUser(userModel);
            Guid ciid1;
            Guid ciid2;
            Guid ciid3;
            using (var trans = conn.BeginTransaction())
            {
                var changesetID = await changesetModel.CreateChangeset(user.ID, trans);
                ciid1 = await ciModel.CreateCI(trans);
                ciid2 = await ciModel.CreateCI(trans);
                ciid3 = await ciModel.CreateCI(trans);
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
                await attributeModel.InsertCINameAttribute("ci1", ciid1, layerID1, changeset, trans);
                await attributeModel.InsertCINameAttribute("ci2", ciid2, layerID1, changeset, trans);
                await attributeModel.InsertCINameAttribute("ci3", ciid3, layerID2, changeset, trans); // name on different layer
                var i1 = await attributeModel.InsertAttribute("a1", AttributeScalarValueText.Build("text1"), ciid1, layerID1, changeset, trans);
                var i2 = await attributeModel.InsertAttribute("a2", AttributeScalarValueText.Build("text1"), ciid2, layerID1, changeset, trans);
                var i3 = await attributeModel.InsertAttribute("a3", AttributeScalarValueText.Build("text1"), ciid1, layerID2, changeset, trans);

                trans.Commit();
            }

            var tt = TimeThreshold.BuildLatest();

            var all = await ciModel.GetCompactCIs(new AllCIIDsSelection(), new LayerSet(layerID1, layerID2), null, tt);

            (await searchModel.SimpleSearch("ci", null, tt)).Should().BeEquivalentTo(all);
            (await searchModel.SimpleSearch("i", null, tt)).Should().BeEquivalentTo(all);
            (await searchModel.SimpleSearch("ci2", null, tt)).Should().BeEquivalentTo(all.Where(ci => ci.Name == "ci2"));
            (await searchModel.SimpleSearch("i3", null, tt)).Should().BeEquivalentTo(all.Where(ci => ci.Name == "ci3"));

            (await searchModel.AdvancedSearch("", new string[] { }, new LayerSet(layerID1, layerID2), null, tt)).Should().BeEquivalentTo(ImmutableArray<CompactCI>.Empty);
            (await searchModel.AdvancedSearch("", new string[] { "test_trait_3" }, new LayerSet(layerID1, layerID2), null, tt)).Should().BeEquivalentTo(all.Where(ci => ci.Name == "ci1"));
            (await searchModel.AdvancedSearch("", new string[] { "test_trait_3" }, new LayerSet(layerID2), null, tt)).Should().BeEquivalentTo(ImmutableArray<CompactCI>.Empty);
            (await searchModel.AdvancedSearch("", new string[] { "test_trait_4" }, new LayerSet(layerID1, layerID2), null, tt)).Should().BeEquivalentTo(ImmutableArray<CompactCI>.Empty);

        }
    }
}
