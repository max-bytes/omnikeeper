using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
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
