﻿using Landscape.Base.Entity;
using Landscape.Base.Model;
using Landscape.Base.Templating;
using Landscape.Base.Utils;
using LandscapeRegistry.Entity.AttributeValues;
using Moq;
using Newtonsoft.Json.Linq;
using Npgsql;
using NUnit.Framework;
using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;
using System;
using System.Collections.Generic;
using System.Text;
using static Landscape.Base.Templating.ScribanVariableService;

namespace Tests.Templating
{
    class ScribanTests
    {
        [Test]
        public void Test()
        {
            {
                var testPredicateA = Predicate.Build("p_a", "p_a_forward", "p_a_backwards", AnchorState.Active);
                var atTime = TimeThreshold.BuildLatest();

                var testCIA = MergedCI.Build(Guid.NewGuid(), "test-ci-a", CIType.UnspecifiedCIType, new LayerSet(), atTime, new List<MergedCIAttribute>()
                {
                    MergedCIAttribute.Build(CIAttribute.Build(0, "a", Guid.NewGuid(), AttributeValueTextScalar.Build("a-value"), AttributeState.New, 0), new long[0]),
                    MergedCIAttribute.Build(CIAttribute.Build(0, "a.b", Guid.NewGuid(), AttributeValueTextScalar.Build("b-value"), AttributeState.New, 0), new long[0]),
                    MergedCIAttribute.Build(CIAttribute.Build(0, "a.c", Guid.NewGuid(), AttributeValueTextArray.Build(new string[] { "c-value0", "c-value1" }), AttributeState.New, 0), new long[0]),
                    MergedCIAttribute.Build(CIAttribute.Build(0, "a.json", Guid.NewGuid(), AttributeValueJSONArray.Build(
                        new string[] { @"{ ""foo"": ""bar""}", @"{ ""second"": { ""yes"": true } }" }), AttributeState.New, 0), new long[0])
                    //MergedCIAttribute.Build(CIAttribute.Build(0, "a.json", Guid.NewGuid(), AttributeValueJSONScalar.Build(
                    //    JObject.Parse(@"{ ""foo"": ""bar""}")), AttributeState.New, 0), new long[0])
                });
                var testCIB = MergedCI.Build(Guid.NewGuid(), "test-ci-b", CIType.UnspecifiedCIType, new LayerSet(), atTime, new List<MergedCIAttribute>() {});

                var relationModel = new Mock<IRelationModel>();
                relationModel.Setup(x => x.GetMergedRelations(It.IsAny<Guid>(), false, It.IsAny<LayerSet>(), IRelationModel.IncludeRelationDirections.Both, It.IsAny<NpgsqlTransaction>(), It.IsAny<TimeThreshold>()))
                    .ReturnsAsync(() => new Relation[] {
                        Relation.Build(0, testCIA.ID, testCIB.ID, testPredicateA, new long[0], RelationState.New, 0)
                    });

                var ciModel = new Mock<ICIModel>();
                ciModel.Setup(x => x.GetCompactCIs(It.IsAny<LayerSet>(), It.IsAny<NpgsqlTransaction>(), It.IsAny<TimeThreshold>(), It.IsAny<IEnumerable<Guid>>()))
                    .ReturnsAsync(new CompactCI[] { CompactCI.Build(testCIB.ID, testCIB.Name, testCIB.Type, testCIB.AtTime) });
                ciModel.Setup(x => x.GetMergedCI(testCIB.ID, It.IsAny<LayerSet>(), It.IsAny<NpgsqlTransaction>(), atTime))
                    .ReturnsAsync(() => testCIB);

                var context = ScribanVariableService.CreateCIBasedTemplateContext(testCIA, new LayerSet(), atTime, null, ciModel.Object, relationModel.Object);

                // scriban cannot deal with JTokens out of the box, TODO
                var t = @"name: {{target.name}}
                          a: {{target.attributes.a}}
                          b: {{target.attributes.a.b}}
                          x: {{target.attributes.a.x}}
                          c: {{target.attributes.a.c[1]}}
                          json: {{target.attributes.a.json}}
                            {{for related_ci in target.relations.forward.p_a}} 
                                related-x: {{related_ci.name}}
                            {{end}}
                          ";
                var template = Scriban.Template.Parse(t);
                var r = template.Render(context);
                Console.WriteLine(r);
            }
        }
    }
}
