using Landscape.Base.Entity;
using LandscapeRegistry.Entity.AttributeValues;
using LandscapeRegistry.Model;
using LandscapeRegistry.Model.Cached;
using LandscapeRegistry.Service;
using LandscapeRegistry.Utils;
using Microsoft.DotNet.InternalAbstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using Npgsql;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Tests.Integration;
using Tests.Integration.Model;

namespace Tests.Ingest
{
    [Explicit]
    class LoadAnsibleInventoryTest
    {
        private static string GetFilepath(string filename)
        {
            string startupPath = ApplicationEnvironment.ApplicationBasePath;
            var pathItems = startupPath.Split(Path.DirectorySeparatorChar);
            var pos = pathItems.Reverse().ToList().FindIndex(x => string.Equals("bin", x));
            string projectPath = String.Join(Path.DirectorySeparatorChar.ToString(), pathItems.Take(pathItems.Length - pos - 1));
            return Path.Combine(projectPath, "Ingest", "ansible-inventory-scan", filename);
        }

        private static string LoadFile(string filename)
        {
            var path = GetFilepath(filename);
            string strText = File.ReadAllText(path, System.Text.Encoding.UTF8);
            return strText;
        }

        private BulkCICandidateAttributeData.Fragment String2Attribute(string name, string value) => BulkCICandidateAttributeData.Fragment.Build(name, AttributeValueTextScalar.Build(value));
        private BulkCICandidateAttributeData.Fragment String2IntegerAttribute(string name, long value) => BulkCICandidateAttributeData.Fragment.Build(name, AttributeValueIntegerScalar.Build(value));

        private BulkCICandidateAttributeData.Fragment JValue2TextAttribute(JToken o, string jsonName, string attributeName = null) => BulkCICandidateAttributeData.Fragment.Build(attributeName ?? jsonName, AttributeValueTextScalar.Build(o[jsonName].Value<string>()));
        private BulkCICandidateAttributeData.Fragment JValue2IntegerAttribute(JToken o, string name) => BulkCICandidateAttributeData.Fragment.Build(name, AttributeValueIntegerScalar.Build(o[name].Value<long>()));
        private BulkCICandidateAttributeData.Fragment JValue2JSONAttribute(JToken o, string jsonName, string attributeName = null) => BulkCICandidateAttributeData.Fragment.Build(attributeName ?? jsonName, AttributeValueJSONScalar.Build(o[jsonName] as JObject));

        [SetUp]
        public void Setup()
        {
            DBSetup.Setup();
        }

        [Test]
        public async Task Test()
        {
            var username = "testUser";
            var userGUID = new Guid("7dc848b7-881d-4785-9f25-985e9b6f2715");

            var dbcb = new DBConnectionBuilder();
            //using var conn = dbcb.Build(DBSetup.dbName, false, true);
            using var conn = dbcb.Build("landscape_prototype", false, true);
            var attributeModel = new AttributeModel(conn);
            var layerModel = new LayerModel(conn);
            var userModel = new UserInDatabaseModel(conn);
            var ciModel = new CIModel(attributeModel, conn);
            var predicateModel = new PredicateModel(conn);
            var relationModel = new RelationModel(predicateModel, conn);
            var ingestDataService = new IngestDataService(attributeModel, ciModel, new ChangesetModel(userModel, conn), relationModel, conn);

            Layer layer1;
            using (var trans = conn.BeginTransaction())
            {
                //layer1 = await layerModel.CreateLayer("l1", trans);
                layer1 = await layerModel.GetLayer("Inventory Scan", trans);

                await predicateModel.InsertOrUpdate("has_mounted_device", "has mounted device", "is mounted to", AnchorState.Active, trans);

                trans.Commit();
            }

            var insertLayer = layer1;
            var user = User.Build(await userModel.UpsertUser(username, userGUID, UserType.Robot, null), new List<Layer>() { layer1 });
            var hosts = new string[] { "h1jmplx01.mhx.at", "h1lscapet01.mhx.local" };
            var layerSet = await layerModel.BuildLayerSet(null);


            // first ingest
            var (ingestData, temp2finalCIIDMap) = await PerformIngest(insertLayer, layerSet, hosts, user, ingestDataService);

            //Assert.That(await ciModel.GetCIIDs(null), Is.SupersetOf(ingestData.CICandidates.Select(cic => cic.Key)));

            // second ingest
            //await PerformIngest(insertLayer, layerSet, hosts, user, ingestDataService);

            //Assert.That(await ciModel.GetCIIDs(null), Is.SupersetOf(ingestData.CICandidates.Select(cic => cic.Key)));

            var cis = await ciModel.GetMergedCIs(layerSet, false, null, DateTimeOffset.Now, ingestData.CICandidates.Select(cic => temp2finalCIIDMap[cic.Key]));
            //Assert.AreEqual(9, cis.Count());

            Assert.That(cis.Select(ci => ci.Name), Is.SupersetOf(hosts));

            foreach(var ciid in cis.Select(ci => ci.ID)) Console.WriteLine(ciid);
        }

