﻿using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Templating;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.Model;
using Moq;
using Newtonsoft.Json.Linq;
using Npgsql;
using NUnit.Framework;
using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;
using Scriban.Syntax;
using System;
using System.Collections.Generic;
using System.Text;
using static Omnikeeper.Base.Templating.ScribanVariableService;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Base.Entity.DataOrigin;

namespace Tests.Templating
{
    class ScribanTests
    {
        [Test]
        public void Test()
        {
            {
                var testPredicateA = new Predicate("p_a", "p_a_forward", "p_a_backwards", AnchorState.Active, PredicateModel.DefaultConstraits);
                var atTime = TimeThreshold.BuildLatest();

                var staticChangesetID = Guid.NewGuid();
                var testCIA = new MergedCI(Guid.NewGuid(), "test-ci-a", new LayerSet(), atTime, new List<MergedCIAttribute>()
                {
                    new MergedCIAttribute(new CIAttribute(Guid.NewGuid(), "a", Guid.NewGuid(), new AttributeScalarValueText("a-value"), AttributeState.New, staticChangesetID, new DataOriginV1(DataOriginType.Manual)), new long[0]),
                    new MergedCIAttribute(new CIAttribute(Guid.NewGuid(), "a.b", Guid.NewGuid(), new AttributeScalarValueText("b-value"), AttributeState.New, staticChangesetID, new DataOriginV1(DataOriginType.Manual)), new long[0]),
                    new MergedCIAttribute(new CIAttribute(Guid.NewGuid(), "a.c", Guid.NewGuid(), AttributeArrayValueText.BuildFromString(new string[] { "c-value0", "c-value1" }), AttributeState.New, staticChangesetID, new DataOriginV1(DataOriginType.Manual)), new long[0]),
                    new MergedCIAttribute(new CIAttribute(Guid.NewGuid(), "a.json", Guid.NewGuid(), AttributeArrayValueJSON.BuildFromString(
                        new string[] { @"{ ""foo"": ""bar""}", @"{ ""second"": { ""yes"": true } }" }), AttributeState.New, staticChangesetID, new DataOriginV1(DataOriginType.Manual)), new long[0])
                    //new MergedCIAttribute(new CIAttribute(0, "a.json", Guid.NewGuid(), AttributeValueJSONScalar.Build(
                    //    JObject.Parse(@"{ ""foo"": ""bar""}")), AttributeState.New, 0), new long[0])
                });
                var testCIB = new MergedCI(Guid.NewGuid(), "test-ci-b", new LayerSet(), atTime, new List<MergedCIAttribute>() {});

                var relationModel = new Mock<IRelationModel>();
                relationModel.Setup(x => x.GetMergedRelations(It.IsAny<IRelationSelection>(), It.IsAny<LayerSet>(), It.IsAny<IModelContext>(), It.IsAny<TimeThreshold>()))
                    .ReturnsAsync(() => new MergedRelation[] {
                        new MergedRelation(new Relation(Guid.NewGuid(), testCIA.ID, testCIB.ID, testPredicateA, RelationState.New, staticChangesetID, new DataOriginV1(DataOriginType.Manual)), new long[0])
                    });

                var ciModel = new Mock<ICIModel>();
                ciModel.Setup(x => x.GetCompactCIs(It.IsAny<ICIIDSelection>(), It.IsAny<LayerSet>(), It.IsAny<IModelContext>(), It.IsAny<TimeThreshold>()))
                    .ReturnsAsync(new CompactCI[] { new CompactCI(testCIB.ID, testCIB.Name, testCIB.Layers.LayerHash, testCIB.AtTime) });
                ciModel.Setup(x => x.GetMergedCI(testCIB.ID, It.IsAny<LayerSet>(), It.IsAny<IModelContext>(), atTime))
                    .ReturnsAsync(() => testCIB);

                var context = ScribanVariableService.CreateComplexCIBasedTemplateContext(testCIA, new LayerSet(), atTime, new Mock<IModelContext>().Object, ciModel.Object, relationModel.Object);

                // scriban cannot deal with JTokens out of the box, TODO
                //var t = @"name: {{target.name}}
                //          a: {{target.attributes.a}}
                //          b: {{target.attributes.a.b}}
                //          x: {{target.attributes.a.x}}
                //          c: {{target.attributes.a.c[1]}}
                //          json: {{target.attributes.a.json}}
                //            {{for related_ci in target.relations.forward.p_a}} 
                //                related-x: {{related_ci.name}}
                //            {{end}}
                //          ";

                var t = @"
                    {{ output = [] }}
                    {{~ for related_ci in [1,2,3] ~}}
                    {{~ capture item ~}}{
                      ""type"": ""service"",
                      ""description"": ""20 generic application"",
                      ""command"": {
                                        ""executable"": ""check_application""
                        ""parameters"": ""--application-name ""
                      }
                    }{{~ end ~}}
                    {{~ output = output | array.add item ~}}
                    {{~ end ~}}
                    {{ output | array.join "","" }}
                    ";
                var template = Scriban.Template.Parse(t);
                var r = template.Render(context);
                Console.WriteLine(r);
            }
        }

        [Test]
        public void TestNestedStuff()
        {
            //var so = new ScriptObjectCI(ci, new ScriptObjectContext(layerSet, trans, atTime, ciModel, relationModel));
            var context = new TemplateContext
            {
                StrictVariables = true
            };
            context.PushGlobal(new ScriptObject() { { "a", new object[] { new { name = "value-a" }, new { name = "value-b" } } } });


            var t = @"{{ a | array.map ""name"" }}";
            var template = Scriban.Template.Parse(t);
            var r = template.Render(context);
            Console.WriteLine(r);
        }
    }
}
