using Autofac;
using Autofac.Extensions.DependencyInjection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OKPluginGenericJSONIngest.Extract;
using OKPluginGenericJSONIngest.Load;
using OKPluginGenericJSONIngest.Transform.JMESPath;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tests.Integration.Model;
using Omnikeeper.Base.Model.TraitBased;

namespace OKPluginGenericJSONIngest
{
    class ContextModelTests : GenericTraitEntityModelTestBase<Context, string>
    {
        protected override void InitServices(ContainerBuilder builder)
        {
            base.InitServices(builder);

            // register plugin services
            var plugin = new PluginRegistration();
            var serviceCollection = new ServiceCollection();
            plugin.RegisterServices(serviceCollection);
            builder.Populate(serviceCollection);
        }

        [Test]
        public void TestTraitGeneration()
        {
            var et = TraitEntityHelper.Class2RecursiveTrait<Context>();

            et.Should().BeEquivalentTo(
                new RecursiveTrait("__meta.config.gji_context", new TraitOriginV1(TraitOriginType.Plugin),
                    new List<TraitAttribute>() {
                        new TraitAttribute("id", CIAttributeTemplate.BuildFromParams("gji_context.id", AttributeValueType.Text, false, true, CIAttributeValueConstraintTextLength.Build(1, null), new CIAttributeValueConstraintTextRegex(OKPluginGenericJSONIngest.Context.ContextIDRegex))),
                        new TraitAttribute("extract_config", CIAttributeTemplate.BuildFromParams("gji_context.extract_config", AttributeValueType.JSON, false, false)),
                        new TraitAttribute("transform_config", CIAttributeTemplate.BuildFromParams("gji_context.transform_config", AttributeValueType.JSON, false, false)),
                        new TraitAttribute("load_config", CIAttributeTemplate.BuildFromParams("gji_context.load_config", AttributeValueType.JSON, false, false))
                    },
                    new List<TraitAttribute>()
                    {
                        new TraitAttribute("name", CIAttributeTemplate.BuildFromParams(ICIModel.NameAttribute, AttributeValueType.Text, false, false, CIAttributeValueConstraintTextLength.Build(1, null))),
                    })
            );
        }

        [Test]
        public async Task TestGenericOperations()
        {
            await TestGenericModelOperations(
                () => new Context("context1", new ExtractConfigPassiveRESTFiles(), new TransformConfigJMESPath("[]"), new LoadConfig(new string[] { "l1", "l2" }, "l3")),
                () => new Context("context2", new ExtractConfigPassiveRESTFiles(), new TransformConfigJMESPath("[] | []"), new LoadConfig(new string[] { "l3" }, "l4")),
                "context1", "context2", "non_existant_id"
                );
        }

        [Test]
        public async Task TestGetByDataID()
        {
            await TestGenericModelGetByDataID(
                () => new Context("context1", new ExtractConfigPassiveRESTFiles(), new TransformConfigJMESPath("[]"), new LoadConfig(new string[] { "l1", "l2" }, "l3")),
                () => new Context("context2", new ExtractConfigPassiveRESTFiles(), new TransformConfigJMESPath("[] | []"), new LoadConfig(new string[] { "l3" }, "l4")),
                "context1", "context2", "non_existant_id"
                );
        }
    }
}