        private async Task<(IngestData, Dictionary<Guid, Guid>)> PerformIngest(Layer writeLayer, LayerSet searchLayers, string[] hosts, User user, IngestDataService ingestDataService)
        {
            var cis = new Dictionary<Guid, CICandidate>();
            var relations = new List<RelationCandidate>();
            foreach (var fqdn in hosts)
            {
                var tempCIID = Guid.NewGuid();
                var f = LoadFile($"{fqdn}\\setup_facts.json");
                var jo = JObject.Parse(f);
                var ciName = fqdn;

                var facts = jo["ansible_facts"];

                var attributeFragments = new List<BulkCICandidateAttributeData.Fragment>()
                {
                    JValue2TextAttribute(facts, "ansible_architecture", "cpu_architecture"),
                    JValue2JSONAttribute(facts, "ansible_cmdline", "ansible.inventory.cmdline"),
                    String2Attribute("__name", ciName),
                    String2Attribute("fqdn", fqdn)
                };
                var attributes = BulkCICandidateAttributeData.Build(attributeFragments);
                var ciCandidate = CICandidate.Build(tempCIID, CIIdentificationMethodByData.Build(new string[] { "fqdn" }), attributes);
                cis.Add(tempCIID, ciCandidate);

                // ansible mounts
                foreach(var mount in facts["ansible_mounts"])
                {
                    var tempMountCIID = Guid.NewGuid();
                    var mountValue = mount["mount"].Value<string>();
                    var ciNameMount = $"{fqdn}:{mountValue}";
                    var attributeFragmentsMount = new List<BulkCICandidateAttributeData.Fragment>
                    {
                        JValue2IntegerAttribute(mount, "block_available"),
                        JValue2IntegerAttribute(mount, "block_size"),
                        JValue2IntegerAttribute(mount, "block_total"),
                        JValue2IntegerAttribute(mount, "block_used"),
                        JValue2TextAttribute(mount, "device"),
                        JValue2TextAttribute(mount, "fstype"),
                        JValue2IntegerAttribute(mount, "inode_available"),
                        JValue2IntegerAttribute(mount, "inode_total"),
                        JValue2IntegerAttribute(mount, "inode_used"),
                        String2Attribute("mount", mountValue),
                        JValue2TextAttribute(mount, "options"),
                        JValue2IntegerAttribute(mount, "size_available"),
                        JValue2IntegerAttribute(mount, "size_total"),
                        JValue2TextAttribute(mount, "uuid"),
                        String2Attribute("__name", ciNameMount)
                    };
                    cis.Add(tempMountCIID, CICandidate.Build(tempMountCIID,
                        // TODO: ansible mounts have an uuid, find out what that is and if they can be used for identification
                        CIIdentificationMethodByData.Build(new string[] { "device", "mount", "__name" }), // TODO: do not use __name, rather maybe use its relation to the host for identification
                        BulkCICandidateAttributeData.Build(attributeFragmentsMount)));

                    relations.Add(RelationCandidate.Build(
                        CIIdentificationMethodByTemporaryCIID.Build(tempCIID),
                        CIIdentificationMethodByTemporaryCIID.Build(tempMountCIID),
                        "has_mounted_device"));
                }
            }

            var ingestData = IngestData.Build(cis, relations);
            var temp2finalCIIDMap = await ingestDataService.Ingest(ingestData, writeLayer, searchLayers, user, NullLogger.Instance);
            return (ingestData, temp2finalCIIDMap);
        }
    }
}
