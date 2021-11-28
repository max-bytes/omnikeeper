using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Omnikeeper.Base.Model.TraitBased;

namespace Tests.Integration.Model
{
    class RecursiveTraitModelTests : GenericTraitEntityModelTestBase<RecursiveTrait, string>
    {
        [Test]
        public void TestTraitGeneration()
        {
            var et = TraitEntityHelper.Class2RecursiveTrait<RecursiveTrait>();

            et.Should().BeEquivalentTo(
                new RecursiveTrait("__meta.config.trait", new TraitOriginV1(TraitOriginType.Core),
                    new List<TraitAttribute>() {
                        new TraitAttribute("id", CIAttributeTemplate.BuildFromParams("trait.id", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null), new CIAttributeValueConstraintTextRegex(IDValidations.TraitIDRegex))),
                        new TraitAttribute("required_attributes", CIAttributeTemplate.BuildFromParams("trait.required_attributes", AttributeValueType.JSON, true, new CIAttributeValueConstraintArrayLength(1, null)))
                    },
                    new List<TraitAttribute>()
                    {
                        new TraitAttribute("optional_attributes", CIAttributeTemplate.BuildFromParams("trait.optional_attributes", AttributeValueType.JSON, true)),
                        new TraitAttribute("required_relations", CIAttributeTemplate.BuildFromParams("trait.required_relations", AttributeValueType.JSON, true)),
                        new TraitAttribute("optional_relations", CIAttributeTemplate.BuildFromParams("trait.optional_relations", AttributeValueType.JSON, true)),
                        new TraitAttribute("required_traits", CIAttributeTemplate.BuildFromParams("trait.required_traits", AttributeValueType.Text, true)),
                        new TraitAttribute("name", CIAttributeTemplate.BuildFromParams(ICIModel.NameAttribute, AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
                    }
                )
            );
        }

        [Test]
        public async Task TestGenericOperations()
        {
            await TestGenericModelOperations(
                () => new RecursiveTrait("trait1", new TraitOriginV1(TraitOriginType.Data),
                    new List<TraitAttribute>() { new TraitAttribute("test_ta1", CIAttributeTemplate.BuildFromParams("test_a", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))) },
                    new List<TraitAttribute>() { new TraitAttribute("test_tb1", CIAttributeTemplate.BuildFromParams("test_b", AttributeValueType.JSON, true, CIAttributeValueConstraintTextLength.Build(1, null))) },
                    new List<TraitRelation>() { },
                    new List<TraitRelation>() { },
                    new List<string>() { "dependent_trait1" }
                    ),
                () => new RecursiveTrait("trait2", new TraitOriginV1(TraitOriginType.Data),
                    new List<TraitAttribute>() { new TraitAttribute("test_ta2", CIAttributeTemplate.BuildFromParams("test_a", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))) },
                    new List<TraitAttribute>() { new TraitAttribute("test_tb2", CIAttributeTemplate.BuildFromParams("test_b", AttributeValueType.JSON, true, CIAttributeValueConstraintTextLength.Build(1, null))) },
                    new List<TraitRelation>() { },
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
                    new List<TraitAttribute>() { new TraitAttribute("test_ta1", CIAttributeTemplate.BuildFromParams("test_a", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))) },
                    new List<TraitAttribute>() { new TraitAttribute("test_tb1", CIAttributeTemplate.BuildFromParams("test_b", AttributeValueType.JSON, true, CIAttributeValueConstraintTextLength.Build(1, null))) },
                    new List<TraitRelation>() { },
                    new List<TraitRelation>() { },
                    new List<string>() { "dependent_trait1" }
                    ),
                () => new RecursiveTrait("trait2", new TraitOriginV1(TraitOriginType.Data),
                    new List<TraitAttribute>() { new TraitAttribute("test_ta2", CIAttributeTemplate.BuildFromParams("test_a", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))) },
                    new List<TraitAttribute>() { new TraitAttribute("test_tb2", CIAttributeTemplate.BuildFromParams("test_b", AttributeValueType.JSON, true, CIAttributeValueConstraintTextLength.Build(1, null))) },
                    new List<TraitRelation>() { },
                    new List<TraitRelation>() { },
                    new List<string>() { "dependent_trait2" }
                    ),
                "trait1", "trait2", "non_existant_id"
            );
        }
    }
}
