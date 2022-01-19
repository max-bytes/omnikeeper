using FluentAssertions;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Omnikeeper.Base.Model.TraitBased;

namespace Tests.Integration.Model
{
    class AuthRoleModelTest : GenericTraitEntityModelTestBase<AuthRole, string>
    {
        [Test]
        public void TestTraitGeneration()
        {
            var et = GenericTraitEntityHelper.Class2RecursiveTrait<AuthRole>();

            et.Should().BeEquivalentTo(
                new RecursiveTrait("__meta.config.auth_role", new TraitOriginV1(TraitOriginType.Core),
                    new List<TraitAttribute>() {
                        new TraitAttribute("id", CIAttributeTemplate.BuildFromParams("auth_role.id", AttributeValueType.Text, false, true, CIAttributeValueConstraintTextLength.Build(1, null))),
                    },
                    new List<TraitAttribute>()
                    {
                        new TraitAttribute("permissions", CIAttributeTemplate.BuildFromParams("auth_role.permissions", AttributeValueType.Text, true, false)),
                        new TraitAttribute("name", CIAttributeTemplate.BuildFromParams(ICIModel.NameAttribute, AttributeValueType.Text, false, false, CIAttributeValueConstraintTextLength.Build(1, null))),
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
