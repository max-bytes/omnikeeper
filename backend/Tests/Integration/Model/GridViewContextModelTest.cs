﻿using Autofac;
using FluentAssertions;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.GridView;
using Omnikeeper.GridView.Entity;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tests.Integration.Model
{
    class GridViewContextModelTest : GenericTraitEntityModelTestBase<GridViewContext, string>
    {
        protected override void InitServices(ContainerBuilder builder)
        {
            base.InitServices(builder);

            builder.RegisterType<GridViewContextModel>().As<GenericTraitEntityModel<GridViewContext, string>>();
        }

        [Test]
        public void TestTraitGeneration()
        {
            var et = GenericTraitEntityHelper.Class2RecursiveTrait<GridViewContext>();

            et.Should().BeEquivalentTo(new RecursiveTrait("__meta.config.gridview_context", new TraitOriginV1(TraitOriginType.Core),
                    new List<TraitAttribute>() {
                        new TraitAttribute("id", CIAttributeTemplate.BuildFromParams("gridview_context.id", AttributeValueType.Text, false, true, CIAttributeValueConstraintTextLength.Build(1, null), new CIAttributeValueConstraintTextRegex(IDValidations.GridViewContextIDRegex))),
                        new TraitAttribute("config", CIAttributeTemplate.BuildFromParams("gridview_context.config", AttributeValueType.JSON, false, false)),
                    },
                    new List<TraitAttribute>()
                    {
                        new TraitAttribute("speaking_name", CIAttributeTemplate.BuildFromParams("gridview_context.speaking_name", AttributeValueType.Text, false, false, CIAttributeValueConstraintTextLength.Build(1, null))),
                        new TraitAttribute("description", CIAttributeTemplate.BuildFromParams("gridview_context.description", AttributeValueType.Text, false, false, CIAttributeValueConstraintTextLength.Build(1, null))),
                        new TraitAttribute("name", CIAttributeTemplate.BuildFromParams(ICIModel.NameAttribute, AttributeValueType.Text, false, false, CIAttributeValueConstraintTextLength.Build(1, null))),
                    }
                ), options => options.WithoutStrictOrdering()
            );
        }

        [Test]
        public async Task TestGenericOperations()
        {
            await TestGenericModelOperations(
                () => new GridViewContext("context1", "Context 1", "Description 1",
                    new GridViewConfiguration(true, "write_layer1", new List<string> { "read_layer1" }, new List<GridViewColumn>() { }, "trait1")),
                () => new GridViewContext("context2", "Context 2", "Description 2",
                    new GridViewConfiguration(true, "write_layer2", new List<string> { "read_layer2" }, new List<GridViewColumn>() { }, "trait2")),
                "context1", "context2", "non_existant_id"
                );
        }
        [Test]
        public async Task TestGetByDataID()
        {
            await TestGenericModelGetByDataID(
                () => new GridViewContext("context1", "Context 1", "Description 1",
                    new GridViewConfiguration(true, "write_layer1", new List<string> { "read_layer1" }, new List<GridViewColumn>() { }, "trait1")),
                () => new GridViewContext("context2", "Context 2", "Description 2",
                    new GridViewConfiguration(true, "write_layer2", new List<string> { "read_layer2" }, new List<GridViewColumn>() { }, "trait2")),
                "context1", "context2", "nonExistingID"
                );
        }
    }
}
