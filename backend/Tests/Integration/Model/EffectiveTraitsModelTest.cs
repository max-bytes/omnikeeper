using FluentAssertions;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Linq;
using System.Threading.Tasks;
using Tests.Integration.Model.Mocks;

namespace Tests.Integration.Model
{
    partial class EffectiveTraitsModelTest : DIServicedTestBase
    {
        [Test]
        public async Task TestTraitAttributes()
        {
            var traitsProvider = new MockedTraitsProvider();
            var (traitModel, ciModel, layerset, ciids) = await BaseSetup();

            var timeThreshold = TimeThreshold.BuildLatest();

            var trans = ModelContextBuilder.BuildImmediate();

            // TODO: move test for TraitsProvider to its own test-class
            var invalidTrait = await traitsProvider.GetActiveTrait("invalid_trait", trans, timeThreshold);
            Assert.AreEqual(null, invalidTrait);

            var testTrait1 = (await traitsProvider.GetActiveTrait("test_trait_1", trans, timeThreshold))!;
            var testTrait2 = (await traitsProvider.GetActiveTrait("test_trait_2", trans, timeThreshold))!;
            var testTrait3 = (await traitsProvider.GetActiveTrait("test_trait_3", trans, timeThreshold))!;

            var cis = await ciModel.GetMergedCIs(new AllCIIDsSelection(), layerset, false, AllAttributeSelection.Instance, trans, timeThreshold);
            var et1 = await traitModel.GetEffectiveTraitsForTrait(testTrait1, cis, layerset, trans, timeThreshold);
            Assert.AreEqual(3, et1.Count());
            var et2 = await traitModel.GetEffectiveTraitsForTrait(testTrait2, cis, layerset, trans, timeThreshold);
            Assert.AreEqual(2, et2.Count());
            Assert.IsTrue(et2.All(t => t.Value.TraitAttributes.Any(ta => ta.Value.Attribute.Name == "a2") && t.Value.TraitAttributes.Any(ta => ta.Value.Attribute.Name == "a4")));
            var et3 = await traitModel.GetEffectiveTraitsForTrait(testTrait3, cis, layerset, trans, timeThreshold);
            Assert.AreEqual(2, et3.Count());
            Assert.IsTrue(et3.All(t => t.Value.TraitAttributes.Any(ta => ta.Value.Attribute.Name == "a1")));

            var cis1 = traitModel.FilterCIsWithTrait(cis, testTrait1, layerset, trans, timeThreshold);
            Assert.AreEqual(3, cis1.Count());
            cis1.Select(c => c.ID).Should().BeEquivalentTo(new Guid[] { ciids[0], ciids[1], ciids[2] }, options => options.WithStrictOrdering());

            var cis2 = traitModel.FilterCIsWithTrait(cis, testTrait2, layerset, trans, timeThreshold);
            Assert.AreEqual(2, cis2.Count());
            cis2.Select(c => c.ID).Should().BeEquivalentTo(new Guid[] { ciids[0], ciids[2] }, options => options.WithStrictOrdering());

            // test inverted filtering
            var cis3 = traitModel.FilterCIsWithoutTrait(cis, testTrait1, layerset, trans, timeThreshold);
            Assert.AreEqual(0, cis3.Count());

            var cis4 = traitModel.FilterCIsWithoutTrait(cis, testTrait2, layerset, trans, timeThreshold);
            Assert.AreEqual(1, cis4.Count());
            cis4.Select(c => c.ID).Should().BeEquivalentTo(new Guid[] { ciids[1] }, options => options.WithStrictOrdering());
        }

