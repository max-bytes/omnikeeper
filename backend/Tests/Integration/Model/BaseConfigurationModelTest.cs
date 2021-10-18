﻿using FluentAssertions;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.Config;
using Omnikeeper.Base.Model;
using Omnikeeper.Entity.AttributeValues;
using System.Collections.Generic;

namespace Tests.Integration.Model
{
    class BaseConfigurationModelTest : GenericTraitEntityModelTestBase<BaseConfigurationV2, string>
    {
        [Test]
        public void TestTraitGeneration()
        {
            var et = TraitBuilderFromClass.Class2RecursiveTrait<BaseConfigurationV2>();

            et.Should().BeEquivalentTo(
                    new RecursiveTrait("__meta.config.base", new TraitOriginV1(TraitOriginType.Core),
                    new List<TraitAttribute>() {
                        new TraitAttribute("archive_data_threshold", CIAttributeTemplate.BuildFromParams("base_config.archive_data_threshold", AttributeValueType.Integer, false)),
                        // TODO: add regex or other check for hangfire compatible cronjob syntax
                        new TraitAttribute("clb_runner_interval", CIAttributeTemplate.BuildFromParams("base_config.clb_runner_interval", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
                        new TraitAttribute("marked_for_deletion_runner_interval", CIAttributeTemplate.BuildFromParams("base_config.marked_for_deletion_runner_interval", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
                        new TraitAttribute("external_id_manager_runner_interval", CIAttributeTemplate.BuildFromParams("base_config.external_id_manager_runner_interval", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
                        new TraitAttribute("archive_old_data_runner_interval", CIAttributeTemplate.BuildFromParams("base_config.archive_old_data_runner_interval", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
                    },
                    new List<TraitAttribute>()
                    {
                        new TraitAttribute("name", CIAttributeTemplate.BuildFromParams(ICIModel.NameAttribute, AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
                    }
                )
            );
        }
    }
}