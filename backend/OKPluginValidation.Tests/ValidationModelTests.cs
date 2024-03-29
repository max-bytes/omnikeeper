﻿using Autofac;
using Autofac.Extensions.DependencyInjection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Entity.AttributeValues;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Tests.Integration.Model;

namespace OKPluginValidation.Tests
{
    class ValidationModelTests : GenericTraitEntityModelTestBase<Validation, string>
    {
        protected override void InitServices(ContainerBuilder builder)
        {
            base.InitServices(builder);

            // register plugin services
            var plugin = new PluginRegistration();
            var serviceCollection = new ServiceCollection();
            plugin.RegisterServices(serviceCollection);
            builder.Populate(serviceCollection);

            builder.RegisterType<ValidationModel>().As<GenericTraitEntityModel<Validation, string>>();
        }

        [Test]
        public void TestTraitGeneration()
        {
            var et = GenericTraitEntityHelper.Class2RecursiveTrait<Validation>();

            et.Should().BeEquivalentTo(
                new RecursiveTrait("__meta.validation.validation", new TraitOriginV1(TraitOriginType.Plugin),
                    new List<TraitAttribute>() {
                                new TraitAttribute("id", CIAttributeTemplate.BuildFromParams("validation.id", AttributeValueType.Text, false, true, CIAttributeValueConstraintTextLength.Build(1, null))),
                                new TraitAttribute("rule_name", CIAttributeTemplate.BuildFromParams("validation.rule_name", AttributeValueType.Text, false, false, CIAttributeValueConstraintTextLength.Build(1, null))),
                                new TraitAttribute("rule_config", CIAttributeTemplate.BuildFromParams("validation.rule_config", AttributeValueType.JSON, false, false)),
                    },
                    new List<TraitAttribute>() {
                                new TraitAttribute("name", CIAttributeTemplate.BuildFromParams(ICIModel.NameAttribute, AttributeValueType.Text, false, false, CIAttributeValueConstraintTextLength.Build(1, null))),
                    },
                    new List<TraitRelation>()
                    {
                        new TraitRelation("detected_issues", new RelationTemplate("__meta.validation.belongs_to_validation", false))
                    }
                )
            );
        }

        [Test]
        public async Task TestGenericOperations()
        {
            await TestGenericModelOperations(
                () => new Validation("validation1", "rule1", JsonDocument.Parse(@"{""foo"": ""bar""}")),
                () => new Validation("validation2", "rule2", JsonDocument.Parse(@"{""blub"": true}")),
                "validation1", "validation2", "non_existant_id"
                );
        }
        [Test]
        public async Task TestGetByDataID()
        {
            await TestGenericModelGetByDataID(
                () => new Validation("validation1", "rule1", JsonDocument.Parse(@"{""foo"": ""bar""}")),
                () => new Validation("validation2", "rule2", JsonDocument.Parse(@"{""blub"": true}")),
                "validation1", "validation2", "non_existant_id"
                );
        }
    }
}
