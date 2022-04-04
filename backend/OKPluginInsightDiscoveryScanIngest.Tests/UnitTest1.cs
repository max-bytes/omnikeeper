using Insight.Discovery.InfoClasses;
using Insight.Discovery.Tools;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Service;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.IO;

namespace OKPluginInsightDiscoveryScanIngest.Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            try
            {
                var filename = "E11C7_00001_Hosts_2022-04-02_0947_0319.xml";
                var path = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory())!.Parent!.Parent!.ToString(), "data", filename);
                var xml = File.ReadAllText(path);

                var hostList = ObjectSerializer.Instance.XMLDeserializeObject<List<HostInfo>>(xml);

                var ciCandidates = new List<CICandidate>();

                var writeLayer = "insight_discovery";
                var searchLayers = new LayerSet(writeLayer);

                Assert.IsNotNull(hostList);

                foreach (var host in hostList)
                {
                    var hostCIID = Guid.NewGuid();

                    var hostname = new CICandidateAttributeData.Fragment("insight_discovery.host.hostname", new AttributeScalarValueText(host.Hostname));

                    var attributes = new List<CICandidateAttributeData.Fragment>();
                    attributes.Add(hostname);
                    var attributeData = new CICandidateAttributeData(attributes);

                    var hostIDMethod = CIIdentificationMethodByData.BuildFromAttributes(new string[] { "insight_discovery.host.hostname" }, attributeData, searchLayers);

                    var hostCI = new CICandidate(hostCIID, hostIDMethod, attributeData);
                    ciCandidates.Add(hostCI);
                }

                await ingestDataService.Ingest(ingestData, writeLayer, user);
            } catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }
    }
}