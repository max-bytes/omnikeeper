using Castle.Core.Logging;
using FluentAssertions;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.Model;
using Omnikeeper.Utils;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualBasic;
using Npgsql;
using NUnit.Framework;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Tests.Integration.Model.Mocks;
using Omnikeeper.Base.Inbound;
using Moq;
using Omnikeeper.Base.Utils.ModelContext;

namespace Tests.Integration.Model
{
    class CISearchModelTest : DBBackedTestBase
    {
        [Test]
        public async Task TestBasics()
        {
            var oap = new Mock<IOnlineAccessProxy>();
            oap.Setup(_ => _.IsOnlineInboundLayer(It.IsAny<long>(), It.IsAny<IModelContext>())).ReturnsAsync(false);
            var attributeModel = new AttributeModel(new BaseAttributeModel());
            var ciModel = new CIModel(attributeModel);
            var predicateModel = new PredicateModel();
            var traitsProvider = new MockedTraitsProvider();
            var relationModel = new RelationModel(new BaseRelationModel(predicateModel));
            var userModel = new UserInDatabaseModel();
            var changesetModel = new ChangesetModel(userModel);
            var layerModel = new LayerModel();
            var traitModel = new EffectiveTraitModel(ciModel, relationModel, traitsProvider, oap.Object, NullLogger<EffectiveTraitModel>.Instance);
            var searchModel = new CISearchModel(attributeModel, ciModel, traitModel, layerModel);
            var user = await DBSetup.SetupUser(userModel, ModelContextBuilder.BuildImmediate());
            Guid ciid1;
            Guid ciid2;
            Guid ciid3;
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changesetID = await changesetModel.CreateChangeset(user.ID, trans);
                ciid1 = await ciModel.CreateCI(trans);
                ciid2 = await ciModel.CreateCI(trans);
                ciid3 = await ciModel.CreateCI(trans);
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
                await attributeModel.InsertCINameAttribute("ci1", ciid1, layerID1, changeset, trans);
                await attributeModel.InsertCINameAttribute("ci2", ciid2, layerID1, changeset, trans);
                await attributeModel.InsertCINameAttribute("ci3", ciid3, layerID2, changeset, trans); // name on different layer
                var i1 = await attributeModel.InsertAttribute("a1", new AttributeScalarValueText("text1"), ciid1, layerID1, changeset, trans);
                var i2 = await attributeModel.InsertAttribute("a2", new AttributeScalarValueText("text1"), ciid2, layerID1, changeset, trans);
                var i3 = await attributeModel.InsertAttribute("a3", new AttributeScalarValueText("text1"), ciid1, layerID2, changeset, trans);

                trans.Commit();
            }

            var tt = TimeThreshold.BuildLatest();

            var transI = ModelContextBuilder.BuildImmediate();

            var all = await ciModel.GetCompactCIs(new AllCIIDsSelection(), new LayerSet(layerID1, layerID2), transI, tt);

            (await searchModel.SimpleSearch("ci", transI, tt)).Should().BeEquivalentTo(all);
            (await searchModel.SimpleSearch("i", transI, tt)).Should().BeEquivalentTo(all);
            (await searchModel.SimpleSearch("ci2", transI, tt)).Should().BeEquivalentTo(all.Where(ci => ci.Name == "ci2"));
            (await searchModel.SimpleSearch("i3", transI, tt)).Should().BeEquivalentTo(all.Where(ci => ci.Name == "ci3"));

            (await searchModel.AdvancedSearch("", new string[] { }, new LayerSet(layerID1, layerID2), transI, tt)).Should().BeEquivalentTo(ImmutableArray<CompactCI>.Empty);
            (await searchModel.AdvancedSearch("", new string[] { "test_trait_3" }, new LayerSet(layerID1, layerID2), transI, tt)).Should().BeEquivalentTo(all.Where(ci => ci.Name == "ci1"));
            (await searchModel.AdvancedSearch("", new string[] { "test_trait_3" }, new LayerSet(layerID2), transI, tt)).Should().BeEquivalentTo(ImmutableArray<CompactCI>.Empty);
            (await searchModel.AdvancedSearch("", new string[] { "test_trait_4" }, new LayerSet(layerID1, layerID2), transI, tt)).Should().BeEquivalentTo(ImmutableArray<CompactCI>.Empty);

        }
    }
}
