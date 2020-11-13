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
using Omnikeeper.Base.Utils.ModelContext;

namespace Tests.Integration.Model
{
    partial class EffectiveTraitsModelTest : DBBackedTestBase
    {
        [Test]
        public async Task TestTraitAttributes()
        {
            var (traitModel, layerset, ciids) = await BaseSetup(new MockedTraitsProvider());

            var timeThreshold = TimeThreshold.BuildLatest();

            var trans = ModelContextBuilder.BuildImmediate();

            var t0 = await traitModel.CalculateEffectiveTraitsForTraitName("invalid_trait", layerset, trans, timeThreshold);
            Assert.AreEqual(null, t0);

            var t1 = await traitModel.CalculateEffectiveTraitsForTraitName("test_trait_1", layerset, trans, timeThreshold);
            Assert.AreEqual(3, t1.Count());
            var t2 = await traitModel.CalculateEffectiveTraitsForTraitName("test_trait_2", layerset, trans, timeThreshold);
            Assert.AreEqual(2, t2.Count());
            Assert.IsTrue(t2.All(t => t.Value.TraitAttributes.Any(ta => ta.Value.Attribute.Name == "a2") && t.Value.TraitAttributes.Any(ta => ta.Value.Attribute.Name == "a4")));
            var t3 = await traitModel.CalculateEffectiveTraitsForTraitName("test_trait_3", layerset, trans, timeThreshold);
            Assert.AreEqual(2, t3.Count());
            Assert.IsTrue(t3.All(t => t.Value.TraitAttributes.Any(ta => ta.Value.Attribute.Name == "a1")));

            var tt0 = await traitModel.CalculateMergedCIsWithTrait("invalid_trait", layerset, trans, timeThreshold);
            Assert.AreEqual(null, tt0);


            var tt1 = await traitModel.CalculateMergedCIsWithTrait("test_trait_1", layerset, trans, timeThreshold);
            Assert.AreEqual(3, tt1.Count());
            tt1.Select(c => c.ID).Should().BeEquivalentTo(new Guid[] { ciids[0], ciids[1], ciids[2] });

            var tt2 = await traitModel.CalculateMergedCIsWithTrait("test_trait_2", layerset, trans, timeThreshold);
            Assert.AreEqual(2, tt2.Count());
            tt2.Select(c => c.ID).Should().BeEquivalentTo(new Guid[] { ciids[0], ciids[2] });
        }

        [Test]
        public async Task TestDependentTraits()
        {
            var (traitModel, layerset, _) = await BaseSetup(new MockedTraitsProvider());

            var timeThreshold = TimeThreshold.BuildLatest();
            var trans = ModelContextBuilder.BuildImmediate();

            var t1 = await traitModel.CalculateEffectiveTraitsForTraitName("test_trait_4", layerset, trans, timeThreshold);
            Assert.AreEqual(2, t1.Count());
            var t2 = await traitModel.CalculateEffectiveTraitsForTraitName("test_trait_5", layerset, trans, timeThreshold);
            Assert.AreEqual(1, t2.Count());
        }

        [Test]
        public async Task TestDependentTraitLoop()
        {
            var timeThreshold = TimeThreshold.BuildLatest();
            var (traitModel, layerset, _) = await BaseSetup(new MockedTraitsProviderWithLoop());
            var trans = ModelContextBuilder.BuildImmediate();
            var t1 = await traitModel.CalculateEffectiveTraitsForTraitName("test_trait_1", layerset, trans, timeThreshold);
            Assert.AreEqual(0, t1.Count());
        }

        private async Task<(EffectiveTraitModel traitModel, LayerSet layerset, Guid[])> BaseSetup(ITraitsProvider traitsProvider)
        {
            var oap = new Mock<IOnlineAccessProxy>();
            oap.Setup(_ => _.IsOnlineInboundLayer(It.IsAny<long>(), It.IsAny<IModelContext>())).ReturnsAsync(false);
            var attributeModel = new AttributeModel(new BaseAttributeModel());
            var ciModel = new CIModel(attributeModel);
            var userModel = new UserInDatabaseModel();
            var changesetModel = new ChangesetModel(userModel);
            var predicateModel = new PredicateModel();
            var relationModel = new RelationModel(new BaseRelationModel(predicateModel));
            var layerModel = new LayerModel();
            var traitModel = new EffectiveTraitModel(ciModel, relationModel, traitsProvider, oap.Object, NullLogger<EffectiveTraitModel>.Instance);

            var transI = ModelContextBuilder.BuildImmediate();
            var user = await DBSetup.SetupUser(userModel, transI);
            var ciid1 = await ciModel.CreateCI(transI);
            var ciid2 = await ciModel.CreateCI(transI);
            var ciid3 = await ciModel.CreateCI(transI);
            var layer1 = await layerModel.CreateLayer("l1", transI);

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = new ChangesetProxy(user, DateTimeOffset.Now, changesetModel);
                await attributeModel.InsertAttribute("a1", new AttributeScalarValueText("text1"), ciid1, layer1.ID, changeset, trans);
                await attributeModel.InsertAttribute("a2", new AttributeScalarValueText("text2"), ciid1, layer1.ID, changeset, trans);
                await attributeModel.InsertAttribute("a3", new AttributeScalarValueText("text3"), ciid1, layer1.ID, changeset, trans);
                await attributeModel.InsertAttribute("a4", new AttributeScalarValueText("text4"), ciid1, layer1.ID, changeset, trans);

                await attributeModel.InsertAttribute("a1", new AttributeScalarValueText("text1"), ciid2, layer1.ID, changeset, trans);
                await attributeModel.InsertAttribute("a4", new AttributeScalarValueText("text4"), ciid2, layer1.ID, changeset, trans);

                await attributeModel.InsertAttribute("a2", new AttributeScalarValueText("text2"), ciid3, layer1.ID, changeset, trans);
                await attributeModel.InsertAttribute("a3", new AttributeScalarValueText("text3"), ciid3, layer1.ID, changeset, trans);
                await attributeModel.InsertAttribute("a4", new AttributeScalarValueText("text4"), ciid3, layer1.ID, changeset, trans);

                trans.Commit();
            }

            var layerset = await layerModel.BuildLayerSet(new string[] { "l1" }, transI);
            return (traitModel, layerset, new Guid[] { ciid1, ciid2, ciid3 });
        }
    }
}
