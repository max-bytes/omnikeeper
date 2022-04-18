using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using OKPluginGenericJSONIngest.Load;
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
                cis = new List<GenericInboundCI>
                {
                },
                relations = new List<GenericInboundRelation> { }
            };

            var ingestData = loader.GenericInboundData2IngestData(inboundData, new Omnikeeper.Base.Entity.LayerSet("1", "2"), NullLogger.Instance);

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
                cis = new List<GenericInboundCI>
                {
                    new GenericInboundCI
                    {
                        idMethod = new InboundIDMethodByData(Array.Empty<string>()),
                        tempID = "foo",
                        attributes = new List<GenericInboundAttribute>
                        {
                            new GenericInboundAttribute
                            {
                                name = "a",
                                value = AttributeArrayValueJSON.BuildFromString(new string[0])
                            }
                        }
                    }
                },
                relations = new List<GenericInboundRelation> { }
            };

            var ingestData = loader.GenericInboundData2IngestData(inboundData, new Omnikeeper.Base.Entity.LayerSet("1", "2"), NullLogger.Instance);

            var jsonValue = ingestData.CICandidates.ToList().First().Attributes.Fragments.First().Value;
            jsonValue.Should().BeEquivalentTo(AttributeArrayValueJSON.BuildFromString(new string[] { }), options => options.WithStrictOrdering().ComparingByMembers<JsonElement>());
        }


        [Test]
        public void TestInvalidRelationIsSkipped()
        {
            var loader = new Preparer();

            var inboundData = new GenericInboundData
            {
                cis = new List<GenericInboundCI>
                {
                    new GenericInboundCI
                    {
                        tempID = "ci1",
                        idMethod = new InboundIDMethodByData(Array.Empty<string>()),
                        attributes = new List<GenericInboundAttribute>()
                    },
                    new GenericInboundCI
                    {
                        tempID = "ci2",
                        idMethod = new InboundIDMethodByData(Array.Empty<string>()),
                        attributes = new List<GenericInboundAttribute>()
                    }
                },
                relations = new List<GenericInboundRelation> {
                    new GenericInboundRelation
                    {
                        from = "ci1",
                        to = "unknown ci",
                        predicate = "predicate"
                    }
                }
            };

            var ingestData = loader.GenericInboundData2IngestData(inboundData, new Omnikeeper.Base.Entity.LayerSet("1", "2"), NullLogger.Instance);
            ingestData.Should().NotBeNull();
            ingestData.RelationCandidates.Should().BeEmpty();
        }
    }
}