        [Test]
        public async Task TestDependentTraits()
        {
            var traitsProvider = new MockedTraitsProvider();
            var (traitModel, ciModel, layerset, _) = await BaseSetup();

            var timeThreshold = TimeThreshold.BuildLatest();
            var trans = ModelContextBuilder.BuildImmediate();


            var t4 = await traitsProvider.GetActiveTrait("test_trait_4", trans, timeThreshold);
            var t5 = await traitsProvider.GetActiveTrait("test_trait_5", trans, timeThreshold);
            var cis = await ciModel.GetMergedCIs(new AllCIIDsSelection(), layerset, false, AllAttributeSelection.Instance, trans, timeThreshold);

            var t1 = await traitModel.GetEffectiveTraitsForTrait(t4!, cis, layerset, trans, timeThreshold);
            Assert.AreEqual(2, t1.Count());
            var t2 = await traitModel.GetEffectiveTraitsForTrait(t5!, cis, layerset, trans, timeThreshold);
            Assert.AreEqual(1, t2.Count());
        }

        [Test]
        public async Task TestDependentTraitLoop()
        {
            var timeThreshold = TimeThreshold.BuildLatest();
            var traitsProvider = new MockedTraitsProviderWithLoop();
            var (traitModel, ciModel, layerset, _) = await BaseSetup();
            var trans = ModelContextBuilder.BuildImmediate();
            var tt1 = await traitsProvider.GetActiveTrait("test_trait_1", trans, timeThreshold);
            var cis = await ciModel.GetMergedCIs(new AllCIIDsSelection(), layerset, false, AllAttributeSelection.Instance, trans, timeThreshold);
            var t1 = await traitModel.GetEffectiveTraitsForTrait(tt1!, cis, layerset, trans, timeThreshold);
            Assert.AreEqual(1, t1.Count());
        }

        private async Task<(IEffectiveTraitModel traitModel, ICIModel ciModel, LayerSet layerset, Guid[])> BaseSetup()
        {
            var transI = ModelContextBuilder.BuildImmediate();
            var ciid1 = await GetService<ICIModel>().CreateCI(transI);
            var ciid2 = await GetService<ICIModel>().CreateCI(transI);
            var ciid3 = await GetService<ICIModel>().CreateCI(transI);
            var (layer1, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("l1", transI);

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = await CreateChangesetProxy();
                await GetService<IAttributeModel>().InsertAttribute("a1", new AttributeScalarValueText("text1"), ciid1, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans, OtherLayersValueHandlingForceWrite.Instance);
                await GetService<IAttributeModel>().InsertAttribute("a2", new AttributeScalarValueText("text2"), ciid1, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans, OtherLayersValueHandlingForceWrite.Instance);
                await GetService<IAttributeModel>().InsertAttribute("a3", new AttributeScalarValueText("text3"), ciid1, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans, OtherLayersValueHandlingForceWrite.Instance);
                await GetService<IAttributeModel>().InsertAttribute("a4", new AttributeScalarValueText("text41"), ciid1, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans, OtherLayersValueHandlingForceWrite.Instance);

                await GetService<IAttributeModel>().InsertAttribute("a1", new AttributeScalarValueText("text1"), ciid2, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans, OtherLayersValueHandlingForceWrite.Instance);
                await GetService<IAttributeModel>().InsertAttribute("a4", new AttributeScalarValueText("text42"), ciid2, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans, OtherLayersValueHandlingForceWrite.Instance);

                await GetService<IAttributeModel>().InsertAttribute("a2", new AttributeScalarValueText("text2"), ciid3, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans, OtherLayersValueHandlingForceWrite.Instance);
                await GetService<IAttributeModel>().InsertAttribute("a3", new AttributeScalarValueText("text3"), ciid3, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans, OtherLayersValueHandlingForceWrite.Instance);
                await GetService<IAttributeModel>().InsertAttribute("a4", new AttributeScalarValueText("text42"), ciid3, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans, OtherLayersValueHandlingForceWrite.Instance);

                trans.Commit();
            }

            var layerset = await GetService<ILayerModel>().BuildLayerSet(new string[] { "l1" }, transI);
            return (GetService<IEffectiveTraitModel>(), GetService<ICIModel>(), layerset, new Guid[] { ciid1, ciid2, ciid3 });
        }
    }
}
