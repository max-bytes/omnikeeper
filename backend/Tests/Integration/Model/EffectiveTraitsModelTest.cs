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
using System.Linq;
using System.Threading.Tasks;
using Tests.Integration.Model.Mocks;

namespace Tests.Integration.Model
{
    partial class EffectiveTraitsModelTest : DBBackedTestBase
    {
        [Test]
        public async Task TestTraitAttributes()
        {
            var traitsProvider = new MockedTraitsProvider();
            var (traitModel, layerset, ciids) = await BaseSetup();

            var timeThreshold = TimeThreshold.BuildLatest();

            var trans = ModelContextBuilder.BuildImmediate();

            // TODO: move test for TraitsProvider to its own test-class
            var invalidTrait = await traitsProvider.GetActiveTrait("invalid_trait", trans, timeThreshold);
            Assert.AreEqual(null, invalidTrait);

            var testTrait1 = (await traitsProvider.GetActiveTrait("test_trait_1", trans, timeThreshold))!;
            var testTrait2 = (await traitsProvider.GetActiveTrait("test_trait_2", trans, timeThreshold))!;
            var testTrait3 = (await traitsProvider.GetActiveTrait("test_trait_3", trans, timeThreshold))!;

            var et1 = await traitModel.GetEffectiveTraitsForTrait(testTrait1, layerset, new AllCIIDsSelection(), trans, timeThreshold);
            Assert.AreEqual(3, et1.Count());
            var et2 = await traitModel.GetEffectiveTraitsForTrait(testTrait2, layerset, new AllCIIDsSelection(), trans, timeThreshold);
            Assert.AreEqual(2, et2.Count());
            Assert.IsTrue(et2.All(t => t.Value.et.TraitAttributes.Any(ta => ta.Value.Attribute.Name == "a2") && t.Value.et.TraitAttributes.Any(ta => ta.Value.Attribute.Name == "a4")));
            var et3 = await traitModel.GetEffectiveTraitsForTrait(testTrait3, layerset, new AllCIIDsSelection(), trans, timeThreshold);
            Assert.AreEqual(2, et3.Count());
            Assert.IsTrue(et3.All(t => t.Value.et.TraitAttributes.Any(ta => ta.Value.Attribute.Name == "a1")));

            var cis1 = await traitModel.GetMergedCIsWithTrait(testTrait1, layerset, new AllCIIDsSelection(), trans, timeThreshold);
            Assert.AreEqual(3, cis1.Count());
            cis1.Select(c => c.ID).Should().BeEquivalentTo(new Guid[] { ciids[0], ciids[1], ciids[2] }, options => options.WithStrictOrdering());

            var cis2 = await traitModel.GetMergedCIsWithTrait(testTrait2, layerset, new AllCIIDsSelection(), trans, timeThreshold);
            Assert.AreEqual(2, cis2.Count());
            cis2.Select(c => c.ID).Should().BeEquivalentTo(new Guid[] { ciids[0], ciids[2] }, options => options.WithStrictOrdering());
        }

        [Test]
        public async Task TestTraitWithNameAndValue()
        {
            var traitsProvider = new MockedTraitsProvider();
            var (traitModel, layerset, ciids) = await BaseSetup();

            var timeThreshold = TimeThreshold.BuildLatest();

            var trans = ModelContextBuilder.BuildImmediate();

            var testTrait1 = (await traitsProvider.GetActiveTrait("test_trait_1", trans, timeThreshold))!;

            var et1 = await traitModel.GetEffectiveTraitsWithTraitAttributeValue(testTrait1, "a4", new AttributeScalarValueText("text41"), layerset, new AllCIIDsSelection(), trans, timeThreshold);
            Assert.AreEqual(1, et1.Count());
            Assert.AreEqual(ciids[0], et1.First().Key);

            var et2 = await traitModel.GetEffectiveTraitsWithTraitAttributeValue(testTrait1, "a4", new AttributeScalarValueText("text42"), layerset, new AllCIIDsSelection(), trans, timeThreshold);
            Assert.AreEqual(2, et2.Count());
            et2.Select(e => e.Key).Should().BeEquivalentTo(new Guid[] { ciids[1], ciids[2] }, options => options.WithStrictOrdering());
        }

        [Test]
        public async Task TestDependentTraits()
        {
            var traitsProvider = new MockedTraitsProvider();
            var (traitModel, layerset, _) = await BaseSetup();

            var timeThreshold = TimeThreshold.BuildLatest();
            var trans = ModelContextBuilder.BuildImmediate();


            var t4 = await traitsProvider.GetActiveTrait("test_trait_4", trans, timeThreshold);
            var t5 = await traitsProvider.GetActiveTrait("test_trait_5", trans, timeThreshold);

            var t1 = await traitModel.GetEffectiveTraitsForTrait(t4!, layerset, new AllCIIDsSelection(), trans, timeThreshold);
            Assert.AreEqual(2, t1.Count());
            var t2 = await traitModel.GetEffectiveTraitsForTrait(t5!, layerset, new AllCIIDsSelection(), trans, timeThreshold);
            Assert.AreEqual(1, t2.Count());
        }

        [Test]
        public async Task TestDependentTraitLoop()
        {
            var timeThreshold = TimeThreshold.BuildLatest();
            var traitsProvider = new MockedTraitsProviderWithLoop();
            var (traitModel, layerset, _) = await BaseSetup();
            var trans = ModelContextBuilder.BuildImmediate();
            var tt1 = await traitsProvider.GetActiveTrait("test_trait_1", trans, timeThreshold);
            var t1 = await traitModel.GetEffectiveTraitsForTrait(tt1!, layerset, new AllCIIDsSelection(), trans, timeThreshold);
            Assert.AreEqual(1, t1.Count());
        }

        private async Task<(EffectiveTraitModel traitModel, LayerSet layerset, Guid[])> BaseSetup()
        {
            var attributeModel = new AttributeModel(new BaseAttributeModel(new PartitionModel(), new CIIDModel()));
            var ciModel = new CIModel(attributeModel, new CIIDModel());
            var userModel = new UserInDatabaseModel();
            var changesetModel = new ChangesetModel(userModel);
            var relationModel = new RelationModel(new BaseRelationModel(new PartitionModel()));
            var layerModel = new LayerModel();
            var traitModel = new EffectiveTraitModel(ciModel, attributeModel, relationModel, NullLogger<EffectiveTraitModel>.Instance);

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
                await attributeModel.InsertAttribute("a4", new AttributeScalarValueText("text41"), ciid1, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);

                await attributeModel.InsertAttribute("a1", new AttributeScalarValueText("text1"), ciid2, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                await attributeModel.InsertAttribute("a4", new AttributeScalarValueText("text42"), ciid2, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);

                await attributeModel.InsertAttribute("a2", new AttributeScalarValueText("text2"), ciid3, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                await attributeModel.InsertAttribute("a3", new AttributeScalarValueText("text3"), ciid3, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                await attributeModel.InsertAttribute("a4", new AttributeScalarValueText("text42"), ciid3, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);

                trans.Commit();
            }

            var layerset = await layerModel.BuildLayerSet(new string[] { "l1" }, transI);
            return (traitModel, layerset, new Guid[] { ciid1, ciid2, ciid3 });
        }
    }
}
