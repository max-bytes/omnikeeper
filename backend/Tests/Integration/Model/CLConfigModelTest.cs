using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Omnikeeper.Base.Model.TraitBased;

namespace Tests.Integration.Model
{
    class CLConfigModelTest : GenericTraitEntityModelTestBase<CLConfigV1, string>
    {
        [Test]
        public void TestTraitGeneration()
        {
            var et = TraitEntityHelper.Class2RecursiveTrait<CLConfigV1>();

            et.Should().BeEquivalentTo(
                new RecursiveTrait("__meta.config.cl_config", new TraitOriginV1(TraitOriginType.Core),
                    new List<TraitAttribute>() {
                        new TraitAttribute("id", CIAttributeTemplate.BuildFromParams("cl_config.id", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null), new CIAttributeValueConstraintTextRegex(IDValidations.CLConfigIDRegexString, IDValidations.CLConfigIDRegexOptions))),
                        new TraitAttribute("cl_brain_reference", CIAttributeTemplate.BuildFromParams("cl_config.cl_brain_reference", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
                        new TraitAttribute("cl_brain_config", CIAttributeTemplate.BuildFromParams("cl_config.cl_brain_config", AttributeValueType.JSON, false)),
                    },
                    new List<TraitAttribute>()
                    {
                        new TraitAttribute("name", CIAttributeTemplate.BuildFromParams(ICIModel.NameAttribute, AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
                    }
                )
            );
        }

        [Test]
        public async Task TestGenericOperations()
        {
            await TestGenericModelOperations(
                () => new CLConfigV1("test_cl_config01", "clBrainRef1", JObject.Parse(@"{""foo"": ""bar""}")),
                () => new CLConfigV1("test_cl_config02", "clBrainRef2", JObject.Parse(@"{""foo"": ""blub""}")),
                "test_cl_config01", "test_cl_config02", "non_existant"
                );
        }
        [Test]
        public async Task TestGetByDataID()
        {
            await TestGenericModelGetByDataID(
                () => new CLConfigV1("test_cl_config01", "clBrainRef1", JObject.Parse(@"{""foo"": ""bar""}")),
                () => new CLConfigV1("test_cl_config02", "clBrainRef2", JObject.Parse(@"{""foo"": ""blub""}")),
                "test_cl_config01", "test_cl_config02", "non_existant_id"
                );
        }
    }
}
