using Autofac;
using FluentAssertions;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tests.Integration.Model
{
    class RecursiveTraitModelTests : GenericTraitEntityModelTestBase<RecursiveTrait, string>
    {
        protected override void InitServices(ContainerBuilder builder)
        {
            base.InitServices(builder);

            builder.RegisterType<RecursiveTraitModel>().As<GenericTraitEntityModel<RecursiveTrait, string>>();
        }

        [Test]
        public void TestTraitGeneration()
        {
            var et = GenericTraitEntityHelper.Class2RecursiveTrait<RecursiveTrait>();

            et.Should().BeEquivalentTo(
                new RecursiveTrait("__meta.config.trait", new TraitOriginV1(TraitOriginType.Core),
                    new List<TraitAttribute>() {
                        new TraitAttribute("id", CIAttributeTemplate.BuildFromParams("trait.id", AttributeValueType.Text, false, true, CIAttributeValueConstraintTextLength.Build(1, null), new CIAttributeValueConstraintTextRegex(IDValidations.TraitIDRegex))),
                        new TraitAttribute("required_attributes", CIAttributeTemplate.BuildFromParams("trait.required_attributes", AttributeValueType.JSON, true, false, new CIAttributeValueConstraintArrayLength(1, null)))
                    },
                    new List<TraitAttribute>()
                    {
                        new TraitAttribute("optional_attributes", CIAttributeTemplate.BuildFromParams("trait.optional_attributes", AttributeValueType.JSON, true, false)),
                        new TraitAttribute("optional_relations", CIAttributeTemplate.BuildFromParams("trait.optional_relations", AttributeValueType.JSON, true, false)),
                        new TraitAttribute("required_traits", CIAttributeTemplate.BuildFromParams("trait.required_traits", AttributeValueType.Text, true, false)),
                        new TraitAttribute("name", CIAttributeTemplate.BuildFromParams(ICIModel.NameAttribute, AttributeValueType.Text, false, false, CIAttributeValueConstraintTextLength.Build(1, null))),
                    }
                )
            );
        }

        [Test]
        public async Task TestGenericOperations()
        {
            await TestGenericModelOperations(
                () => new RecursiveTrait("trait1", new TraitOriginV1(TraitOriginType.Data),
                    new List<TraitAttribute>() { new TraitAttribute("test_ta1", CIAttributeTemplate.BuildFromParams("test_a", AttributeValueType.Text, false, false, CIAttributeValueConstraintTextLength.Build(1, null))) },
                    new List<TraitAttribute>() { new TraitAttribute("test_tb1", CIAttributeTemplate.BuildFromParams("test_b", AttributeValueType.JSON, false, true, CIAttributeValueConstraintTextLength.Build(1, null))) },
                    new List<TraitRelation>() { },
                    new List<string>() { "dependent_trait1" }
                    ),
                () => new RecursiveTrait("trait2", new TraitOriginV1(TraitOriginType.Data),
                    new List<TraitAttribute>() { new TraitAttribute("test_ta2", CIAttributeTemplate.BuildFromParams("test_a", AttributeValueType.Text, false, false, CIAttributeValueConstraintTextLength.Build(1, null))) },
                    new List<TraitAttribute>() { new TraitAttribute("test_tb2", CIAttributeTemplate.BuildFromParams("test_b", AttributeValueType.JSON, false, true, CIAttributeValueConstraintTextLength.Build(1, null))) },
                    new List<TraitRelation>() { },
                    new List<string>() { "dependent_trait2" }
                    ),
                    "trait1", "trait2", "non_existant_id"
                );
        }

        [Test]
        public async Task TestGetByDataID()
        {
            await TestGenericModelGetByDataID(
                () => new RecursiveTrait("trait1", new TraitOriginV1(TraitOriginType.Data),
                    new List<TraitAttribute>() { new TraitAttribute("test_ta1", CIAttributeTemplate.BuildFromParams("test_a", AttributeValueType.Text, false, false, CIAttributeValueConstraintTextLength.Build(1, null))) },
                    new List<TraitAttribute>() { new TraitAttribute("test_tb1", CIAttributeTemplate.BuildFromParams("test_b", AttributeValueType.JSON, false, true, CIAttributeValueConstraintTextLength.Build(1, null))) },
                    new List<TraitRelation>() { },
                    new List<string>() { "dependent_trait1" }
                    ),
                () => new RecursiveTrait("trait2", new TraitOriginV1(TraitOriginType.Data),
                    new List<TraitAttribute>() { new TraitAttribute("test_ta2", CIAttributeTemplate.BuildFromParams("test_a", AttributeValueType.Text, false, false, CIAttributeValueConstraintTextLength.Build(1, null))) },
                    new List<TraitAttribute>() { new TraitAttribute("test_tb2", CIAttributeTemplate.BuildFromParams("test_b", AttributeValueType.JSON, false, true, CIAttributeValueConstraintTextLength.Build(1, null))) },
                    new List<TraitRelation>() { },
                    new List<string>() { "dependent_trait2" }
                    ),
                "trait1", "trait2", "non_existant_id"
            );
        }
    }
}
