using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.GridView.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Integration.Model
{
    class PredicateModelTest : GenericTraitEntityModelTestBase
    {
        [Test]
        public void TestTraitGeneration()
        {
            var et = TraitBuilderFromClass.Class2RecursiveTrait<Predicate>();

            et.Should().BeEquivalentTo(
                new RecursiveTrait("__meta.config.predicate", new TraitOriginV1(TraitOriginType.Core),
                    new List<TraitAttribute>() {
                        new TraitAttribute("id", CIAttributeTemplate.BuildFromParams("predicate.id", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null), new CIAttributeValueConstraintTextRegex(IDValidations.PredicateIDRegex))),
                        new TraitAttribute("wording_from", CIAttributeTemplate.BuildFromParams("predicate.wording_from", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
                        new TraitAttribute("wording_to", CIAttributeTemplate.BuildFromParams("predicate.wording_to", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
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
                () => new Predicate("p1", "wording_from_1", "wording_to_1"),
                () => new Predicate("p2", "wording_from_2", "wording_to_2"),
                "p1", "p2", "non_existant_id"
                );
        }
    }
}
