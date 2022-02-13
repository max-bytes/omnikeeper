using FluentAssertions;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Generator;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tests.Integration.Model
{
    class GeneratorModelTest : GenericTraitEntityModelTestBase<GeneratorV1, string>
    {
        [Test]
        public void TestTraitGeneration()
        {
            var et = GenericTraitEntityHelper.Class2RecursiveTrait<GeneratorV1>();

            et.Should().BeEquivalentTo(
                new RecursiveTrait("__meta.config.generator", new TraitOriginV1(TraitOriginType.Core),
                    new List<TraitAttribute>() {
                        new TraitAttribute("id", CIAttributeTemplate.BuildFromParams("generator.id", AttributeValueType.Text, false, true, CIAttributeValueConstraintTextLength.Build(1, null), new CIAttributeValueConstraintTextRegex(IDValidations.GeneratorIDRegexString, IDValidations.GeneratorIDRegexOptions))),
                        new TraitAttribute("attribute_name", CIAttributeTemplate.BuildFromParams("generator.attribute_name", AttributeValueType.Text, false, false, CIAttributeValueConstraintTextLength.Build(1, null))),
                        new TraitAttribute("attribute_value_template", CIAttributeTemplate.BuildFromParams("generator.attribute_value_template", AttributeValueType.MultilineText, false, false)),
                    },
                    new List<TraitAttribute>()
                    {
                        new TraitAttribute("name", CIAttributeTemplate.BuildFromParams(ICIModel.NameAttribute, AttributeValueType.Text, false, false, CIAttributeValueConstraintTextLength.Build(1, null))),
                    }
                )
            );
        }

        [Test]
        public async Task TestGenericOperations()
        {
            await TestGenericModelOperations(
                () => new GeneratorV1("generator1", "attribute name 1", "template string 1"),
                () => new GeneratorV1("generator2", "attribute name 2", "template string 2"),
                "generator1", "generator2", "non_existant"
                );
        }
        [Test]
        public async Task TestGetByDataID()
        {
            await TestGenericModelGetByDataID(
                () => new GeneratorV1("generator1", "attribute name 1", "template string 1"),
                () => new GeneratorV1("generator2", "attribute name 2", "template string 2"),
                "generator1", "generator2", "non_existant_id"
                );
        }
    }
}
