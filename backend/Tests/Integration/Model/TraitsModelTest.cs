using Landscape.Base.Entity;
using Landscape.Base.Model;
using LandscapeRegistry.Entity.AttributeValues;
using LandscapeRegistry.Model;
using LandscapeRegistry.Utils;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Tests.Integration.Model
{
    class TraitsModelTest
    {
        [SetUp]
        public void Setup()
        {
            DBSetup.Setup();
        }

        private class MockedTraitsProvider : ITraitsProvider
        {
            public async Task<IImmutableDictionary<string, Trait>> GetTraits(NpgsqlTransaction trans)
            {
                return new List<Trait>()
                {
                    Trait.Build("test_trait_1", new List<TraitAttribute>()
                    {
                        TraitAttribute.Build("a4",
                            CIAttributeTemplate.BuildFromParams("a4", AttributeValueType.Text, false)
                        )
                    }, new List<TraitAttribute>() { }, new List<TraitRelation>() { }),
                    Trait.Build("test_trait_2", new List<TraitAttribute>()
                    {
                        TraitAttribute.Build("a4",
                            CIAttributeTemplate.BuildFromParams("a4", AttributeValueType.Text, false)
                        ),
                        TraitAttribute.Build("a2",
                            CIAttributeTemplate.BuildFromParams("a2", AttributeValueType.Text, false)
                        )
                    }, new List<TraitAttribute>() { }, new List<TraitRelation>() { }),
                    Trait.Build("test_trait_3", new List<TraitAttribute>()
                    {
                        TraitAttribute.Build("a1",
                            CIAttributeTemplate.BuildFromParams("a1", AttributeValueType.Text, false)
                        )
                    }, new List<TraitAttribute>() { }, new List<TraitRelation>() { }),
                    Trait.Build("test_trait_4", new List<TraitAttribute>()
                    {
                        TraitAttribute.Build("a1",
                            CIAttributeTemplate.BuildFromParams("a1", AttributeValueType.Text, false)
                        )
                    }, requiredTraits: new List<string>() { "test_trait_1" }),
                    Trait.Build("test_trait_5", new List<TraitAttribute>()
                    {
                        TraitAttribute.Build("a2",
                            CIAttributeTemplate.BuildFromParams("a2", AttributeValueType.Text, false)
                        )
                    }, requiredTraits: new List<string>() { "test_trait_4" })
                }.ToImmutableDictionary(t => t.Name);
            }
        }

        private class MockedTraitsProviderWithLoop : ITraitsProvider
        {
            public async Task<IImmutableDictionary<string, Trait>> GetTraits(NpgsqlTransaction trans)
            {
                return new List<Trait>()
                {
                    Trait.Build("test_trait_1", new List<TraitAttribute>()
                    {
                        TraitAttribute.Build("a4",
                            CIAttributeTemplate.BuildFromParams("a4", AttributeValueType.Text, false)
                        )
                    }, requiredTraits: new List<string>() { "test_trait_2" }),
                    Trait.Build("test_trait_2", new List<TraitAttribute>()
                    {
                        TraitAttribute.Build("a4",
                            CIAttributeTemplate.BuildFromParams("a4", AttributeValueType.Text, false)
                        ),
                        TraitAttribute.Build("a2",
                            CIAttributeTemplate.BuildFromParams("a2", AttributeValueType.Text, false)
                        )
                    }, requiredTraits: new List<string>() { "test_trait_3" }),
                    Trait.Build("test_trait_3", new List<TraitAttribute>()
                    {
                        TraitAttribute.Build("a1",
                            CIAttributeTemplate.BuildFromParams("a1", AttributeValueType.Text, false)
                        )
                    }, requiredTraits: new List<string>() { "test_trait_1" })
                }.ToImmutableDictionary(t => t.Name);
            }
        }

        [Test]
        public async Task TestTraitAttributes()
        {
            var dbcb = new DBConnectionBuilder();
            using var conn = dbcb.Build(DBSetup.dbName, false, true);
            var (traitModel, layerset) = await BaseSetup(new MockedTraitsProvider(), conn);

            var t0 = await traitModel.CalculateEffectiveTraitSetsForTraitName("invalid_trait", layerset, null, DateTimeOffset.Now);
            Assert.AreEqual(null, t0);

            var t1 = await traitModel.CalculateEffectiveTraitSetsForTraitName("test_trait_1", layerset, null, DateTimeOffset.Now);
            Assert.AreEqual(3, t1.Count());
            var t2 = await traitModel.CalculateEffectiveTraitSetsForTraitName("test_trait_2", layerset, null, DateTimeOffset.Now);
            Assert.AreEqual(2, t2.Count());
            Assert.IsTrue(t2.All(t => t.EffectiveTraits["test_trait_2"].TraitAttributes.Any(ta => ta.Value.Attribute.Name == "a2") && t.EffectiveTraits["test_trait_2"].TraitAttributes.Any(ta => ta.Value.Attribute.Name == "a4")));
            var t3 = await traitModel.CalculateEffectiveTraitSetsForTraitName("test_trait_3", layerset, null, DateTimeOffset.Now);
            Assert.AreEqual(2, t3.Count());
            Assert.IsTrue(t3.All(t => t.EffectiveTraits["test_trait_3"].TraitAttributes.Any(ta => ta.Value.Attribute.Name == "a1")));
        }


        [Test]
        public async Task TestDependentTraits()
        {
            var dbcb = new DBConnectionBuilder();
            using var conn = dbcb.Build(DBSetup.dbName, false, true);
            var (traitModel, layerset) = await BaseSetup(new MockedTraitsProvider(), conn);

            var t1 = await traitModel.CalculateEffectiveTraitSetsForTraitName("test_trait_4", layerset, null, DateTimeOffset.Now);
            Assert.AreEqual(2, t1.Count());
            var t2 = await traitModel.CalculateEffectiveTraitSetsForTraitName("test_trait_5", layerset, null, DateTimeOffset.Now);
            Assert.AreEqual(1, t2.Count());
        }

        [Test]
        public async Task TestDependentTraitLoop()
        {
            var dbcb = new DBConnectionBuilder();
            using var conn = dbcb.Build(DBSetup.dbName, false, true);
            var (traitModel, layerset) = await BaseSetup(new MockedTraitsProviderWithLoop(), conn);
            var t1 = await traitModel.CalculateEffectiveTraitSetsForTraitName("test_trait_1", layerset, null, DateTimeOffset.Now);
            Assert.AreEqual(0, t1.Count());
        }

        private async Task<(TraitModel traitModel, LayerSet layerset)> BaseSetup(ITraitsProvider traitsProvider, NpgsqlConnection conn)
        {
            var attributeModel = new AttributeModel(conn);
            var ciModel = new CIModel(attributeModel, conn);
            var userModel = new UserInDatabaseModel(conn);
            var changesetModel = new ChangesetModel(userModel, conn);
            var predicateModel = new PredicateModel(conn);
            var relationModel = new RelationModel(predicateModel, conn);
            var layerModel = new LayerModel(conn);
            var traitModel = new TraitModel(ciModel, relationModel, traitsProvider, NullLogger<TraitModel>.Instance, conn);
            var user = await DBSetup.SetupUser(userModel);
            var ciid1 = await ciModel.CreateCI(null);
            var ciid2 = await ciModel.CreateCI(null);
            var ciid3 = await ciModel.CreateCI(null);
            var layer1 = await layerModel.CreateLayer("l1", null);

            using (var trans = conn.BeginTransaction())
            {
                var changeset = await changesetModel.CreateChangeset(user.ID, trans);
                await attributeModel.InsertAttribute("a1", AttributeValueTextScalar.Build("text1"), layer1.ID, ciid1, changeset.ID, trans);
                await attributeModel.InsertAttribute("a2", AttributeValueTextScalar.Build("text2"), layer1.ID, ciid1, changeset.ID, trans);
                await attributeModel.InsertAttribute("a3", AttributeValueTextScalar.Build("text3"), layer1.ID, ciid1, changeset.ID, trans);
                await attributeModel.InsertAttribute("a4", AttributeValueTextScalar.Build("text4"), layer1.ID, ciid1, changeset.ID, trans);

                await attributeModel.InsertAttribute("a1", AttributeValueTextScalar.Build("text1"), layer1.ID, ciid2, changeset.ID, trans);
                await attributeModel.InsertAttribute("a4", AttributeValueTextScalar.Build("text4"), layer1.ID, ciid2, changeset.ID, trans);

                await attributeModel.InsertAttribute("a2", AttributeValueTextScalar.Build("text2"), layer1.ID, ciid3, changeset.ID, trans);
                await attributeModel.InsertAttribute("a3", AttributeValueTextScalar.Build("text3"), layer1.ID, ciid3, changeset.ID, trans);
                await attributeModel.InsertAttribute("a4", AttributeValueTextScalar.Build("text4"), layer1.ID, ciid3, changeset.ID, trans);

                trans.Commit();
            }

            var layerset = await layerModel.BuildLayerSet(new string[] { "l1" }, null);
            return (traitModel, layerset);
        }
    }
}
