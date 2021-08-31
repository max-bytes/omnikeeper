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
using Omnikeeper.Model.Decorators;
using Omnikeeper.Model.Decorators.CachingEffectiveTraits;
using Omnikeeper.Utils;
using System;
using System.Linq;
using System.Threading.Tasks;
using Tests.Integration.Model.Mocks;

namespace Tests.Integration.Model
{
    partial class CachingEffectiveTraitsModelTest : DBBackedTestBase
    {
        [Test]
        public async Task TestGetMergedCIsWithTrait()
        {
            var traitsProvider = new MockedTraitsProvider();
            var (traitModel, cache, attributeModel, changesetModel, user, layerset, ciids) = await BaseSetup();

            var timeThreshold = TimeThreshold.BuildLatest();

            var trans = ModelContextBuilder.BuildImmediate();

            var invalidTrait = await traitsProvider.GetActiveTrait("invalid_trait", trans, timeThreshold);
            Assert.AreEqual(null, invalidTrait);

            var testTrait1 = (await traitsProvider.GetActiveTrait("test_trait_1", trans, timeThreshold))!;
            var testTrait2 = (await traitsProvider.GetActiveTrait("test_trait_2", trans, timeThreshold))!;
            var testTrait3 = (await traitsProvider.GetActiveTrait("test_trait_3", trans, timeThreshold))!;

            // first access
            var cis1 = await traitModel.GetMergedCIsWithTrait(testTrait1, layerset, new AllCIIDsSelection(), trans, timeThreshold);
            Assert.AreEqual(3, cis1.Count());
            cis1.Select(c => c.ID).Should().BeEquivalentTo(new Guid[] { ciids[0], ciids[1], ciids[2] });

            // check if cache is filled
            Assert.IsTrue(cache.GetCIIDsHavingTrait(testTrait1.ID, layerset, out var ciidsInCache1));
            ciidsInCache1.Should().BeEquivalentTo(new Guid[] { ciids[0], ciids[1], ciids[2] });
            Assert.IsFalse(cache.GetCIIDsHavingTrait(testTrait2.ID, layerset, out var _));
            Assert.IsFalse(cache.GetCIIDsHavingTrait(testTrait3.ID, layerset, out var _));

            // second access, should come from cache
            var cis1again = await traitModel.GetMergedCIsWithTrait(testTrait1, layerset, new AllCIIDsSelection(), trans, timeThreshold);
            Assert.AreEqual(3, cis1again.Count());
            cis1again.Select(c => c.ID).Should().BeEquivalentTo(new Guid[] { ciids[0], ciids[1], ciids[2] });

            // first access for trait2
            var cis2 = await traitModel.GetMergedCIsWithTrait(testTrait2, layerset, new AllCIIDsSelection(), trans, timeThreshold);
            Assert.AreEqual(2, cis2.Count());
            cis2.Select(c => c.ID).Should().BeEquivalentTo(new Guid[] { ciids[0], ciids[2] });

            // check if cache is filled for trait2 too
            Assert.IsTrue(cache.GetCIIDsHavingTrait(testTrait2.ID, layerset, out var ciidsInCache2));
            ciidsInCache2.Should().BeEquivalentTo(new Guid[] { ciids[0], ciids[2] });
            Assert.IsTrue(cache.GetCIIDsHavingTrait(testTrait1.ID, layerset, out var _));

            // do a change to ci1's attributes
            using (var transD = ModelContextBuilder.BuildDeferred())
            {
                var changeset = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
                await attributeModel.InsertAttribute("a1", new AttributeScalarValueText("text1_changed"), ciids[1], "l1", changeset, new DataOriginV1(DataOriginType.Manual), transD);
                transD.Commit();
            }

            // trait 2 should now contain 1 more item in cache, trait 1 still full
            Assert.IsTrue(cache.GetCIIDsHavingTrait(testTrait2.ID, layerset, out var ciidsInCache3));
            ciidsInCache3.Should().BeEquivalentTo(new Guid[] { ciids[0], ciids[1], ciids[2] });
            Assert.IsTrue(cache.GetCIIDsHavingTrait(testTrait1.ID, layerset, out var ciidsInCache4));
            ciidsInCache4.Should().BeEquivalentTo(new Guid[] { ciids[0], ciids[1], ciids[2] });


            // second access for trait2, should still return the same two cis, even when cache contains 3
            var cis3 = await traitModel.GetMergedCIsWithTrait(testTrait2, layerset, new AllCIIDsSelection(), trans, timeThreshold);
            Assert.AreEqual(2, cis3.Count());
            cis3.Select(c => c.ID).Should().BeEquivalentTo(new Guid[] { ciids[0], ciids[2] });

            // cache for trait 2 is updated again
            Assert.IsTrue(cache.GetCIIDsHavingTrait(testTrait2.ID, layerset, out var ciidsInCache5));
            ciidsInCache5.Should().BeEquivalentTo(new Guid[] { ciids[0], ciids[2] });
        }

