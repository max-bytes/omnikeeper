using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OKPluginGenericJSONIngest.Load;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
                cis = new List<GenericInboundCI> {
                },
                relations = new List<GenericInboundRelation> { }
            };

            var ingestData = loader.GenericInboundData2IngestData(inboundData, new Omnikeeper.Base.Entity.LayerSet(1, 2));

            ingestData.Should().BeEquivalentTo(
                new IngestData(
                    new List<CICandidate> { }, 
                    new List<RelationCandidate> { }
                )
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
                        idMethod = new GenericInboundIDMethod
                        {
                            method = "byData",
                            attributes = new string[] {}
                        },
                        tempID = "foo",
                        attributes = new List<GenericInboundAttribute>
                        {
                            new GenericInboundAttribute
                            {
                                name = "a",
                                type = AttributeValueType.JSON,
                                value = JToken.Parse("[]")
                            }
                        }
                    }
                },
                relations = new List<GenericInboundRelation> { }
            };

            var ingestData = loader.GenericInboundData2IngestData(inboundData, new Omnikeeper.Base.Entity.LayerSet(1, 2));

            var jsonValue = ingestData.CICandidates.ToList().First().Attributes.Fragments.First().Value;
            jsonValue.Should().BeEquivalentTo(AttributeArrayValueJSON.BuildFromString(new string[] { }));
        }


        [Test]
        public void TestInvalidRelationID()
        {
            var loader = new Preparer();

            var inboundData = new GenericInboundData
            {
                cis = new List<GenericInboundCI>
                {
                    new GenericInboundCI
                    {
                        tempID = "ci1",
                        idMethod = new GenericInboundIDMethod() {attributes = new string[0], method = "byData" },
                        attributes = new List<GenericInboundAttribute>()
                    },
                    new GenericInboundCI
                    {
                        tempID = "ci2",
                        idMethod = new GenericInboundIDMethod() {attributes = new string[0], method = "byData" },
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

            Assert.Throws<Exception>(() => loader.GenericInboundData2IngestData(inboundData, new Omnikeeper.Base.Entity.LayerSet(1, 2)));
        }
    }
}
