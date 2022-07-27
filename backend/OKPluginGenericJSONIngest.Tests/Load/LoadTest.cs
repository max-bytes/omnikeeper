using FluentAssertions;
using Moq;
using NUnit.Framework;
using OKPluginGenericJSONIngest.Load;
using Omnikeeper.Base.Entity.DTO;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace OKPluginGenericJSONIngest.Tests.Load
{
    class LoadTest
    {
        [Test]
        public void TestBasicGenericInboundData2IngestData()
        {
            var loader = new Preparer();

            var inboundData = new GenericInboundData
            {
                CIs = new List<GenericInboundCI>
                {
                },
                Relations = new List<GenericInboundRelation> { }
            };

            var issueAccumulator = new Mock<IIssueAccumulator>();

            var ingestData = loader.GenericInboundData2IngestData(inboundData, new Omnikeeper.Base.Entity.LayerSet("1", "2"), issueAccumulator.Object);

            ingestData.Should().BeEquivalentTo(
                new IngestData(
                    new List<CICandidate> { },
                    new List<RelationCandidate> { }
                ), options => options.WithStrictOrdering()
            );
        }


        [Test]
        public void TestEmptyJSONArray()
        {
            var loader = new Preparer();

            var inboundData = new GenericInboundData
            {
                CIs = new List<GenericInboundCI>
                {
                    new GenericInboundCI
                    {
                        IDMethod = new InboundIDMethodByData(Array.Empty<string>()),
                        TempID = "foo",
                        Attributes = new List<GenericInboundAttribute>
                        {
                            new GenericInboundAttribute
                            {
                                Name = "a",
                                Value = AttributeValueDTO.Build(AttributeArrayValueJSON.BuildFromString(new string[0], false))
                            }
                        }
                    }
                },
                Relations = new List<GenericInboundRelation> { }
            };

            var issueAccumulator = new Mock<IIssueAccumulator>();
            var ingestData = loader.GenericInboundData2IngestData(inboundData, new Omnikeeper.Base.Entity.LayerSet("1", "2"), issueAccumulator.Object);

            var jsonValue = ingestData.CICandidates.ToList().First().Attributes.Fragments.First().Value;
            jsonValue.Should().BeEquivalentTo(AttributeArrayValueJSON.BuildFromString(new string[] { }, false), options => options.WithStrictOrdering().ComparingByMembers<JsonElement>());
        }


        [Test]
        public void TestInvalidRelationIsSkipped()
        {
            var loader = new Preparer();

            var inboundData = new GenericInboundData
            {
                CIs = new List<GenericInboundCI>
                {
                    new GenericInboundCI
                    {
                        TempID = "ci1",
                        IDMethod = new InboundIDMethodByData(Array.Empty<string>()),
                        Attributes = new List<GenericInboundAttribute>()
                    },
                    new GenericInboundCI
                    {
                        TempID = "ci2",
                        IDMethod = new InboundIDMethodByData(Array.Empty<string>()),
                        Attributes = new List<GenericInboundAttribute>()
                    }
                },
                Relations = new List<GenericInboundRelation> {
                    new GenericInboundRelation
                    {
                        From = "ci1",
                        To = "unknown ci",
                        Predicate = "predicate"
                    }
                }
            };

            var issueAccumulator = new Mock<IIssueAccumulator>();
            var ingestData = loader.GenericInboundData2IngestData(inboundData, new Omnikeeper.Base.Entity.LayerSet("1", "2"), issueAccumulator.Object);
            ingestData.Should().NotBeNull();
            ingestData.RelationCandidates.Should().BeEmpty();
        }
    }
}