        private async Task<(CachingEffectiveTraitModel traitModel, EffectiveTraitCache cache, AttributeModel attributeModel, 
            ChangesetModel changesetModel, UserInDatabase user, LayerSet layerset, Guid[])> BaseSetup()
        {
            var baseConfigurationModel = new BaseConfigurationModel(null);
            var cache = new EffectiveTraitCache();
            var oap = new Mock<IOnlineAccessProxy>();
            oap.Setup(_ => _.IsOnlineInboundLayer(It.IsAny<string>(), It.IsAny<IModelContext>())).ReturnsAsync(false);
            oap.Setup(_ => _.ContainsOnlineInboundLayer(It.IsAny<LayerSet>(), It.IsAny<IModelContext>())).ReturnsAsync(false);
            var decoratedBaseAttributeModel = new TraitCacheInvalidationBaseAttributeModel(new BaseAttributeModel(new PartitionModel()), baseConfigurationModel, cache);
            var attributeModel = new AttributeModel(decoratedBaseAttributeModel);

            var ciModel = new CIModel(attributeModel, new CIIDModel());
            var userModel = new UserInDatabaseModel();
            var changesetModel = new ChangesetModel(userModel);
            var relationModel = new RelationModel(new BaseRelationModel(new PartitionModel()));
            var layerModel = new LayerModel();
            var traitModel = new EffectiveTraitModel(ciModel, attributeModel, relationModel, oap.Object, NullLogger<EffectiveTraitModel>.Instance);

            var decoratedTraitModel = new CachingEffectiveTraitModel(traitModel, ciModel, cache, oap.Object);

            var transI = ModelContextBuilder.BuildImmediate();
            var user = await DBSetup.SetupUser(userModel, transI);
            var ciid1 = await ciModel.CreateCI(transI);
            var ciid2 = await ciModel.CreateCI(transI);
            var ciid3 = await ciModel.CreateCI(transI);
            var layer1 = await layerModel.UpsertLayer("l1", transI);

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
                await attributeModel.InsertAttribute("a1", new AttributeScalarValueText("text1"), ciid1, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                await attributeModel.InsertAttribute("a2", new AttributeScalarValueText("text2"), ciid1, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                await attributeModel.InsertAttribute("a3", new AttributeScalarValueText("text3"), ciid1, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                await attributeModel.InsertAttribute("a4", new AttributeScalarValueText("text4"), ciid1, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);

                await attributeModel.InsertAttribute("a1", new AttributeScalarValueText("text1"), ciid2, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                await attributeModel.InsertAttribute("a4", new AttributeScalarValueText("text4"), ciid2, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);

                await attributeModel.InsertAttribute("a2", new AttributeScalarValueText("text2"), ciid3, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                await attributeModel.InsertAttribute("a3", new AttributeScalarValueText("text3"), ciid3, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                await attributeModel.InsertAttribute("a4", new AttributeScalarValueText("text4"), ciid3, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);

                trans.Commit();
            }

            var layerset = await layerModel.BuildLayerSet(new string[] { "l1" }, transI);
            return (decoratedTraitModel, cache, attributeModel, changesetModel, user, layerset, new Guid[] { ciid1, ciid2, ciid3 });
        }
    }
}
