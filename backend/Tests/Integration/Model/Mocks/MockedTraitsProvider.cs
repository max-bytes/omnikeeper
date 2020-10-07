using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.Service;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Tests.Integration.Model.Mocks
{
    public class MockedTraitsProvider : ITraitsProvider
    {
        public async Task<TraitSet> GetActiveTraitSet(NpgsqlTransaction trans, TimeThreshold timeThreshold)
        {
            var r = new List<RecursiveTrait>() {
                RecursiveTrait.Build("test_trait_1", new List<TraitAttribute>()
                {
                    TraitAttribute.Build("a4",
                        CIAttributeTemplate.BuildFromParams("a4", AttributeValueType.Text, false)
                    )
                }, new List<TraitAttribute>() { }, new List<TraitRelation>() { }),
                RecursiveTrait.Build("test_trait_2", new List<TraitAttribute>()
                {
                    TraitAttribute.Build("a4",
                        CIAttributeTemplate.BuildFromParams("a4", AttributeValueType.Text, false)
                    ),
                    TraitAttribute.Build("a2",
                        CIAttributeTemplate.BuildFromParams("a2", AttributeValueType.Text, false)
                    )
                }, new List<TraitAttribute>() { }, new List<TraitRelation>() { }),
                RecursiveTrait.Build("test_trait_3", new List<TraitAttribute>()
                {
                    TraitAttribute.Build("a1",
                        CIAttributeTemplate.BuildFromParams("a1", AttributeValueType.Text, false)
                    )
                }, new List<TraitAttribute>() { }, new List<TraitRelation>() { }),
                RecursiveTrait.Build("test_trait_4", new List<TraitAttribute>()
                {
                    TraitAttribute.Build("a1",
                        CIAttributeTemplate.BuildFromParams("a1", AttributeValueType.Text, false)
                    )
                }, requiredTraits: new List<string>() { "test_trait_1" }),
                RecursiveTrait.Build("test_trait_5", new List<TraitAttribute>()
                {
                    TraitAttribute.Build("a2",
                        CIAttributeTemplate.BuildFromParams("a2", AttributeValueType.Text, false)
                    )
                }, requiredTraits: new List<string>() { "test_trait_4" })
            };

            // TODO: should we really flatten here in a mocked class?
            return TraitSet.Build(RecursiveTraitService.FlattenDependentTraits(r.ToImmutableDictionary(r => r.Name)));
        }
    }
}
