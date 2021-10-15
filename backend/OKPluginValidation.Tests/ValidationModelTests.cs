using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OKPluginValidation.Validation;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tests.Integration.Model;

namespace OKPluginValidation.Tests
{
    class ValidationModelTests : GenericTraitEntityModelTestBase<Validation.Validation, string>
    {
        protected override void InitServices(IServiceCollection services)
        {
            base.InitServices(services);

            // register plugin services
            var plugin = new PluginRegistration();
            plugin.RegisterServices(services);
        }

        [Test]
        public void TestTraitGeneration()
        {
            var et = TraitEntityHelper.Class2RecursiveTrait<Validation.Validation>();

            et.Should().BeEquivalentTo(
                new RecursiveTrait("__meta.validation.validation", new TraitOriginV1(TraitOriginType.Plugin),
                    new List<TraitAttribute>() {
                                new TraitAttribute("id", CIAttributeTemplate.BuildFromParams("validation.id", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
                                new TraitAttribute("rule_name", CIAttributeTemplate.BuildFromParams("validation.rule_name", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
                                new TraitAttribute("rule_config", CIAttributeTemplate.BuildFromParams("validation.rule_config", AttributeValueType.JSON, false)),
                    },
                    new List<TraitAttribute>() {
                                new TraitAttribute("name", CIAttributeTemplate.BuildFromParams(ICIModel.NameAttribute, AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
                    }
                )
            );
        }

        [Test]
        public async Task TestGenericOperations()
        {
            await TestGenericModelOperations(
                () => new Validation.Validation("validation1", "rule1", JObject.Parse(@"{""foo"": ""bar""}")),
                () => new Validation.Validation("validation2", "rule2", JObject.Parse(@"{""blub"": true}")),
                "validation1", "validation2", "non_existant_id"
                );
        }
        [Test]
        public async Task TestGetByDataID()
        {
            await TestGenericModelGetByDataID(
                () => new Validation.Validation("validation1", "rule1", JObject.Parse(@"{""foo"": ""bar""}")),
                () => new Validation.Validation("validation2", "rule2", JObject.Parse(@"{""blub"": true}")),
                "validation1", "validation2", "non_existant_id"
                );
        }
    }
}
