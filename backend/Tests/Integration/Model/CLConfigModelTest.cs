using FluentAssertions;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tests.Integration.Model
{
    class CLConfigModelTest : GenericTraitEntityModelTestBase
    {
        [Test]
        public void TestTraitGeneration()
        {
            var et = TraitBuilderFromClass.Class2RecursiveTrait<CLConfigV1>();

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
                () => new AuthRole("test_auth_role01", new string[] { "p1", "p2" }),
                () => new AuthRole("test_auth_role02", new string[] { "p3" }),
                "test_auth_role01", "test_auth_role02", "non_existant"
                );
        }
        [Test]
        public async Task TestGetByDataID()
        {
            await TestGenericModelGetByDataID(
                () => new AuthRole("test_auth_role01", new string[] { "p1", "p2" }),
                () => new AuthRole("test_auth_role02", new string[] { "p3" }),
                "test_auth_role01", "test_auth_role02", "non_existant_id"
                );
        }
    }
}
