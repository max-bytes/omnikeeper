using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.Model;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Tests.Integration.Model.Mocks;

namespace Tests.Integration.Model
{
    class CISearchModelTest : DBBackedTestBase
    {
        [Test]
        public async Task TestBasics()
        {
            var oap = new Mock<IOnlineAccessProxy>();
            oap.Setup(_ => _.IsOnlineInboundLayer(It.IsAny<string>(), It.IsAny<IModelContext>())).ReturnsAsync(false);
            var attributeModel = new AttributeModel(new BaseAttributeModel(new PartitionModel()));
            var ciModel = new CIModel(attributeModel, new CIIDModel());
            var relationModel = new RelationModel(new BaseRelationModel(new PartitionModel()));
            var userModel = new UserInDatabaseModel();
            var changesetModel = new ChangesetModel(userModel);
            var layerModel = new LayerModel();
            var traitsProvider = new MockedTraitsProvider();
            var traitModel = new EffectiveTraitModel(ciModel, attributeModel, relationModel, oap.Object, NullLogger<EffectiveTraitModel>.Instance);
            var searchModel = new CISearchModel(attributeModel, ciModel, traitModel, traitsProvider, NullLogger<CISearchModel>.Instance);
            var user = await DBSetup.SetupUser(userModel, ModelContextBuilder.BuildImmediate());
            Guid ciid1;
            Guid ciid2;
            Guid ciid3;
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                ciid1 = await ciModel.CreateCI(trans);
                ciid2 = await ciModel.CreateCI(trans);
                ciid3 = await ciModel.CreateCI(trans);
                trans.Commit();
            }

            string layerID1;
            string layerID2;
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var layer1 = await layerModel.UpsertLayer("l1", trans);
                var layer2 = await layerModel.UpsertLayer("l2", trans);
                layerID1 = layer1.ID;
                layerID2 = layer2.ID;
                trans.Commit();
            }

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
                await attributeModel.InsertCINameAttribute("ci1", ciid1, layerID1, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                await attributeModel.InsertCINameAttribute("ci2", ciid2, layerID1, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                await attributeModel.InsertCINameAttribute("ci3", ciid3, layerID2, changeset, new DataOriginV1(DataOriginType.Manual), trans); // name on different layer
                var i1 = await attributeModel.InsertAttribute("a1", new AttributeScalarValueText("text1"), ciid1, layerID1, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                var i2 = await attributeModel.InsertAttribute("a2", new AttributeScalarValueText("text1"), ciid2, layerID1, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                var i3 = await attributeModel.InsertAttribute("a3", new AttributeScalarValueText("text1"), ciid1, layerID2, changeset, new DataOriginV1(DataOriginType.Manual), trans);

                trans.Commit();
            }

            var tt = TimeThreshold.BuildLatest();

            var transI = ModelContextBuilder.BuildImmediate();

            var all = await ciModel.GetCompactCIs(new AllCIIDsSelection(), new LayerSet(layerID1, layerID2), transI, tt);

            //(await searchModel.SimpleSearch("ci", transI, tt)).Should().BeEquivalentTo(all);
            //(await searchModel.SimpleSearch("i", transI, tt)).Should().BeEquivalentTo(all);
            //(await searchModel.SimpleSearch("ci2", transI, tt)).Should().BeEquivalentTo(all.Where(ci => ci.Name == "ci2"));
            //(await searchModel.SimpleSearch("i3", transI, tt)).Should().BeEquivalentTo(all.Where(ci => ci.Name == "ci3"));

            (await searchModel.AdvancedSearchForCompactCIs("", new string[] { }, new string[] { }, new LayerSet(layerID1, layerID2), transI, tt)).Should().BeEquivalentTo(all);
            (await searchModel.AdvancedSearchForCompactCIs("", new string[] { "test_trait_3" }, new string[] { }, new LayerSet(layerID1, layerID2), transI, tt)).Should().BeEquivalentTo(all.Where(ci => ci.Name == "ci1"));
            (await searchModel.AdvancedSearchForCompactCIs("", new string[] { "test_trait_3" }, new string[] { }, new LayerSet(layerID2), transI, tt)).Should().BeEquivalentTo(ImmutableArray<CompactCI>.Empty);
            (await searchModel.AdvancedSearchForCompactCIs("", new string[] { "test_trait_4" }, new string[] { }, new LayerSet(layerID1, layerID2), transI, tt)).Should().BeEquivalentTo(ImmutableArray<CompactCI>.Empty);
            (await searchModel.AdvancedSearchForCompactCIs("", new string[] { }, new string[] { "test_trait_3" }, new LayerSet(layerID1, layerID2), transI, tt)).Should().BeEquivalentTo(all.Where(ci => ci.Name != "ci1"));

        }
    }
}
