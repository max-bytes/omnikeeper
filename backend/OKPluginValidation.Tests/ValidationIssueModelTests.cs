using Autofac;
using Autofac.Extensions.DependencyInjection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
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
    class ValidationIssueModelTests : GenericTraitEntityModelTestBase<ValidationIssue, string>
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
            var et = TraitEntityHelper.Class2RecursiveTrait<ValidationIssue>();

            et.Should().BeEquivalentTo(
                new RecursiveTrait("__meta.validation.validation_issue", new TraitOriginV1(TraitOriginType.Plugin),
                    new List<TraitAttribute>() {
                        new TraitAttribute("id", CIAttributeTemplate.BuildFromParams("validation_issue.id", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
                        new TraitAttribute("message", CIAttributeTemplate.BuildFromParams("validation_issue.message", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
                    },
                    new List<TraitAttribute>() {
                        new TraitAttribute("name", CIAttributeTemplate.BuildFromParams(ICIModel.NameAttribute, AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
                    },
                    new List<TraitRelation>()
                    {
                        new TraitRelation("has_issue", new RelationTemplate("__meta.validation.has_issue", false, 1, null)),
                    }
                )
            );
        }

        [Test]
        public async Task TestGenericOperations()
        {
            var ciModel = ServiceProvider.GetRequiredService<ICIModel>();
            var affectedCIIDs = new Guid[] { 
                await ciModel.CreateCI(ModelContextBuilder.BuildImmediate()), 
                await ciModel.CreateCI(ModelContextBuilder.BuildImmediate()), 
                await ciModel.CreateCI(ModelContextBuilder.BuildImmediate()) 
            };
            await TestGenericModelOperations(
                () => new ValidationIssue("validation_issue1", "msg1", new Guid[] { affectedCIIDs[0], affectedCIIDs[1] }),
                () => new ValidationIssue("validation_issue2", "msg2", new Guid[] { affectedCIIDs[2] }),
                "validation_issue1", "validation_issue2", "non_existant_id"
                );
        }
        [Test]
        public async Task TestGetByDataID()
        {
            var ciModel = ServiceProvider.GetRequiredService<ICIModel>();
            var affectedCIIDs = new Guid[] {
                await ciModel.CreateCI(ModelContextBuilder.BuildImmediate()),
                await ciModel.CreateCI(ModelContextBuilder.BuildImmediate()),
                await ciModel.CreateCI(ModelContextBuilder.BuildImmediate())
            };

            await TestGenericModelGetByDataID(
                () => new ValidationIssue("validation_issue1", "msg1", new Guid[] { affectedCIIDs[0], affectedCIIDs[1] }),
                () => new ValidationIssue("validation_issue2", "msg2", new Guid[] { affectedCIIDs[2] }),
                "validation_issue1", "validation_issue2", "non_existant_id"
                );
        }
    }
}
